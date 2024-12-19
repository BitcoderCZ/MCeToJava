using MCeToJava.Utils;
using SharpNBT;
using System.Buffers.Binary;
using System.Diagnostics;
using System.IO.Compression;

namespace MCeToJava
{
	internal sealed class WorldData
	{
		private const int TimestampOffset = 0x1000;
		private const int HeaderLength = 0x1000 + 0x1000;
		private const int ChunkSize = 0x1000;

		public readonly Dictionary<string, byte[]> Files = [];

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
			int regionX = x >> 5;
			int regionZ = z >> 5;
			int chunkX = x & 31;
			int chunkZ = z & 31;
			int chunkIndex = (chunkZ << 5) | chunkX;

			string fileName = $"region/r.{regionX}.{regionZ}.mca";

			using (MemoryStream ms = new MemoryStream())
			using (ZLibStream zlib = new ZLibStream(ms, CompressionLevel.SmallestSize))
			using (TagWriter writer = new TagWriter(zlib, FormatOptions.Java))
			{
				// for some reason if the name is empty, the type doesn't get written... wtf, also in this case an empty name is expected
				// compound type
				zlib.WriteByte(10);

				// name length
				Debug.Assert(string.IsNullOrEmpty(tag.Name));
				zlib.WriteByte(0);
				zlib.WriteByte(0);

				writer.WriteTag(tag);
				zlib.Flush();

				ms.Position = 0;

				int dataLength = (int)ms.Length;
				int totalLength = dataLength + 5;
				int paddedLength = totalLength % ChunkSize == 0 ? totalLength : totalLength + (ChunkSize - (totalLength % ChunkSize));

				int index;
				if (Files.TryGetValue(fileName, out var bytes))
				{
					byte[] newBytes = new byte[bytes.Length + paddedLength];
					Buffer.BlockCopy(bytes, 0, newBytes, 0, bytes.Length);

					index = bytes.Length;

					bytes = newBytes;
					Files[fileName] = bytes;
				}
				else
				{
					bytes = new byte[HeaderLength + paddedLength];
					index = HeaderLength;

					Files.Add(fileName, bytes);
				}

				int indexInHeader = chunkIndex * 4;

				int headerIndex = index / ChunkSize;
				bytes[indexInHeader + 2] = (byte)((headerIndex >> 0) & byte.MaxValue);
				bytes[indexInHeader + 1] = (byte)((headerIndex >> 8) & byte.MaxValue);
				bytes[indexInHeader + 0] = (byte)((headerIndex >> 24) & byte.MaxValue);

				bytes[indexInHeader + 3] = (byte)(paddedLength / ChunkSize);

				BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(indexInHeader + TimestampOffset), (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds());

				BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(index), dataLength);
				bytes[index + 4] = 2; // compression type, 2 - zlib

				ms.Read(bytes, index + 5, (int)ms.Length);
			}
		}

		public CompoundTag GetChunkNBT(int x, int z)
		{
			int regionX = x >> 5;
			int regionZ = z >> 5;
			int chunkX = x & 31;
			int chunkZ = z & 31;
			int chunkIndex = (chunkZ << 5) | chunkX;

			using MemoryStream ms = new MemoryStream(Files[$"region/r.{regionX}.{regionZ}.mca"]);
			using BinaryReader reader = new BinaryReader(ms);

			ms.Seek(chunkIndex * 4, SeekOrigin.Begin);
			int offset = (int)(reader.ReadUInt32BigEndian() >> 8);

			ms.Seek(offset * ChunkSize, SeekOrigin.Begin);

			int length = (int)reader.ReadUInt32BigEndian();
			byte compressionType = reader.ReadByte();
			byte[] compressed = new byte[length];
			ms.Read(compressed);
			byte[] uncompressed;
			switch (compressionType)
			{
				case 1:
					{
						using GZipStream gZipStream = new GZipStream(new MemoryStream(compressed), CompressionMode.Decompress, false);
						using MemoryStream resultStream = new MemoryStream();
						gZipStream.CopyTo(resultStream);
						uncompressed = resultStream.ToArray();
					}
					break;
				case 2:
					{
						using ZLibStream deflateStream = new ZLibStream(new MemoryStream(compressed), CompressionMode.Decompress, false);
						using MemoryStream resultStream = new MemoryStream();
						deflateStream.CopyTo(resultStream);
						uncompressed = resultStream.ToArray();
					}
					break;
				case 3:
					{
						uncompressed = compressed;
						break;
					}
				default:
					throw new IOException($"Invalid/unknown compression type {compressionType}.");
			}

			using (MemoryStream tagStream = new MemoryStream(uncompressed))
			using (TagReader tagReader = new TagReader(tagStream, FormatOptions.Java, false))
			{
				CompoundTag tag = tagReader.ReadTag<CompoundTag>();

				return tag;
			}
		}

		public void WriteToStream(Stream stream)
		{
			using ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create, true);

			foreach (var (path, data) in Files)
			{
				var entry = archive.CreateEntry(path, CompressionLevel.SmallestSize);
				using var entryStream = entry.Open();
				entryStream.Write(data);
			}
		}
	}
}
