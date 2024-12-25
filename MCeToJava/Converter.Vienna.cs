// <copyright file="Converter.Vienna.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using MathUtils.Vectors;
using MCeToJava.Models.MCE;
using MCeToJava.Models.Vienna;
using MCeToJava.Utils;
using System.Buffers;
using System.Text;

namespace MCeToJava;

internal static partial class Converter
{
	private static class Vienna
	{
		public static void AddViennaFiles(WorldData worldData, Buildplate buildplate, string name, Options options)
		{
			options.Logger.Information($"[{name}] Creating buildplate_metadata.json");

			worldData.Files.Add("buildplate_metadata.json", Encoding.UTF8.GetBytes(JsonUtils.SerializeJson(new BuildplateMetadata(
				1,
				Math.Max(buildplate.Dimension.X, buildplate.Dimension.Z),
				buildplate.Offset.Y,
				options.Night))));

			int fileCount = worldData.Files.Count;
			string[] keys = ArrayPool<string>.Shared.Rent(fileCount);

			try
			{
				worldData.Files.Keys.CopyTo(keys, 0);

				for (int i = 0; i < fileCount; i++)
				{
					string path = keys[i];

					if (RegionFileRegex().IsMatch(path))
					{
						int2 regionPos = RegionPathToPos(path);
						worldData.Files.Add($"entities/r.{regionPos.X}.{regionPos.Y}.mca", []);
					}
				}
			}
			finally
			{
				ArrayPool<string>.Shared.Return(keys);
			}
		}
	}
}
