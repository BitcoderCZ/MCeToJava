using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MCeToJava.Models.MCE
{
	internal record BuildplateModel(object[] BlockEntities, object[] Entities, [property: JsonPropertyName("format_version")] int FormatVersion, bool IsNight, [property: JsonPropertyName("sub_chunks")] SubChunk[] SubChunks);
}
