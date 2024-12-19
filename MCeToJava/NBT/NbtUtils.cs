using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace MCeToJava.NBT
{
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
		{
			if (val is byte[] bytes)
				return (T)bytes.Clone();
			else if (val is int[] ints)
				return (T)ints.Clone();
			else if (val is long[] longs)
				return (T)longs.Clone();

			return val;
		}

		[return: NotNullIfNotNull(nameof(val))]
		public static object? CloneObject(object? val)
		{
			if (val is byte[] bytes)
				return bytes.Clone();
			else if (val is int[] ints)
				return ints.Clone();
			else if (val is long[] longs)
				return longs.Clone();

			return val;
		}

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

		private static readonly char[] HEX_CODE = "0123456789ABCDEF".ToArray();

		public static string PrintHexBinary(byte[] data)
		{
			StringBuilder r = new StringBuilder(data.Length << 1);
			foreach (byte b in data)
			{
				r.Append(HEX_CODE[(b >> 4) & 0xF]);
				r.Append(HEX_CODE[(b & 0xF)]);
			}
			return r.ToString();
		}
	}
}
