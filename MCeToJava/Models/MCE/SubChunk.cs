// <copyright file="SubChunk.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using MathUtils.Vectors;
using System.Text.Json.Serialization;

namespace MCeToJava.Models.MCE;

internal record SubChunk([property: JsonPropertyName("block_palette")] List<PaletteEntry> BlockPalette, int[] Blocks, int3 Position);
