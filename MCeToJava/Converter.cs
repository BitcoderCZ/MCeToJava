// <copyright file="Converter.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using FluentResults;
using MathUtils.Vectors;
using MCeToJava.Exceptions;
using MCeToJava.Models;
using MCeToJava.Models.MCE;
using MCeToJava.NBT;
using MCeToJava.Registry;
using MCeToJava.Utils;
using Serilog;
using SharpNBT;
using Spectre.Console;
using System.Buffers.Binary;
using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace MCeToJava;

internal static partial class Converter
{
	// Read file
	// Parse buildplate json
	// Parse model json
	// Calculate Y, add solid air
	// Convert (add chunk nbt)
	// Fill air
	// Additional files (level.dat, buildplate_metadata.json)
	// Write zip
	public static readonly int NumbProgressStages = 8;

	private static bool registryInitialized = false;

	public static async Task<Result> ConvertFile(string? inPath, string outPath, ProgressTask? task, Options options)
	{
		if (task is not null)
		{
			task.MaxValue = NumbProgressStages;
			task.Value = 0;
		}

		if (string.IsNullOrEmpty(inPath))
		{
			options.Logger.Error($"Invalid in-path '{inPath}'");
			return Result.Fail(new ErrorCodeError($"Invalid in-path '{inPath}'", ErrorCode.CliParseError));
		}

		task?.StartTask();

		options.Logger.Information($"Converting '{Path.GetFullPath(inPath)}'");

		string buildplateText;
		try
		{
			buildplateText = await File.ReadAllTextAsync(inPath).ConfigureAwait(false);
		}
		catch (FileNotFoundException fileNotFound)
		{
			options.Logger.Error($"File '{fileNotFound.FileName}' wasn't found.");
			return Result.Fail(new ErrorCodeError($"File '{fileNotFound.FileName}' wasn't found", ErrorCode.FileNotFound).CausedBy(fileNotFound));
		}
		catch (Exception ex)
		{
			options.Logger.Error($"Failed to read input file: {ex}");
			return Result.Fail(new ErrorCodeError($"Failed to read input file", ErrorCode.UnknownError).CausedBy(ex));
		}

		task?.Increment(1);

		Buildplate? buildplate;
		try
		{
			buildplate = JsonUtils.DeserializeJson<Buildplate>(buildplateText);

			if (buildplate is null)
			{
				throw new ConvertException("Invalid json - null.");
			}
		}
		catch (Exception ex)
		{
			options.Logger.Error($"Failed to parse input file: {ex}");
			return Result.Fail(new ErrorCodeError($"Failed to parse input file", ErrorCode.UnknownError).CausedBy(ex));
		}

		task?.Increment(1);

		try
		{
			InitRegistry(options.Logger);
		}
		catch (Exception ex)
		{
			options.Logger.Error($"Failed to initialize block registry: {ex}");
			return Result.Fail(new ErrorCodeError($"Failed to initialize block registry", ErrorCode.UnknownError).CausedBy(ex));
		}

		WorldData data;
		try
		{
			data = await Convert(buildplate, task, options).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			options.Logger.Error($"Failed to convert buildplate: {ex}");
			return Result.Fail(new ErrorCodeError($"Failed to convert buildplate", ErrorCode.UnknownError).CausedBy(ex));
		}

		options.Logger.Information($"Writing output zip");

		try
		{
			using (FileStream fs = new FileStream(outPath, FileMode.Create, FileAccess.Write, FileShare.Read))
			{
				data.WriteToStream(fs);
			}
		}
		catch (Exception ex)
		{
			options.Logger.Error($"Failed to write output file: {ex}");
			return Result.Fail(new ErrorCodeError($"Failed to write output file", ErrorCode.UnknownError).CausedBy(ex));
		}

		task?.Increment(1);

		options.Logger.Information($"Done");

		return Result.Ok();
	}

