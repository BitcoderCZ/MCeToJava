﻿using MCeToJava.Exceptions;
using MCeToJava.NBT;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace MCeToJava
{
	internal static class JsonNbtConverter
	{
		public static NbtMap Convert(CompoundJsonNbtTag tag)
		{
			Dictionary<string, object> value = new();
			foreach (var entry in (Dictionary<string, JsonNbtTag>)tag.value)
				value[entry.Key] = Convert(entry.Value);

			return new NbtMap(value);
		}

		public static NbtList Convert(ListJsonNbtTag tag)
		{
			List<object> value = new();
			foreach (JsonNbtTag item in (JsonNbtTag[])tag.value)
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
				return i.value;
			else if (tag is ByteJsonNbtTag b)
				return b.value;
			else if (tag is FloatJsonNbtTag f)
				return f.value;
			else if (tag is StringJsonNbtTag s)
				return s.value;
			else
				throw new UnsupportedOperationException($"Cannot convert tag of type {tag.GetType().Name}");
		}

		public abstract class JsonNbtTag
		{
			[JsonConverter(typeof(JsonStringEnumConverter))]
			public enum Type
			{
				[EnumMember(Value = "compound")] COMPOUND,
				[EnumMember(Value = "list")] LIST,
				[EnumMember(Value = "int")] INT,
				[EnumMember(Value = "byte")] BYTE,
				[EnumMember(Value = "float")] FLOAT,
				[EnumMember(Value = "string")] STRING
			}

			public readonly Type type;
			public readonly object value;

			public JsonNbtTag(Type type, object value)
			{
				this.type = type;
				this.value = value;
			}
		}

		public sealed class CompoundJsonNbtTag : JsonNbtTag
		{
			public CompoundJsonNbtTag(Dictionary<string, JsonNbtTag> value)
				: base(Type.COMPOUND, value)
			{
			}
		}

		public sealed class ListJsonNbtTag : JsonNbtTag
		{
			public ListJsonNbtTag(JsonNbtTag[] value)
				: base(Type.LIST, value)
			{
			}
		}

		public sealed class IntJsonNbtTag : JsonNbtTag
		{
			public IntJsonNbtTag(int value)
				: base(Type.INT, value)
			{
			}
		}

		public sealed class ByteJsonNbtTag : JsonNbtTag
		{
			public ByteJsonNbtTag(byte value)
				: base(Type.BYTE, value)
			{
			}
		}

		public sealed class FloatJsonNbtTag : JsonNbtTag
		{
			public FloatJsonNbtTag(float value)
				: base(Type.FLOAT, value)
			{
			}
		}

		public sealed class StringJsonNbtTag : JsonNbtTag
		{
			public StringJsonNbtTag(string value)
				: base(Type.STRING, value)
			{
			}
		}
	}
}
