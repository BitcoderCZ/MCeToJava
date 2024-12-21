using MathUtils.Vectors;
using MCeToJava.Exceptions;
using MCeToJava.Models;
using MCeToJava.Models.MCE;
using MCeToJava.Models.Vienna;
using MCeToJava.Registry;
using MCeToJava.Utils;
using Serilog;
using SharpNBT;
using System.Buffers;
using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace MCeToJava;

internal static partial class Converter
{
	[GeneratedRegex(@"^region/r\.-?\d+\.-?\d+\.mca$")]
	private static partial Regex RegionFileRegex();

	public static WorldData Convert(Buildplate buildplate, ExportTarget exportTarget, string biome, bool night, string worldName)
	{
		BuildplateModel? model = U.DeserializeJson<BuildplateModel>(System.Convert.FromBase64String(buildplate.Model));

		if (model is null)
		{
			throw new ConvertException("Invalid json - buildplate is null.");
		}

		if (model.FormatVersion != 1)
		{
			throw new ConvertException($"Unsupported version '{model.FormatVersion}', only version 1 is supported.");
		}

		WorldData worldData = new WorldData();

		int lowestY = CalculateLowestY(model);
		if (lowestY == int.MaxValue)
		{
			Log.Error("Failed to calculate lowest y position.");
		}
		else
		{
			AddSolidAir(buildplate, model, lowestY);
		}

		Log.Information("Writing chunks");
		Dictionary<int2, Chunk> chunks = [];

		foreach (var subChunk in model.SubChunks)
		{
			Chunk chunk = chunks.ComputeIfAbsent(new int2(subChunk.Position.X, subChunk.Position.Z), pos => new Chunk(pos.X, pos.Y))!;

			Debug.Assert(subChunk.Position.Y >= 0);

			var blocks = subChunk.Blocks;
			var palette = subChunk.BlockPalette;

			int yOffset = subChunk.Position.Y * 16;
			for (int x = 0; x < 16; x++)
			{
				for (int y = 0; y < 16; y++)
				{
					for (int z = 0; z < 16; z++)
					{
						int paletteIndex = blocks[(x * 16 + y) * 16 + z];
						var paletteEntry = palette[paletteIndex];
						int blockId = BedrockBlocks.GetId(paletteEntry.Name);
						chunk.Blocks[(x * 256 + (y + yOffset)) * 16 + z] = blockId == -1 ? BedrockBlocks.AIR : blockId + paletteEntry.Data;
					}
				}
			}
		}

		Log.Information("Converting chunks");
		foreach (var (pos, chunk) in chunks)
		{
			worldData.AddChunkNBT(pos.X, pos.Y, chunk.ToTag());
		}

		Log.Information("Filling regin files with empty chunks");
		FillWithAirChunks(worldData, buildplate.Offset.Y);

		switch (exportTarget)
		{
			case ExportTarget.Java:
				Log.Information("Creating level.dat");

				using (MemoryStream ms = new MemoryStream())
				using (GZipStream gzs = new GZipStream(ms, CompressionLevel.Optimal))
				using (TagWriter writer = new TagWriter(gzs, FormatOptions.Java))
				{
					var tag = CreateLevelDat(false, night, biome, worldName);

					// for some reason if the name is empty, the type doesn't get written... wtf, also in this case an empty name is expected
					// compound type
					gzs.WriteByte(10);

					// name length
					Debug.Assert(string.IsNullOrEmpty(tag.Name));
					gzs.WriteByte(0);
					gzs.WriteByte(0);

					writer.WriteTag(tag);
					gzs.Flush();

					ms.Position = 0;
					worldData.Files.Add("level.dat", ms.ToArray());
				}
				break;
			case ExportTarget.Vienna:
				Log.Information("Creating buildplate_metadata.json");

				worldData.Files.Add("buildplate_metadata.json", Encoding.UTF8.GetBytes(U.SerializeJson(new BuildplateMetadata(
					1,
					Math.Max(buildplate.Dimension.X, buildplate.Dimension.Z),
					buildplate.Offset.Y,
					night
				))));

				int fileCount = worldData.Files.Count;
				var keys = ArrayPool<string>.Shared.Rent(fileCount);
				try
				{
					worldData.Files.Keys.CopyTo(keys, 0);

					for (int i = 0; i < fileCount; i++)
					{
						string path = keys[i];

						if (RegionFileRegex().IsMatch(path))
						{
							int2 regionPos = RegionPathToPos(path);
							worldData.Files.Add($"entities/r.{regionPos.X}.{regionPos.Y}.mca", Array.Empty<byte>());
						}
					}
				}
				finally
				{
					ArrayPool<string>.Shared.Return(keys);
				}
				break;
		}

		return worldData;
	}

