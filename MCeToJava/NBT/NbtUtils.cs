// <copyright file="NbtUtils.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using SharpNBT;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace MCeToJava.NBT;

internal static class NbtUtils
{
	public static string ToString(object o)
	{
		if (o is byte b)
		{
			return b + "b";
		}
		else if (o is short s)
		{
			return s + "s";
		}
		else if (o is int i)
		{
			return i + "i";
		}
		else if (o is long)
		{
			return ((long)o) + "l";
		}
		else if (o is float f)
		{
			return f.ToString(CultureInfo.InvariantCulture) + "f";
		}
		else if (o is double d)
		{
			return d.ToString(CultureInfo.InvariantCulture) + "d";
		}
		else if (o is byte[] byteArr)
		{
			return "0x" + PrintHexBinary(byteArr);
		}
		else if (o is string str)
		{
			return "\"" + str + "\"";
		}
		else if (o is int[] intAr)
		{
			return "[ " + string.Join(", ", intAr.Select(item => item + "i")) + " ]";
		}
		else if (o is long[] longAr)
		{
			return "[ " + string.Join(", ", longAr.Select(item => item + "l")) + " ]";
		}

		return o.ToString()!;
	}

	[return: NotNullIfNotNull(nameof(val))]
	public static T? Clone<T>(T? val)
		=> val switch
		{
			byte[] bytes => (T)bytes.Clone(),
			int[] ints => (T)ints.Clone(),
			long[] longs => (T)longs.Clone(),
			_ => val,
		};

	[return: NotNullIfNotNull(nameof(val))]
	public static object? CloneObject(object? val)
		=> val switch
		{
			byte[] bytes => bytes.Clone(),
			int[] ints => ints.Clone(),
			long[] longs => longs.Clone(),
			_ => val,
		};

	public static string Indent(string str)
	{
		StringBuilder builder = new StringBuilder("  " + str);
		for (int i = 2; i < builder.Length; i++)
		{
			if (builder[i] == '\n')
			{
				builder.Insert(i + 1, "  ");
				i += 2;
			}
		}

		return builder.ToString();
	}

#pragma warning disable SA1201 // Elements should appear in the correct order
	private static readonly char[] HexDigits = [.. "0123456789ABCDEF"];
#pragma warning restore SA1201

	public static string PrintHexBinary(byte[] data)
	{
		StringBuilder r = new StringBuilder(data.Length << 1);
		foreach (byte b in data)
		{
			r.Append(HexDigits[(b >> 4) & 0xF]);
			r.Append(HexDigits[b & 0xF]);
		}

		return r.ToString();
	}

	public static Tag? CreateTag(string? name, object value)
	{
		switch (value)
		{
			case byte b:
				return new ByteTag(name, b);
			case short s:
				return new ShortTag(name, s);
			case int i:
				return new IntTag(name, i);
			case long l:
				return new LongTag(name, l);
			case float f:
				return new FloatTag(name, f);
			case double d:
				return new DoubleTag(name, d);
			case byte[] ba:
				return new ByteArrayTag(name, ba);
			case string s:
				return new StringTag(name, s);
			case NbtList list:
				{
					ListTag listTag = new ListTag(name, list.Type.Enumeration, list.Count);
					foreach (object? item in list)
					{
						var tag = CreateTag(null, item);
						if (tag is not null)
						{
							listTag.Add(tag);
						}
					}

					return listTag;
				}

			case NbtMap map:
				{
					CompoundTag compoundTag = new CompoundTag(name);

					foreach (var (key, item) in map.Map)
					{
						var tag = CreateTag(key, item);
						if (tag is not null)
						{
							compoundTag.Add(key, tag);
						}
					}

					return compoundTag;
				}

			case int[] ia:
				return new IntArrayTag(name, ia);
			case long[] la:
				return new LongArrayTag(name, la);
			default:
				return null;
		}
	}
}
