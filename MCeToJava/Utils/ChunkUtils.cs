// <copyright file="ChunkUtils.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using MathUtils.Vectors;
using System.Runtime.CompilerServices;

namespace MCeToJava.Utils;

internal static class ChunkUtils
{
	public const int Width = 16;
	public const int Height = 256;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int2 BlockToChunk(int blockX, int blockZ)
	=> new int2(blockX >> 4, blockZ >> 4);
}
