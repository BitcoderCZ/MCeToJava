﻿// <copyright file="JsonConverter_int3.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using MathUtils.Vectors;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MCeToJava.JsonConverters;

internal sealed class JsonConverter_int3 : JsonConverter<int3>
{
	public override int3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType != JsonTokenType.StartObject)
		{
			throw new JsonException($"Unexpected token {reader.TokenType}, expected StartObject.");
		}

		int x = 0, y = 0, z = 0;

		string propertyX = options.PropertyNamingPolicy?.ConvertName(nameof(int3.X)) ?? nameof(int3.X);
		string propertyY = options.PropertyNamingPolicy?.ConvertName(nameof(int3.Y)) ?? nameof(int3.Y);
		string propertyZ = options.PropertyNamingPolicy?.ConvertName(nameof(int3.Z)) ?? nameof(int3.Z);

		while (reader.Read())
		{
			if (reader.TokenType == JsonTokenType.EndObject)
			{
				return new int3(x, y, z);
			}

			if (reader.TokenType == JsonTokenType.PropertyName)
			{
				string? propertyName = reader.GetString();
				reader.Read();

				if (StringEquals(propertyName, propertyX))
				{
					x = reader.GetInt32();
				}
#pragma warning disable IDE0045 // Convert to conditional expression
				else if (StringEquals(propertyName, propertyY))
				{
					y = reader.GetInt32();
				}
				else if (StringEquals(propertyName, propertyZ))
				{
					z = reader.GetInt32();
				}
				else
				{
					throw new JsonException($"Unknown property {propertyName}");
				}
#pragma warning restore IDE0045 // Convert to conditional expression
			}
		}

		throw new JsonException("Unexpected end of JSON.");

		bool StringEquals(string? a, string? b)
		{
			return a is null || b is null
				? a is null && b is null
				: options.PropertyNameCaseInsensitive
				? a.Equals(b, StringComparison.OrdinalIgnoreCase)
				: a.Equals(b, StringComparison.Ordinal);
		}
	}

	public override void Write(Utf8JsonWriter writer, int3 value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();

		string propertyX = options.PropertyNamingPolicy?.ConvertName(nameof(int3.X)) ?? nameof(int3.X);
		string propertyY = options.PropertyNamingPolicy?.ConvertName(nameof(int3.Y)) ?? nameof(int3.Y);
		string propertyZ = options.PropertyNamingPolicy?.ConvertName(nameof(int3.Z)) ?? nameof(int3.Z);

		writer.WriteNumber(propertyX, value.X);
		writer.WriteNumber(propertyY, value.Y);
		writer.WriteNumber(propertyZ, value.Z);

		writer.WriteEndObject();
	}
}
