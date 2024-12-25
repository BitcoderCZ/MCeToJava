﻿using MathUtils.Vectors;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MCeToJava.JsonConverters;

internal sealed class JsonConverter_float3 : JsonConverter<float3>
{
	public override float3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType != JsonTokenType.StartObject)
		{
			throw new JsonException($"Unexpected token {reader.TokenType}, expected StartObject.");
		}

		float x = 0, y = 0, z = 0;

		string propertyX = options.PropertyNamingPolicy?.ConvertName(nameof(float3.X)) ?? nameof(float3.X);
		string propertyY = options.PropertyNamingPolicy?.ConvertName(nameof(float3.Y)) ?? nameof(float3.Y);
		string propertyZ = options.PropertyNamingPolicy?.ConvertName(nameof(float3.Z)) ?? nameof(float3.Z);

		while (reader.Read())
		{
			if (reader.TokenType == JsonTokenType.EndObject)
			{
				return new float3(x, y, z);
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
				else if (StringEquals(propertyName, propertyZ))
				{
					z = reader.GetSingle();
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

	public override void Write(Utf8JsonWriter writer, float3 value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();

		string propertyX = options.PropertyNamingPolicy?.ConvertName(nameof(float3.X)) ?? nameof(float3.X);
		string propertyY = options.PropertyNamingPolicy?.ConvertName(nameof(float3.Y)) ?? nameof(float3.Y);
		string propertyZ = options.PropertyNamingPolicy?.ConvertName(nameof(float3.Z)) ?? nameof(float3.Z);

		writer.WriteNumber(propertyX, value.X);
		writer.WriteNumber(propertyY, value.Y);
		writer.WriteNumber(propertyZ, value.Z);

		writer.WriteEndObject();
	}
}