	public static async Task<WorldData> Convert(Buildplate buildplate, ProgressTask? task, Options options)
	{
		BuildplateModel model = JsonUtils.DeserializeJson<BuildplateModel>(System.Convert.FromBase64String(buildplate.Model))
			?? throw new ConvertException("Invalid json - buildplate is null.");

		if (model.FormatVersion != 1)
		{
			throw new ConvertException($"Unsupported version '{model.FormatVersion}', only version 1 is supported.");
		}

		task?.Increment(1);

		WorldData worldData = new WorldData();

		int lowestY = CalculateLowestY(model);
		if (lowestY == int.MaxValue)
		{
			options.Logger.Error($"Failed to calculate lowest y position.");
		}
		else
		{
			AddSolidAir(buildplate, model, lowestY);
		}

		task?.Increment(1);

		options.Logger.Information($"Writing chunks");
		Dictionary<int2, BlockChunk> blockChunks = [];
		Dictionary<int2, EntityChunk> entityChunks = [];

		foreach (var subChunk in model.SubChunks)
		{
			BlockChunk chunk = blockChunks.ComputeIfAbsent(new int2(subChunk.Position.X, subChunk.Position.Z), pos => new BlockChunk(pos.X, pos.Y))!;

			Debug.Assert(subChunk.Position.Y >= 0, "Y chunk position should be positive.");

			int[] blocks = subChunk.Blocks;
			List<PaletteEntry> palette = subChunk.BlockPalette;

			int yOffset = subChunk.Position.Y * 16;
			for (int x = 0; x < 16; x++)
			{
				for (int y = 0; y < 16; y++)
				{
					for (int z = 0; z < 16; z++)
					{
						int paletteIndex = blocks[(((x * 16) + y) * 16) + z];
						var paletteEntry = palette[paletteIndex];
						int blockId = BedrockBlocks.GetId(paletteEntry.Name);
						chunk.Blocks[(((x * 256) + (y + yOffset)) * 16) + z] = blockId == -1 ? BedrockBlocks.AirId : blockId + paletteEntry.Data;
					}
				}
			}
		}

		foreach (var entity in model.Entities)
		{
			int2 chunkPos = ChunkUtils.BlockToChunk((int)entity.Position.X, (int)entity.Position.Z);

			if (!entityChunks.TryGetValue(chunkPos, out var chunk))
			{
				chunk = new EntityChunk(chunkPos.X, chunkPos.Y);
				entityChunks.Add(chunkPos, chunk);
			}

			chunk.Entities.Add(entity);
		}

		if (model.BlockEntities is not null)
		{
			foreach (var blockEntity in model.BlockEntities)
			{
				JsonNbtConverter.CompoundJsonNbtTag? jsonMap = blockEntity.Data as JsonNbtConverter.CompoundJsonNbtTag;

				if (jsonMap is null)
				{
					options.Logger.Warning($"Invalid block entity: Data wasn't compound tag.");
					continue;
				}

				var map = jsonMap.Value;

				if (!map.TryGetValue("x", out var xTag) || !map.TryGetValue("y", out var yTag) || !map.TryGetValue("z", out var zTag) || !map.TryGetValue("id", out var idTag))
				{
					options.Logger.Warning($"Invalid block entity: Missing x, y, z or id tag(s).");
					continue;
				}

				if (xTag is not JsonNbtConverter.IntJsonNbtTag xInt || blockEntity.Position.X != xInt.Value)
				{
					options.Logger.Warning($"Invalid block entity: x tags's value doesn't match x position.");
					continue;
				}
				else if (yTag is not JsonNbtConverter.IntJsonNbtTag yInt || blockEntity.Position.Y != yInt.Value)
				{
					options.Logger.Warning($"Invalid block entity: y tags's value doesn't match y position.");
					continue;
				}
				else if (zTag is not JsonNbtConverter.IntJsonNbtTag zInt || blockEntity.Position.Z != zInt.Value)
				{
					options.Logger.Warning($"Invalid block entity: z tags's value doesn't match z position.");
					continue;
				}
				else if (idTag is not JsonNbtConverter.StringJsonNbtTag)
				{
					options.Logger.Warning($"Invalid block entity: id tags's value must be a string.");
					continue;
				}

				int2 chunkPos = ChunkUtils.BlockToChunk(blockEntity.Position.X, blockEntity.Position.Z);
				if (!blockChunks.TryGetValue(chunkPos, out var chunk))
				{
					chunk = new BlockChunk(chunkPos.X, chunkPos.Y);
					blockChunks.Add(chunkPos, chunk);
				}

				chunk.BlockEntities.Add(JsonNbtConverter.Convert(jsonMap));
			}
		}

		options.Logger.Information($"Converting blocks");
		foreach (var (pos, chunk) in blockChunks)
		{
			worldData.AddNBTToRegion(pos.X, pos.Y, "region", chunk.ToTag(options.Biome, true, options.Logger));
		}

		options.Logger.Information($"Converting entities");
		foreach (var (pos, chunk) in entityChunks)
		{
			worldData.AddNBTToRegion(pos.X, pos.Y, "entities", chunk.ToTag(options.ConvertTarget, options.Logger));
		}

		task?.Increment(1);

		options.Logger.Information($"Filling region files with empty chunks");
		await FillWithAirChunks(worldData, task, buildplate.Offset.Y, options.Biome, options.Logger).ConfigureAwait(false);

		task?.Increment(1);

		switch (options.ConvertTarget)
		{
			case ConvertTarget.Java:
				Java.AddJavaFiles(worldData, model.IsNight, options);
				break;
			case ConvertTarget.Vienna:
				Vienna.AddViennaFiles(worldData, buildplate, model.IsNight, options);
				break;
			default:
				Debug.Fail($"Unknown {nameof(ConvertTarget)} '{options.ConvertTarget}'");
				break;
		}

		task?.Increment(1);

		return worldData;
	}

