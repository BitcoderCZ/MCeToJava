using MCeToJava.Utils;
using SharpNBT;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCeToJava
{
	internal sealed class WorldData
	{
		private const int TimestampOffset = 1024 * 4;
		private const int HeaderLength = (1024 * 4) + (1024 * 4);

		private readonly Dictionary<string, byte[]> _files = [];

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
					_files.Add(entry.FullName, ms.ToArray());
				}
			}
		}

		// https://minecraft.wiki/w/Region_file_format
		public void AddChunk(int x, int z, CompoundTag tag)
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
				writer.WriteTag(tag);

				int dataLength = (int)ms.Length;
				int totalLength = dataLength + 5;
				int paddedLength = totalLength % 0x2000 == 0 ? totalLength : totalLength + (0x2000 - (totalLength % 0x2000));

				int index;
				if (_files.TryGetValue(fileName, out var bytes))
				{
					byte[] newBytes = new byte[bytes.Length + paddedLength];
					Buffer.BlockCopy(bytes, 0, newBytes, 0, bytes.Length);

					index = bytes.Length;

					bytes = newBytes;
					_files[fileName] = bytes;
				}
				else
				{
					bytes = new byte[HeaderLength + paddedLength];
					index = HeaderLength;

					_files.Add(fileName, bytes);
				}

				int indexInHeader = chunkIndex * 4;

				int headerIndex = index / 0x2000;
				bytes[indexInHeader + 2] = (byte)((headerIndex >> 0) & byte.MaxValue);
				bytes[indexInHeader + 1] = (byte)((headerIndex >> 8) & byte.MaxValue);
				bytes[indexInHeader + 0] = (byte)((headerIndex >> 24) & byte.MaxValue);

				bytes[indexInHeader + 3] = (byte)(paddedLength / 0x2000);

				BinaryPrimitives.WriteInt32BigEndian(bytes[(indexInHeader + TimestampOffset)..], (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds());

				BinaryPrimitives.WriteInt32BigEndian(bytes[index..], dataLength);
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

			using MemoryStream ms = new MemoryStream(_files[$"region/r.{regionX}.{regionZ}.mca"]);
			using BinaryReader reader = new BinaryReader(ms);

			ms.Seek(chunkIndex * 4, SeekOrigin.Begin);
			int offset = (int)(reader.ReadUInt32BE() >> 8);

			ms.Seek(offset * 4096, SeekOrigin.Begin);

			int length = (int)reader.ReadUInt32BE();
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

		public ZipArchive GetAsZip()
		{
			throw new NotImplementedException();
		}
	}
}
