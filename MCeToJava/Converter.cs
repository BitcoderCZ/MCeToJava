using MathUtils.Vectors;
using MCeToJava.Exceptions;
using MCeToJava.Models.MCE;
using MCeToJava.Registry;
using MCeToJava.Utils;
using SharpNBT;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MCeToJava
{
	internal sealed class Converter
	{
		private const int CHUNK_RADIUS = 2;

		public static WorldData Convert(Buildplate buildplate)
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
							chunk.blocks[(x * 256 + (y + yOffset)) * 16 + z] = blockId == -1 ? BedrockBlocks.AIR : blockId + paletteEntry.Data;
						}
					}
				}
			}

			foreach (var (pos, chunk) in chunks)
			{
				worldData.AddChunk(pos.X, pos.Y, chunk.ToTag());
			}

			// only for testing
			using (MemoryStream ms = new MemoryStream()) 
			using (GZipStream gzs = new GZipStream(ms, CompressionLevel.Optimal))
			using (TagWriter writer = new TagWriter(gzs, FormatOptions.Java))
			{
				writer.WriteTag(createLevelDat(false, false));
				gzs.Flush();

				ms.Position = 0;
				worldData.Files.Add("level.dat", ms.ToArray());
			}

			return worldData;
		}

		private static CompoundTag createLevelDat(bool survival, bool night)
		{
			CompoundTag dataTag = new NbtBuilder.Compound()
				.Put("GameType", survival ? 0 : 1)
				.Put("Difficulty", 1)
				.Put("DayTime", !night ? 6000 : 18000)
				.Put("GameRules", new NbtBuilder.Compound()
						.Put("doDaylightCycle", "false")
						.Put("doWeatherCycle", "false")
						.Put("doMobSpawning", "false")
						.Put("fountain:doMobDespawn", "false")
						.Put("keepInventory", "true")
				)
				.Put("WorldGenSettings", new NbtBuilder.Compound()
						.Put("seed", 0L)    // TODO
						.Put("generate_features", (byte)0)
						.Put("dimensions", new NbtBuilder.Compound()
								.Put("minecraft:overworld", new NbtBuilder.Compound()
										.Put("type", "minecraft:overworld")
										.Put("generator", new NbtBuilder.Compound()
												.Put("type", "fountain:wrapper")
												.Put("buildplate", new NbtBuilder.Compound()
														.Put("ground_level", 63))
												.Put("inner", new NbtBuilder.Compound()
														.Put("type", "minecraft:noise")
														.Put("settings", "minecraft:overworld")
														.Put("biome_source", new NbtBuilder.Compound()
																.Put("type", "minecraft:multi_noise")
																.Put("preset", "minecraft:overworld")
														)
												)
										)
								)
								.Put("minecraft:the_nether", new NbtBuilder.Compound()
										.Put("type", "minecraft:the_nether")
										.Put("generator", new NbtBuilder.Compound()
												.Put("type", "fountain:wrapper")
												.Put("buildplate", new NbtBuilder.Compound()
														.Put("ground_level", 32))
												.Put("inner", new NbtBuilder.Compound()
														.Put("type", "minecraft:noise")
														.Put("settings", "minecraft:nether")
														.Put("biome_source", new NbtBuilder.Compound()
																.Put("type", "minecraft:fixed")
																.Put("biome", "minecraft:nether_wastes")
														)
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
	}
}
