using MathUtils.Vectors;
using System.Text.Json.Serialization;

namespace MCeToJava.Models.MCE;

internal record SubChunk([property: JsonPropertyName("block_palette")] List<PaletteEntry> BlockPalette, int[] Blocks, int3 Position);
