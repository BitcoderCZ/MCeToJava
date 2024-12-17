using MCeToJava.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MCeToJava.Utils
{
	internal static class U
	{
		private static readonly JsonSerializerOptions DefaultJsonOptions = new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, IncludeFields = true };

		static U()
		{
			DefaultJsonOptions.Converters.Add(new JsonConverter_int3());
		}

		public static T? DeserializeJson<T>(ReadOnlySpan<char> json)
			=> JsonSerializer.Deserialize<T>(json, DefaultJsonOptions);

		public static T? DeserializeJson<T>(ReadOnlySpan<byte> utf8Json)
			=> JsonSerializer.Deserialize<T>(utf8Json, DefaultJsonOptions);
	}
}
