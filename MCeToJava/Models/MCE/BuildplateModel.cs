using System.Text.Json.Serialization;

namespace MCeToJava.Models.MCE;

internal record BuildplateModel(BlockEntity[] BlockEntities, object[] Entities, [property: JsonPropertyName("format_version")] int FormatVersion, bool IsNight, [property: JsonPropertyName("sub_chunks")] SubChunk[] SubChunks);
