using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MCeToJava.Models.MCE
{
	internal record Buildplate(Guid Id, string ETag, DateTime LastUpdated, bool IsModified, bool Locked, long NumberOfBlocks, long RequiredLevel, Guid TemplateId, Buildplate.Gamemode Type, string Model)
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public enum Gamemode
        {
            Survival,
        }
    }
}
