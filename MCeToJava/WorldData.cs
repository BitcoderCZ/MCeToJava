using MathUtils.Vectors;
using MCeToJava.Utils;
using SharpNBT;
using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MCeToJava;

internal sealed class WorldData
{
	public readonly Dictionary<string, byte[]> Files = [];

	public readonly
#if NET9_0_OR_GREATER
		Lock
#else
		object
#endif
		FilesLock = new();

	public WorldData()
	{
	}

	public WorldData(Stream inputStream)
	{
		using ZipArchive archive = new ZipArchive(inputStream);

		foreach (var entry in archive.Entries)
		{
			if (entry.IsDirectory())
			{
				continue;
			}

			using (Stream entryStream = entry.Open())
			using (MemoryStream ms = new MemoryStream())
			{
				entryStream.CopyTo(ms);
				Files.Add(entry.FullName, ms.ToArray());
			}
		}
	}

	// https://minecraft.wiki/w/Region_file_format
	public void AddChunkNBT(int x, int z, CompoundTag tag)
	{
		int2 region = RegionUtils.ChunkToRegion(x, z);
		int2 local = RegionUtils.ChunkToLocal(x, z);

		string fileName = $"region/r.{region.X}.{region.Y}.mca";

		ref byte[] bytes = ref Unsafe.NullRef<byte[]>();

		lock (FilesLock)
		{
			if (!Files.ContainsKey(fileName))
			{
				Files.Add(fileName, []);
			}

			bytes = ref CollectionsMarshal.GetValueRefOrNullRef(Files, fileName);
		}

		Debug.Assert(!Unsafe.IsNullRef(ref bytes));

		RegionUtils.WriteChunkNBT(ref bytes, tag, local.X, local.Y);
	}

	public CompoundTag GetChunkNBT(int x, int z)
	{
		int2 region = RegionUtils.ChunkToRegion(x, z);
		int2 local = RegionUtils.ChunkToLocal(x, z);

		lock (FilesLock)
		{
			return RegionUtils.ReadChunkNTB(Files[$"region/r.{region.X}.{region.Y}.mca"], local.X, local.Y);
		}
	}

	public void WriteToStream(Stream stream)
	{
		using ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create, true);

		lock (FilesLock)
		{
			foreach (var (path, data) in Files)
			{
				var entry = archive.CreateEntry(path, CompressionLevel.SmallestSize);
				using var entryStream = entry.Open();
				entryStream.Write(data);
			}
		}
	}
}
