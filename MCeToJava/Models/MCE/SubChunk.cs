using MathUtils.Vectors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MCeToJava.Models.MCE
{
	internal record SubChunk([property: JsonPropertyName("block_palette")] PaletteEntry[] BlockPalette, int[] Blocks, int3 Position);
}
