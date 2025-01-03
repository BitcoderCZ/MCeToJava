﻿// <copyright file="RegionUtils.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

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
	public static uint CalculatePaddedLength(uint chunkDataLength)
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

		Debug.Assert(ContainsChunk(dataSpan, localX, localZ), $"{nameof(regionData)} should contain a chunk at {localX},{localZ}.");

		int chunkIndex = LocalToIndex(localX, localZ);

		int offset = (BinaryPrimitives.ReadInt32BigEndian(dataSpan[(chunkIndex * 4)..]) >> 8) * ChunkSize;

		int length = BinaryPrimitives.ReadInt32BigEndian(dataSpan[offset..]) - 1;
		compressionType = dataSpan[offset + 4];

		return regionData.Slice(offset + 5, length);
	}

	/// <exception cref="InvalidDataException">Thrown if the compression type is invalid.</exception>
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

	/// <exception cref="InvalidDataException">Thrown if the compression type is invalid.</exception>
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

	public static void WriteRawChunkData(Span<byte> regionData, Stream chunkData, uint index, byte compressionType, int localX, int localZ)
	{
		ValidateLocalCoords(localX, localZ);

		Debug.Assert(chunkData.CanRead, $"{nameof(chunkData)} should be readable.");
		Debug.Assert(chunkData.CanSeek, $"{nameof(chunkData)} should be seekable.");
		Debug.Assert(index % ChunkSize == 0, $"{nameof(index)} should be a multiple of {nameof(ChunkSize)}.");
		Debug.Assert(index / ChunkSize >= 2, $"{nameof(index)} should be greater than or equal to 2×{nameof(ChunkSize)}.");

		int chunkIndex = LocalToIndex(localX, localZ);

		uint dataLength = checked((uint)chunkData.Length);
		Debug.Assert(index + dataLength + 5 <= regionData.Length, $"There should be enough space in {nameof(regionData)} to fit {nameof(chunkData)} starting at {index}");
		uint paddedLength = CalculatePaddedLength(dataLength);

		BinaryPrimitives.WriteUInt32BigEndian(regionData[(chunkIndex * 4)..], ((index / ChunkSize) << 8) | paddedLength / ChunkSize);
		BinaryPrimitives.WriteUInt32BigEndian(regionData[((chunkIndex * 4) + TimestampOffset)..], (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds());

		BinaryPrimitives.WriteUInt32BigEndian(regionData[(int)index..], dataLength + 1);
		regionData[(int)index + 4] = compressionType;

		chunkData.Position = 0;
		chunkData.Read(regionData.Slice((int)index + 5, (int)dataLength));
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
		Debug.Assert(string.IsNullOrEmpty(chunkNBT.Name), $"{nameof(chunkNBT)}.Name should be null or empty.");
		zlib.WriteByte(0);
		zlib.WriteByte(0);

		writer.WriteTag(chunkNBT);
		zlib.Flush();

		uint dataLength = checked((uint)ms.Length);
		uint paddedLength = CalculatePaddedLength(dataLength);

		uint index;
		if (regionData.Length == 0)
		{
			regionData = new byte[HeaderLength + paddedLength];
			index = HeaderLength;
		}
		else
		{
			byte[] newRegionData = new byte[regionData.Length + paddedLength];
			Buffer.BlockCopy(regionData, 0, newRegionData, 0, regionData.Length);

			index = (uint)regionData.Length;

			regionData = newRegionData;
		}

		WriteRawChunkData(regionData, ms, index, CompressionTypeZlib, localX, localZ);
	}

	[Conditional("DEBUG")]
	private static void ValidateLocalCoords(int localX, int localZ)
	{
		Debug.Assert(localX >= 0 && localX < RegionSize, $"{nameof(localX)} must be in bounds.");
		Debug.Assert(localZ >= 0 && localZ < RegionSize, $"{nameof(localZ)} must be in bounds.");
	}
}
