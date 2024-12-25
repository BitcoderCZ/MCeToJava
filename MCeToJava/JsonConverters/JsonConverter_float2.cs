using MathUtils.Vectors;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MCeToJava.JsonConverters;

internal sealed class JsonConverter_float2 : JsonConverter<float2>
{
	public override float2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType != JsonTokenType.StartObject)
		{
			throw new JsonException($"Unexpected token {reader.TokenType}, expected StartObject.");
		}

		float x = 0, y = 0;

		string propertyX = options.PropertyNamingPolicy?.ConvertName(nameof(float2.X)) ?? nameof(float2.X);
		string propertyY = options.PropertyNamingPolicy?.ConvertName(nameof(float2.Y)) ?? nameof(float2.Y);

		while (reader.Read())
		{
			if (reader.TokenType == JsonTokenType.EndObject)
			{
				return new float2(x, y);
			}

			if (reader.TokenType == JsonTokenType.PropertyName)
			{
				string? propertyName = reader.GetString();
				reader.Read();

				if (StringEquals(propertyName, propertyX))
				{
					x = reader.GetSingle();
				}
				else if (StringEquals(propertyName, propertyY))
				{
					y = reader.GetSingle();
				}
				else
				{
					throw new JsonException($"Unknown property {propertyName}");
				}
			}
		}

		throw new JsonException("Unexpected end of JSON.");

		bool StringEquals(string? a, string? b)
		{
			if (a is null || b is null)
				return a is null && b is null;

			return options.PropertyNameCaseInsensitive ? a.Equals(b, StringComparison.OrdinalIgnoreCase) : a.Equals(b, StringComparison.Ordinal);
		}
	}

	public override void Write(Utf8JsonWriter writer, float2 value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();

		string propertyX = options.PropertyNamingPolicy?.ConvertName(nameof(float2.X)) ?? nameof(float2.X);
		string propertyY = options.PropertyNamingPolicy?.ConvertName(nameof(float2.Y)) ?? nameof(float2.Y);

		writer.WriteNumber(propertyX, value.X);
		writer.WriteNumber(propertyY, value.Y);

		writer.WriteEndObject();
	}
}
