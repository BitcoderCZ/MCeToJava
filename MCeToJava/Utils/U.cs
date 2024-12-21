using MCeToJava.Converters;
using System.Text.Json;

namespace MCeToJava.Utils;

internal static class U
{
	private static readonly JsonSerializerOptions DefaultJsonOptions = new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

	static U()
	{
		DefaultJsonOptions.Converters.Add(new JsonConverter_int3());
	}

	public static T? DeserializeJson<T>(ReadOnlySpan<char> json)
		=> JsonSerializer.Deserialize<T>(json, DefaultJsonOptions);

	public static T? DeserializeJson<T>(ReadOnlySpan<byte> utf8Json)
		=> JsonSerializer.Deserialize<T>(utf8Json, DefaultJsonOptions);

	public static string SerializeJson<T>(T value)
		=> JsonSerializer.Serialize(value, DefaultJsonOptions);
}