	public static void InitRegistry(ILogger logger)
	{
		if (registryInitialized)
		{
			return;
		}

		logger.Information("[registry] Initializing");
		BedrockBlocks.Load(JsonSerializer.Deserialize<JsonArray>(ReadFile("blocks_bedrock.json"))!);

		JavaBlocks.Load(
			JsonSerializer.Deserialize<JsonArray>(ReadFile("blocks_java.json"))!,
			JsonSerializer.Deserialize<JsonArray>(ReadFile("blocks_java_nonvanilla.json"))!);

		logger.Information("[registry] Initialization finished");

		registryInitialized = true;

		string ReadFile(string fileName)
		{
			return File.ReadAllText(Path.Combine("Data", fileName));
		}
	}

	[GeneratedRegex(@"^region/r\.-?\d+\.-?\d+\.mca$")]
	private static partial Regex RegionFileRegex();

	private static int CalculateLowestY(BuildplateModel model)
	{
		int lowestY = int.MaxValue;
		int lowestChunkY = model.SubChunks.Min(chunk => chunk.Position.Y);
		foreach (var subChunk in model.SubChunks.Where(chunk => chunk.Position.Y == lowestChunkY))
		{
			int[] blocks = subChunk.Blocks;
			List<PaletteEntry> palette = subChunk.BlockPalette;

			int yOffset = subChunk.Position.Y * 16;

			for (int y = 0; y < 16; y++)
			{
				for (int x = 0; x < 16; x++)
				{
					for (int z = 0; z < 16; z++)
					{
						int paletteIndex = blocks[(((x * 16) + y) * 16) + z];
						var paletteEntry = palette[paletteIndex];

						switch (paletteEntry.Name)
						{
							case "minecraft:air":
							case "minecraft:invisible_constraint":
								break;
							default:
								if (y + yOffset < lowestY)
								{
									lowestY = y + yOffset;
									goto next;
								}
								else
								{
									goto next;
								}
						}
					}
				}
			}

		next:;
		}

		return lowestY;
	}

