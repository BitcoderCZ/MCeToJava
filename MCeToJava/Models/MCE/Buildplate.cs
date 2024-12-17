using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MCeToJava.Models.MCE
{
	internal sealed class Buildplate
	{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
		public Guid Id;
		public string ETag;
		public DateTime LastUpdated;
		public bool IsModified;
		public bool Locked;
		public long NumberOfBlocks;
		public long RequiredLevel;
		public Guid TemplateId;
		public Gamemode Type;
		public string Model;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

		[JsonConverter(typeof(JsonStringEnumConverter))]
		public enum Gamemode
		{
			Survival,
		}
	}
}
