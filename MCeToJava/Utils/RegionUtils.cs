using MathUtils.Vectors;
using SharpNBT;
using System.Buffers.Binary;
using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.CompilerServices;

namespace MCeToJava.Utils;

internal static class RegionUtils
{
	public const int RegionSize = 32;

	public const int TimestampOffset = 0x1000;
	public const int HeaderLength = 0x1000 + 0x1000;
	public const int ChunkSize = 0x1000;

	public const byte CompressionTypeGzip = 1;
	public const byte CompressionTypeZlib = 2;
	public const byte CompressionTypeNone = 3;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int2 ChunkToRegion(int chunkX, int chunkZ)
		=> new int2(chunkX >> 5, chunkZ >> 5);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int2 ChunkToLocal(int chunkX, int chunkZ)
		=> new int2(chunkX & 31, chunkZ & 31);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int LocalToIndex(int localX, int localZ)
		=> (localZ << 5) | localX;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int GetPaddedLength(int chunkDataLength)
	{
		chunkDataLength += 5; // header
		return chunkDataLength % ChunkSize == 0 ? chunkDataLength : chunkDataLength + (ChunkSize - (chunkDataLength % ChunkSize));
	}

	public static bool ContainsChunk(Span<byte> regionData, int localX, int localZ)
	{
		ValidateLocalCoords(localX, localZ);

		int chunkIndex = LocalToIndex(localX, localZ);

		int offset = BinaryPrimitives.ReadInt32BigEndian(regionData[(chunkIndex * 4)..]) >> 8;

		return offset >= 2;
	}

	public static Memory<byte> ReadRawChunkData(Memory<byte> regionData, int localX, int localZ, out byte compressionType)
	{
		ValidateLocalCoords(localX, localZ);

		var dataSpan = regionData.Span;

		Debug.Assert(ContainsChunk(dataSpan, localX, localZ));

		int chunkIndex = LocalToIndex(localX, localZ);

		int offset = (BinaryPrimitives.ReadInt32BigEndian(dataSpan[(chunkIndex * 4)..]) >> 8) * ChunkSize;

		int length = BinaryPrimitives.ReadInt32BigEndian(dataSpan[offset..]);
		compressionType = dataSpan[offset + 4];

		return regionData.Slice(offset + 5, length);
	}

	/// <exception cref="InvalidDataException"></exception>
	public static MemoryStream ReadChunkData(Memory<byte> regionData, int localX, int localZ)
	{
		ValidateLocalCoords(localX, localZ);

		Memory<byte> chunkData = ReadRawChunkData(regionData, localX, localZ, out byte compressionType);

		MemoryStream uncompressed;

		switch (compressionType)
		{
			case CompressionTypeGzip:
				{
					uncompressed = new MemoryStream(chunkData.Length * 2);

					using GZipStream gZipStream = new GZipStream(new SpanStream(chunkData), CompressionMode.Decompress, false);
					gZipStream.CopyTo(uncompressed);
				}

				break;
			case CompressionTypeZlib:
				{
					uncompressed = new MemoryStream(chunkData.Length * 2);

					using ZLibStream deflateStream = new ZLibStream(new SpanStream(chunkData), CompressionMode.Decompress, false);
					deflateStream.CopyTo(uncompressed);
				}

				break;
			case CompressionTypeNone:
				{
					byte[] buffer = new byte[chunkData.Length];
					chunkData.CopyTo(buffer.AsMemory());
					uncompressed = new MemoryStream(buffer);
					break;
				}
			default:
				throw new InvalidDataException($"Invalid/unknown compression type '{compressionType}'.");
		}

		return uncompressed;
	}

	/// <exception cref="InvalidDataException"></exception>
	public static CompoundTag ReadChunkNTB(Memory<byte> regionData, int localX, int localZ)
	{
		ValidateLocalCoords(localX, localZ);

		using (MemoryStream ms = ReadChunkData(regionData, localX, localZ))
		using (TagReader tagReader = new TagReader(ms, FormatOptions.Java))
		{
			CompoundTag tag = tagReader.ReadTag<CompoundTag>();

			return tag;
		}
	}

	public static void WriteRawChunkData(Span<byte> regionData, Stream chunkData, int index, byte compressionType, int localX, int localZ)
	{
		ValidateLocalCoords(localX, localZ);

		Debug.Assert(chunkData.CanRead);
		Debug.Assert(chunkData.CanSeek);
		Debug.Assert(index % ChunkSize == 0);
		Debug.Assert(index / ChunkSize >= 2);

		int chunkIndex = LocalToIndex(localX, localZ);

		int dataLength = checked((int)chunkData.Length);
		Debug.Assert(index + dataLength + 5 <= regionData.Length);
		int paddedLength = GetPaddedLength(dataLength);

		BinaryPrimitives.WriteInt32BigEndian(regionData[(chunkIndex * 4)..], ((index / ChunkSize) << 8) | paddedLength / ChunkSize);
		BinaryPrimitives.WriteInt32BigEndian(regionData[(chunkIndex * 4 + TimestampOffset)..], (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds());

		BinaryPrimitives.WriteInt32BigEndian(regionData[index..], dataLength);
		regionData[index + 4] = compressionType;

		chunkData.Position = 0;
		chunkData.Read(regionData.Slice(index + 5, dataLength));
	}

	public static void WriteChunkNBT(ref byte[] regionData, CompoundTag chunkNBT, int localX, int localZ)
	{
		ValidateLocalCoords(localX, localZ);

		using MemoryStream ms = new MemoryStream();
		using ZLibStream zlib = new ZLibStream(ms, CompressionLevel.SmallestSize);
		using TagWriter writer = new TagWriter(zlib, FormatOptions.Java);

		// for some reason if the name is empty, the type doesn't get written... wtf, also in this case an empty name is expected
		// compound type
		zlib.WriteByte(10);

		// name length
		Debug.Assert(string.IsNullOrEmpty(chunkNBT.Name));
		zlib.WriteByte(0);
		zlib.WriteByte(0);

		writer.WriteTag(chunkNBT);
		zlib.Flush();

		int dataLength = checked((int)ms.Length);
		int paddedLength = GetPaddedLength(dataLength);

		int index;
		if (regionData.Length == 0)
		{
			regionData = new byte[HeaderLength + paddedLength];
			index = HeaderLength;
		}
		else
		{
			byte[] newRegionData = new byte[regionData.Length + paddedLength];
			Buffer.BlockCopy(regionData, 0, newRegionData, 0, regionData.Length);

			index = regionData.Length;

			regionData = newRegionData;
		}

		WriteRawChunkData(regionData, ms, index, CompressionTypeZlib, localX, localZ);
	}

	[Conditional("DEBUG")]
	private static void ValidateLocalCoords(int localX, int localZ)
	{
		Debug.Assert(localX >= 0 && localX < RegionSize);
		Debug.Assert(localZ >= 0 && localZ < RegionSize);
	}
}