	private static void AddSolidAir(Buildplate buildplate, BuildplateModel model, int lowestY)
	{
		int surfaceY = buildplate.Offset.Y - 1;
		int xOffset = (buildplate.Dimension.X / 2) + 1;
		int zOffset = (buildplate.Dimension.Z / 2) + 1;

		int minX = ChunkToMinBlock(model.SubChunks.Min(chunk => chunk.Position.X));
		int maxX = ChunkToMaxBlock(model.SubChunks.Max(chunk => chunk.Position.X));
		int minY = ChunkToMinBlock(model.SubChunks.Min(chunk => chunk.Position.Y));
		int minZ = ChunkToMinBlock(model.SubChunks.Min(chunk => chunk.Position.Z));
		int maxZ = ChunkToMaxBlock(model.SubChunks.Max(chunk => chunk.Position.Z));

		Fill(new int3(minX, minY, minZ), new int3(maxX, lowestY - 2, maxZ), 0, "fountain:solid_air");

		Fill(new int3(minX, lowestY - 1, minZ), new int3(maxX, surfaceY, -zOffset - 4), 0, "fountain:solid_air");
		Fill(new int3(minX, lowestY - 1, zOffset + 3), new int3(maxX, surfaceY, maxZ), 0, "fountain:solid_air");
		Fill(new int3(minX, lowestY - 1, minZ), new int3(-xOffset - 4, surfaceY, maxZ), 0, "fountain:solid_air");
		Fill(new int3(xOffset + 3, lowestY - 1, minZ), new int3(maxX, surfaceY, maxZ), 0, "fountain:solid_air");

		void Fill(int3 from, int3 to, ushort data, string name)
		{
			int3 fromChunk = ChunkDivVec(from, 16);
			int3 toChunk = ChunkDivVec(to, 16);

			int3 fromInChunk = new int3(from.X & 15, from.Y & 15, from.Z & 15);
			int3 toInChunk = new int3(to.X & 15, to.Y & 15, to.Z & 15);

			for (int chunkY = fromChunk.Y; chunkY <= toChunk.Y; chunkY++)
			{
				for (int chunkZ = fromChunk.Z; chunkZ <= toChunk.Z; chunkZ++)
				{
					for (int chunkX = fromChunk.X; chunkX <= toChunk.X; chunkX++)
					{
						int3 chunkPos = new int3(chunkX, chunkY, chunkZ);
						var chunk = model.SubChunks.FirstOrDefault(chunk => chunk.Position == chunkPos);

						if (chunk is null)
						{
							continue;
						}

						int index = chunk.BlockPalette.IndexOf(new PaletteEntry(data, name));

						if (index == -1)
						{
							index = chunk.BlockPalette.Count;
							chunk.BlockPalette.Add(new PaletteEntry(data, name));
						}

						int xMax = chunkPos.X == toChunk.X ? toInChunk.X : 15;
						int yMax = chunkPos.Y == toChunk.Y ? toInChunk.Y : 15;
						int zMax = chunkPos.Z == toChunk.Z ? toInChunk.Z : 15;

						for (int x = chunkPos.X == fromChunk.X ? fromInChunk.X : 0; x <= xMax; x++)
						{
							for (int y = chunkPos.Y == fromChunk.Y ? fromInChunk.Y : 0; y <= yMax; y++)
							{
								for (int z = chunkPos.Z == fromChunk.Z ? fromInChunk.Z : 0; z <= zMax; z++)
								{
									chunk.Blocks[(((x * 16) + y) * 16) + z] = index;
								}
							}
						}
					}
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		int3 ChunkDivVec(int3 a, int b)
		{
			return new int3(ChunkDiv(a.X, b), ChunkDiv(a.Y, b), ChunkDiv(a.Z, b));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		int ChunkDiv(int a, int b)
		{
			return (a >= 0) ? a / b : ((a + 1) / b) - 1;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		int ChunkToMinBlock(int pos)
		{
			return pos * 16;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		int ChunkToMaxBlock(int pos)
		{
			return (pos * 16) + 15;
		}
	}

	private static async Task FillWithAirChunks(WorldData worldData, ProgressTask? task, int groundPos, string biome, ILogger logger)
	{
		int numbRegionFiles = 0;
		foreach (string path in worldData.Files.Keys)
		{
			if (RegionFileRegex().IsMatch(path))
			{
				numbRegionFiles++;
			}
		}

		if (task is not null)
		{
			task.MaxValue += numbRegionFiles;
		}

		BlockChunk emptyChunk = new BlockChunk(0, 0);

		// 16x256x16
		// (x * 256 + y) * 16 + z
		for (int x = 0, index = 0; x < 16; x++, index += 16 * 256)
		{
			Array.Fill(emptyChunk.Blocks, BlockChunk.SolidAirId, index, groundPos * 16);
			Array.Fill(emptyChunk.Blocks, BedrockBlocks.AirId, index + (groundPos * 16), (256 - groundPos) * 16);
		}

		CompoundTag chunkTag = emptyChunk.ToTag(biome, false, logger);

		await Parallel.ForEachAsync(worldData.Files.Keys, ParallelUtils.DefaultOptions, (path, _) =>
		{
			if (!RegionFileRegex().IsMatch(path))
			{
				return ValueTask.CompletedTask;
			}

			using MemoryStream chunkDataStream = new MemoryStream(2048);
			using TagWriter writer = new TagWriter(chunkDataStream, FormatOptions.Java);

			// for some reason if the name is empty, the type doesn't get written... wtf, also in this case an empty name is expected
			// compound type
			chunkDataStream.WriteByte(10);

			// name length
			Debug.Assert(string.IsNullOrEmpty(chunkTag.Name), $"{nameof(chunkTag)}.Name should be null or empty.");
			chunkDataStream.WriteByte(0);
			chunkDataStream.WriteByte(0);

			writer.WriteTag(chunkTag);

			Span<byte> chunkData = chunkDataStream.ToArray();

			using MemoryStream compressedStream = new MemoryStream(RegionUtils.ChunkSize);

			ref byte[] data = ref CollectionsMarshal.GetValueRefOrNullRef(worldData.Files, path);
			Debug.Assert(!Unsafe.IsNullRef(ref data), $"{nameof(data)} shouldn't be null.");

			int2 regionPos = RegionPathToPos(path) * RegionUtils.RegionSize;

			int numbChunksToAdd = 0;

			for (int z = 0; z < RegionUtils.RegionSize; z++)
			{
				for (int x = 0; x < RegionUtils.RegionSize; x++)
				{
					if (!RegionUtils.ContainsChunk(data, x, z))
					{
						numbChunksToAdd++;
					}
				}
			}

			int index = data.Length;
			Array.Resize(ref data, data.Length + (numbChunksToAdd * RegionUtils.ChunkSize));

			if (numbChunksToAdd == 0)
			{
				return ValueTask.CompletedTask;
			}

			for (int z = 0; z < RegionUtils.RegionSize; z++)
			{
				for (int x = 0; x < RegionUtils.RegionSize; x++)
				{
					if (RegionUtils.ContainsChunk(data, x, z))
					{
						continue;
					}

					// to not have to write the tag every time, when only the chunk position changes, it's written only once and the position is changed in the serialized buffer, ugly but faster
					BinaryPrimitives.WriteInt32BigEndian(chunkData[10..], regionPos.X + x);
					BinaryPrimitives.WriteInt32BigEndian(chunkData[21..], regionPos.Y + z);

					compressedStream.SetLength(0);
					using ZLibStream zlibStream = new ZLibStream(compressedStream, CompressionLevel.Optimal, true); // TODO: measure how much faster this is than CompressionLevel.SmallestSize
					zlibStream.Write(chunkData);
					zlibStream.Flush();

					int paddedLength = RegionUtils.GetPaddedLength((int)compressedStream.Length);

					if (index + paddedLength > data.Length)
					{
						Array.Resize(ref data, data.Length + Math.Max(paddedLength, RegionUtils.RegionSize * 8));
					}

					RegionUtils.WriteRawChunkData(data, compressedStream, index, RegionUtils.CompressionTypeZlib, x, z);

					index += paddedLength;
				}
			}

			task?.Increment(1);

			return ValueTask.CompletedTask;
		}).ConfigureAwait(false);
	}

	#region Helpers
	private static int2 RegionPathToPos(ReadOnlySpan<char> path)
	{
		Debug.Assert(RegionFileRegex().IsMatch(path), $"{nameof(path)} should corespond to a region file.");

		Debug.Assert(path.StartsWith("region/"), $"{nameof(path)} should start with 'region/'");
		path = path[7..];

		Debug.Assert(!path.Contains('/'), $"{nameof(path)} shouldn't contain '/' at this point.");
		Debug.Assert(path.StartsWith("r."), $"{nameof(path)} should start with 'r.' at this point.");
		path = path[2..];

		int dotIndex = path.IndexOf('.');
		Debug.Assert(dotIndex != -1, $"{nameof(path)} should contain '.' at this point.");
		int regionX = int.Parse(path[..dotIndex]);
		path = path[(dotIndex + 1)..];

		dotIndex = path.IndexOf('.');
		Debug.Assert(dotIndex != -1, $"{nameof(path)} should contain '.' at this point.");
		int regionZ = int.Parse(path[..dotIndex]);

		return new int2(regionX, regionZ);
	}
	#endregion

	public sealed class Options
	{
		public Options(ILogger logger, ConvertTarget exportTarget, string biome, string worldName)
		{
			Logger = logger;
			ConvertTarget = exportTarget;
			Biome = biome;
			WorldName = worldName;
		}

		public ILogger Logger { get; }

		public ConvertTarget ConvertTarget { get; }

		public string Biome { get; }

		public string WorldName { get; }
	}
}
