using MCeToJava.Utils;
using SharpNBT;

namespace MCeToJava.NBT;

internal sealed class NbtType
{
#pragma warning disable SA1310 // Field names should not contain underscore
	public static readonly NbtType END = new NbtType(typeof(void), TagType.End);
	public static readonly NbtType BYTE = new NbtType(typeof(byte), TagType.Byte);
	public static readonly NbtType SHORT = new NbtType(typeof(short), TagType.Short);
	public static readonly NbtType INT = new NbtType(typeof(int), TagType.Int);
	public static readonly NbtType LONG = new NbtType(typeof(long), TagType.Long);
	public static readonly NbtType FLOAT = new NbtType(typeof(float), TagType.Float);
	public static readonly NbtType DOUBLE = new NbtType(typeof(double), TagType.Double);
	public static readonly NbtType BYTE_ARRAY = new NbtType(typeof(byte[]), TagType.ByteArray);
	public static readonly NbtType STRING = new NbtType(typeof(string), TagType.Short);

	public static readonly NbtType LIST = new NbtType(typeof(NbtList), TagType.List);
	public static readonly NbtType COMPOUND = new NbtType(typeof(NbtMap), TagType.Compound);
	public static readonly NbtType INT_ARRAY = new NbtType(typeof(int[]), TagType.IntArray);
	public static readonly NbtType LONG_ARRAY = new NbtType(typeof(long[]), TagType.LongArray);

	private static readonly NbtType[] BY_ID = [END, BYTE, SHORT, INT, LONG, FLOAT, DOUBLE, BYTE_ARRAY, STRING, LIST, COMPOUND, INT_ARRAY, LONG_ARRAY];

	private static readonly Dictionary<Type, NbtType> BY_CLASS = [];
#pragma warning restore SA1310 // Field names should not contain underscore

	static NbtType()
	{
		foreach (NbtType type in BY_ID)
		{
			BY_CLASS.Add(type.TagClass, type);
		}
	}

	private NbtType(Type tagClass, TagType enumeration)
	{
		TagClass = tagClass;
		Enumeration = enumeration;
	}

	public Type TagClass { get; private set; }

	public TagType Enumeration { get; private set; }

	public int Id => (int)Enumeration;

	public string TypeName => Enumeration.GetName();

	public static NbtType FromId(int id)
		=> id >= 0 && id < BY_ID.Length
			? BY_ID[id]
			: throw new IndexOutOfRangeException($"Tag type id must be greater than 0 and less than {BY_ID.Length - 1}.");

	public static NbtType FromClass(Type tagClass)
	{
		NbtType? type = BY_CLASS.GetOrDefault(tagClass);
		return type is null
			? throw new ArgumentException($"Tag of class '{tagClass.FullName}' does not exist", nameof(tagClass))
			: type;
	}
}

#pragma warning disable SA1204 // Static elements should appear before instance elements
internal static class NbtType_EnumExtensions
#pragma warning restore SA1204
{
	public static string GetName(this TagType e)
		=> "TAG_" + Enum.GetName(e);
}
