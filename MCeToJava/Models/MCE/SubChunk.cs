using MathUtils.Vectors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MCeToJava.Models.MCE
{
	internal sealed class SubChunk
	{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
		[JsonPropertyName("block_palette")] public PaletteBlock[] BlockPalette;
		public uint[] Blocks;
		public int3 Position;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
	}
}