	private static int CalculateLowestY(BuildplateModel model)
	{
		int lowestY = int.MaxValue;
		int lowestChunkY = model.SubChunks.Min(chunk => chunk.Position.Y);
		foreach (var subChunk in model.SubChunks.Where(chunk => chunk.Position.Y == lowestChunkY))
		{
			var blocks = subChunk.Blocks;
			var palette = subChunk.BlockPalette;

			int yOffset = subChunk.Position.Y * 16;

			for (int y = 0; y < 16; y++)
			{
				for (int x = 0; x < 16; x++)
				{
					for (int z = 0; z < 16; z++)
					{
						int paletteIndex = blocks[(x * 16 + y) * 16 + z];
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

		next: { }
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
									chunk.Blocks[(x * 16 + y) * 16 + z] = index;
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
			return (a >= 0) ? a / b : (a + 1) / b - 1;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		int ChunkToMinBlock(int pos)
		{
			return pos * 16;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		int ChunkToMaxBlock(int pos)
		{
			return pos * 16 + 15;
		}
	}

	private static CompoundTag CreateLevelDat(bool survival, bool night, string biome, string worldName)
	{
		CompoundTag dataTag = new NbtBuilder.Compound()
			.Put("GameType", survival ? 0 : 1)
			.Put("Difficulty", 1)
			.Put("DayTime", !night ? 6000 : 18000)
			.Put("LevelName", worldName)
			.Put("LastPlayed", DateTimeOffset.UtcNow.ToUnixTimeSeconds())
			//.Put("MapFeatures", (byte)0)
			.Put("GameRules", new NbtBuilder.Compound()
				.Put("doDaylightCycle", "false")
				.Put("doWeatherCycle", "false")
				.Put("doMobSpawning", "false")
				.Put("keepInventory", "true")
			)
			.Put("WorldGenSettings", new NbtBuilder.Compound()
				.Put("seed", 0L)    // TODO
				.Put("generate_features", (byte)0)
				.Put("dimensions", new NbtBuilder.Compound()
					.Put("minecraft:overworld", new NbtBuilder.Compound()
						.Put("type", "minecraft:overworld")
						.Put("generator", new NbtBuilder.Compound()
							.Put("type", "minecraft:flat")
							.Put("settings", new NbtBuilder.Compound()
								.Put("layers", new NbtBuilder.List(TagType.Compound))
								.Put("biome", biome)
							)
						)
					)
				)
			)
			.Put("DataVersion", 3700)
			.Put("version", 19133)
			.Put("Version", new NbtBuilder.Compound()
				.Put("Id", 3700)
				.Put("Name", "1.20.4")
				.Put("Series", "main")
				.Put("Snapshot", (byte)0)
			)
			.Put("initialized", (byte)1)
			.Build("Data");

		CompoundTag tag = new CompoundTag(null);
		tag["Data"] = dataTag;
		return tag;
	}

	private static void FillWithAirChunks(WorldData worldData, int groundPos)
	{
		Chunk emptyChunk = new Chunk(0, 0);

		// 16x256x16
		// (x * 256 + y) * 16 + z
		for (int x = 0, index = 0; x < 16; x++, index += 16 * 256)
		{
			Array.Fill(emptyChunk.Blocks, Chunk.SolidAirId, index, groundPos * 16);
			Array.Fill(emptyChunk.Blocks, BedrockBlocks.AIR, index + groundPos * 16, (256 - groundPos) * 16);
		}

		CompoundTag chunkTag = emptyChunk.ToTag();

		foreach (string path in worldData.Files.Keys)
		{
			if (!RegionFileRegex().IsMatch(path))
			{
				continue;
			}

			ref byte[] data = ref CollectionsMarshal.GetValueRefOrNullRef(worldData.Files, path);
			Debug.Assert(!Unsafe.IsNullRef(ref data));

			int2 regionPos = RegionPathToPos(path) * RegionUtils.RegionSize;

			for (int z = 0; z < RegionUtils.RegionSize; z++)
			{
				for (int x = 0; x < RegionUtils.RegionSize; x++)
				{
					if (RegionUtils.ContainsChunk(data, x, z))
					{
						continue;
					}

					chunkTag["xPos"] = new IntTag("xPos", regionPos.X + x);
					chunkTag["zPos"] = new IntTag("zPos", regionPos.Y + z);

					RegionUtils.WriteChunkNBT(ref data, chunkTag, x, z);
				}
			}
		}
	}

	private static int2 RegionPathToPos(ReadOnlySpan<char> path)
	{
		Debug.Assert(RegionFileRegex().IsMatch(path));

		Debug.Assert(path.StartsWith("region/"));
		path = path[7..];

		Debug.Assert(!path.Contains('/'));
		Debug.Assert(path.StartsWith("r."));
		path = path[2..];

		int dotIndex = path.IndexOf('.');
		Debug.Assert(dotIndex != -1);
		int regionX = int.Parse(path[..dotIndex]);
		path = path[(dotIndex + 1)..];

		dotIndex = path.IndexOf('.');
		Debug.Assert(dotIndex != -1);
		int regionZ = int.Parse(path[..dotIndex]);

		return new int2(regionX, regionZ);
	}
}
