using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MCeToJava.Models.MCE
{
	internal sealed class BuildplateModel
	{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
		public object[] BlockEntities;
		public object[] Entities;
		[JsonPropertyName("format_version")] public int FormatVersion;
		public bool IsNight;
		[JsonPropertyName("sub_chunks")] public SubChunk[] SubChunks;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
	}
}
