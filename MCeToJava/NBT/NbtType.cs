﻿using MCeToJava.Utils;

namespace MCeToJava.NBT
{
	internal sealed class NbtType
	{
		public static readonly NbtType END = new NbtType(typeof(void), Enum.END);
		public static readonly NbtType BYTE = new NbtType(typeof(byte), Enum.BYTE);
		public static readonly NbtType SHORT = new NbtType(typeof(short), Enum.SHORT);
		public static readonly NbtType INT = new NbtType(typeof(int), Enum.INT);
		public static readonly NbtType LONG = new NbtType(typeof(long), Enum.LONG);
		public static readonly NbtType FLOAT = new NbtType(typeof(float), Enum.FLOAT);
		public static readonly NbtType DOUBLE = new NbtType(typeof(double), Enum.DOUBLE);
		public static readonly NbtType BYTE_ARRAY = new NbtType(typeof(byte[]), Enum.BYTE_ARRAY);
		public static readonly NbtType STRING = new NbtType(typeof(string), Enum.STRING);

		public static readonly NbtType LIST = new NbtType(typeof(NbtList), Enum.LIST);
		public static readonly NbtType COMPOUND = new NbtType(typeof(NbtMap), Enum.COMPOUND);
		public static readonly NbtType INT_ARRAY = new NbtType(typeof(int[]), Enum.INT_ARRAY);
		public static readonly NbtType LONG_ARRAY = new NbtType(typeof(long[]), Enum.LONG_ARRAY);

		private static readonly NbtType[] BY_ID =
				{ END, BYTE, SHORT, INT, LONG, FLOAT, DOUBLE, BYTE_ARRAY, STRING, LIST, COMPOUND, INT_ARRAY, LONG_ARRAY};

		private static readonly Dictionary<Type, NbtType> BY_CLASS = new();

		static NbtType()
		{
			foreach (NbtType type in BY_ID)
				BY_CLASS.Add(type.TagClass, type);
		}

		private NbtType(Type tagClass, Enum enumeration)
		{
			TagClass = tagClass;
			Enumeration = enumeration;
		}

		public Type TagClass { get; private set; }

		public Enum Enumeration { get; private set; }

		public int Id => (int)Enumeration;

		public string TypeName => Enumeration.GetName();

		public static NbtType FromId(int id)
		{
			if (id >= 0 && id < BY_ID.Length)
			{
				return BY_ID[id];
			}
			else
			{
				throw new IndexOutOfRangeException("Tag type id must be greater than 0 and less than " + (BY_ID.Length - 1));
			}
		}

		public static NbtType FromClass(Type tagClass)
		{
			NbtType? type = BY_CLASS.GetOrDefault(tagClass);
			if (type == null)
				throw new ArgumentException("Tag of class " + tagClass + " does not exist", nameof(tagClass));

			return type;
		}

		public enum Enum : int
		{
			END,
			BYTE,
			SHORT,
			INT,
			LONG,
			FLOAT,
			DOUBLE,
			BYTE_ARRAY,
			STRING,
			LIST,
			COMPOUND,
			INT_ARRAY,
			LONG_ARRAY
		}
	}

	internal static class NbtType_EnumExtensions
	{
		public static string GetName(this NbtType.Enum e)
			=> "TAG_" + Enum.GetName(e);
	}
}
