using MCeToJava.Exceptions;
using MCeToJava.NBT;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace MCeToJava;

internal static class JsonNbtConverter
{
	public static NbtMap Convert(CompoundJsonNbtTag tag)
	{
		Dictionary<string, object> value = new();
		foreach (var entry in tag.Value)
			value[entry.Key] = Convert(entry.Value);

		return new NbtMap(value);
	}

	public static NbtList Convert(ListJsonNbtTag tag)
	{
		List<object> value = new();
		foreach (JsonNbtTag item in tag.Value)
			value.Add(Convert(item));

		Debug.Assert(value.Count > 0);

		return new NbtList(NbtType.FromClass(value[0].GetType()), value);
	}

	private static object Convert(JsonNbtTag tag)
	{
		if (tag is CompoundJsonNbtTag map)
			return Convert(map);
		else if (tag is ListJsonNbtTag list)
			return Convert(list);
		else if (tag is IntJsonNbtTag i)
			return i.Value;
		else if (tag is ByteJsonNbtTag b)
			return b.Value;
		else if (tag is FloatJsonNbtTag f)
			return f.Value;
		else if (tag is StringJsonNbtTag s)
			return s.Value;
		else
			throw new UnsupportedOperationException($"Cannot convert tag of type {tag.GetType().Name}");
	}

	[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
	[JsonDerivedType(typeof(CompoundJsonNbtTag), "compound")]
	[JsonDerivedType(typeof(ListJsonNbtTag), "list")]
	[JsonDerivedType(typeof(IntJsonNbtTag), "int")]
	[JsonDerivedType(typeof(ByteJsonNbtTag), "byte")]
	[JsonDerivedType(typeof(FloatJsonNbtTag), "float")]
	[JsonDerivedType(typeof(StringJsonNbtTag), "string")]
	public abstract class JsonNbtTag
	{
		[JsonConverter(typeof(JsonStringEnumConverter))]
		public enum TagType
		{
			[EnumMember(Value = "compound")] COMPOUND,
			[EnumMember(Value = "list")] LIST,
			[EnumMember(Value = "int")] INT,
			[EnumMember(Value = "byte")] BYTE,
			[EnumMember(Value = "float")] FLOAT,
			[EnumMember(Value = "string")] STRING
		}

		public TagType Type { get; }

		protected JsonNbtTag(TagType type)
		{
			Type = type;
		}
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
