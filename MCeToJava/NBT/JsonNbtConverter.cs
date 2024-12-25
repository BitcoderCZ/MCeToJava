// <copyright file="JsonNbtConverter.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using MCeToJava.Exceptions;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace MCeToJava.NBT;

internal static class JsonNbtConverter
{
	public static NbtMap Convert(CompoundJsonNbtTag tag)
	{
		Dictionary<string, object> value = [];
		foreach (var entry in tag.Value)
		{
			value[entry.Key] = Convert(entry.Value);
		}

		return new NbtMap(value);
	}

	public static NbtList Convert(ListJsonNbtTag tag)
	{
		if (tag.Value.Count == 0)
		{
			throw new ArgumentException($"{nameof(tag)} cannot be empty.", nameof(tag));
		}

		List<object> value = [];
		foreach (JsonNbtTag item in tag.Value)
		{
			value.Add(Convert(item));
		}

		return new NbtList(NbtType.FromClass(value[0].GetType()), value);
	}

	private static object Convert(JsonNbtTag tag)
		=> tag switch
		{
			CompoundJsonNbtTag map => Convert(map),
			ListJsonNbtTag list => Convert(list),
			IntJsonNbtTag i => i.Value,
			ByteJsonNbtTag b => b.Value,
			FloatJsonNbtTag f => f.Value,
			StringJsonNbtTag s => s.Value,
			_ => throw new UnsupportedOperationException($"Cannot convert tag of type '{tag.GetType().Name}'."),
		};

	[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
	[JsonDerivedType(typeof(CompoundJsonNbtTag), "compound")]
	[JsonDerivedType(typeof(ListJsonNbtTag), "list")]
	[JsonDerivedType(typeof(IntJsonNbtTag), "int")]
	[JsonDerivedType(typeof(ByteJsonNbtTag), "byte")]
	[JsonDerivedType(typeof(FloatJsonNbtTag), "float")]
	[JsonDerivedType(typeof(StringJsonNbtTag), "string")]
	public abstract class JsonNbtTag
	{
		protected JsonNbtTag(TagType type)
		{
			Type = type;
		}

		[JsonConverter(typeof(JsonStringEnumConverter))]
		public enum TagType
		{
			[EnumMember(Value = "compound")]
			COMPOUND,
			[EnumMember(Value = "list")]
			LIST,
			[EnumMember(Value = "int")]
			INT,
			[EnumMember(Value = "byte")]
			BYTE,
			[EnumMember(Value = "float")]
			FLOAT,
			[EnumMember(Value = "string")]
			STRING,
		}

		public TagType Type { get; }
	}

	public sealed class CompoundJsonNbtTag : JsonNbtTag
	{
		public CompoundJsonNbtTag()
			: base(TagType.COMPOUND)
		{
		}

		public required Dictionary<string, JsonNbtTag> Value { get; init; }
	}

	public sealed class ListJsonNbtTag : JsonNbtTag
	{
		public ListJsonNbtTag()
			: base(TagType.LIST)
		{
		}

		public required List<JsonNbtTag> Value { get; init; }
	}

	public sealed class IntJsonNbtTag : JsonNbtTag
	{
		public IntJsonNbtTag()
			: base(TagType.INT)
		{
		}

		public required int Value { get; init; }
	}

	public sealed class ByteJsonNbtTag : JsonNbtTag
	{
		public ByteJsonNbtTag()
			: base(TagType.BYTE)
		{
		}

		public required byte Value { get; init; }
	}

	public sealed class FloatJsonNbtTag : JsonNbtTag
	{
		public FloatJsonNbtTag()
			: base(TagType.FLOAT)
		{
		}

		public required float Value { get; init; }
	}

	public sealed class StringJsonNbtTag : JsonNbtTag
	{
		public StringJsonNbtTag()
			: base(TagType.STRING)
		{
		}

		public required string Value { get; init; }
	}
}
