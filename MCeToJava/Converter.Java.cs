// <copyright file="Converter.Java.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using MCeToJava.Utils;
using SharpNBT;
using System.Diagnostics;
using System.IO.Compression;

namespace MCeToJava;

internal static partial class Converter
{
	private static class Java
	{
		public static void AddJavaFiles(WorldData worldData, bool night, Options options)
		{
			options.Logger.Information($"Creating level.dat");

			using (MemoryStream ms = new MemoryStream())
			using (GZipStream gzs = new GZipStream(ms, CompressionLevel.Optimal))
			using (TagWriter writer = new TagWriter(gzs, FormatOptions.Java))
			{
				var tag = CreateLevelDat(false, night, options.Biome, options.WorldName);

				// for some reason if the name is empty, the type doesn't get written... wtf, also in this case an empty name is expected
				// compound type
				gzs.WriteByte(10);

				// name length
				Debug.Assert(string.IsNullOrEmpty(tag.Name), "Tag name should be null or empty.");
				gzs.WriteByte(0);
				gzs.WriteByte(0);

				writer.WriteTag(tag);
				gzs.Flush();

				ms.Position = 0;
				worldData.Files.Add("level.dat", ms.ToArray());
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
				.Put("GameRules", new NbtBuilder.Compound()
					.Put("doDaylightCycle", "false")
					.Put("doWeatherCycle", "false")
					.Put("doMobSpawning", "false")
					.Put("keepInventory", "true"))
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
									.Put("biome", biome))))))
				.Put("DataVersion", 3700)
				.Put("version", 19133)
				.Put("Version", new NbtBuilder.Compound()
					.Put("Id", 3700)
					.Put("Name", "1.20.4")
					.Put("Series", "main")
					.Put("Snapshot", (byte)0))
				.Put("initialized", (byte)1)
				.Build("Data");

			return new CompoundTag(null)
			{
				["Data"] = dataTag,
			};
		}
	}
}
