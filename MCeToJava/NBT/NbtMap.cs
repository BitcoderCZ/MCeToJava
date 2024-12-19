using MCeToJava.Utils;
using System.Text;
using System.Text.Json.Serialization;

namespace MCeToJava.NBT
{
	internal sealed class NbtMap
	{
		public static readonly NbtMap EMPTY = new NbtMap();

		internal readonly IDictionary<string, object> map;

		public int Count => map.Count;

		[JsonIgnore]
		private bool hashCodeGenerated;
		[JsonIgnore]
		private int hashCode;

		private NbtMap()
		{
			map = new Dictionary<string, object>();
		}

		internal NbtMap(IDictionary<string, object> map)
		{
			this.map = map;
		}

		public static NbtMapBuilder CreateBuilder()
		{
			return new NbtMapBuilder();
		}

		public NbtMapBuilder ToBuilder()
		{
			return NbtMapBuilder.from(this);
		}

		public bool ContainsKey(string key)
			=> map.ContainsKey(key);

		public bool ContainsKey(string key, NbtType type)
		{
			if (map.TryGetValue(key, out object? o))
				return o.GetType() == type.TagClass;
			else
				return false;
		}

		public object? Get(string key)
		{
			return NbtUtils.CloneObject(map.GetOrDefault(key));
		}

		public bool GetBool(string key)
		{
			return GetBool(key, false);
		}

		public bool GetBool(string key, bool defaultValue)
		{
			object? tag = map.GetOrDefault(key);
			if (tag is byte b)
				return b != 0;

			return defaultValue;
		}

		public void ListenForBool(string key, Action<bool> consumer)
		{
			object? tag = map.GetOrDefault(key);
			if (tag is byte b)
				consumer.Invoke(b != 0);
		}

		public byte GetByte(string key)
		{
			return GetByte(key, 0);
		}

		public byte GetByte(string key, byte defaultValue)
		{
			object? tag = map.GetOrDefault(key);
			if (tag is byte b)
				return b;

			return defaultValue;
		}

		public void ListenForByte(string key, Action<byte> consumer)
		{
			object? tag = map.GetOrDefault(key);
			if (tag is byte b)
				consumer.Invoke(b);
		}

		public short GetShort(string key)
		{
			return GetShort(key, 0);
		}

		public short GetShort(string key, short defaultValue)
		{
			object? tag = map.GetOrDefault(key);
			if (tag is short s)
				return s;

			return defaultValue;
		}

		public void ListenForShort(string key, Action<short> consumer)
		{
			object? tag = map.GetOrDefault(key);
			if (tag is short s)
				consumer.Invoke(s);
		}

		public int GetInt(string key)
		{
			return GetInt(key, 0);
		}

		public int GetInt(string key, int defaultValue)
		{
			object? tag = map.GetOrDefault(key);
			if (tag is int i)
				return i;

			return defaultValue;
		}

		public void ListenForInt(string key, Action<int> consumer)
		{
			object? tag = map.GetOrDefault(key);
			if (tag is int i)
				consumer.Invoke(i);
		}

		public long GetLong(string key)
		{
			return GetLong(key, 0L);
		}

		public long GetLong(string key, long defaultValue)
		{
			object? tag = map.GetOrDefault(key);
			if (tag is long l)
				return l;

			return defaultValue;
		}

		public void ListenForLong(string key, Action<long> consumer)
		{
			object? tag = map.GetOrDefault(key);
			if (tag is long l)
				consumer.Invoke(l);
		}

		public float GetFloat(string key)
		{
			return GetFloat(key, 0F);
		}

		public float GetFloat(string key, float defaultValue)
		{
			object? tag = map.GetOrDefault(key);
			if (tag is float f)
				return f;

			return defaultValue;
		}

		public void ListenForFloat(string key, Action<float> consumer)
		{
			object? tag = map.GetOrDefault(key);
			if (tag is float f)
				consumer.Invoke(f);
		}

		public double GetDouble(string key)
		{
			return GetDouble(key, 0.0);
		}

		public double GetDouble(string key, double defaultValue)
		{
			object? tag = map.GetOrDefault(key);
			if (tag is double d)
				return d;

			return defaultValue;
		}

		public void ListenForDouble(string key, Action<double> consumer)
		{
			object? tag = map.GetOrDefault(key);
			if (tag is double d)
				consumer.Invoke(d);
		}

		public string? GetString(string key)
		{
			return Getstring(key, string.Empty);
		}

		public string? Getstring(string key, string? defaultValue)
		{
			object? tag = map.GetOrDefault(key);
			if (tag is string s)
				return s;

			return defaultValue;
		}

		public void ListenForstring(string key, Action<string> consumer)
		{
			object? tag = map.GetOrDefault(key);
			if (tag is string s)
				consumer.Invoke(s);
		}

		public byte[]? getByteArray(string key)
		{
			return GetByteArray(key, Array.Empty<byte>());
		}

		public byte[]? GetByteArray(string key, byte[]? defaultValue)
		{
			object? tag = map.GetOrDefault(key);
			if (tag is byte[] bytes)
				return (byte[])bytes.Clone();

			return defaultValue;
		}

		public void ListenForByteArray(string key, Action<byte[]> consumer)
		{
			object? tag = map.GetOrDefault(key);
			if (tag is byte[] bytes)
				consumer.Invoke((byte[])bytes.Clone());
		}

		public int[]? GetIntArray(string key)
		{
			return GetIntArray(key, Array.Empty<int>());
		}

		public int[]? GetIntArray(string key, int[]? defaultValue)
		{
			object? tag = map.GetOrDefault(key);
			if (tag is int[] ints)
				return (int[])ints.Clone();

			return defaultValue;
		}

		public void ListenForIntArray(string key, Action<int[]> consumer)
		{
			object? tag = map.GetOrDefault(key);
			if (tag is int[] ints)
				consumer.Invoke((int[])ints.Clone());
		}

		public long[]? GetLongArray(string key)
		{
			return GetLongArray(key, Array.Empty<long>());
		}

		public long[]? GetLongArray(string key, long[]? defaultValue)
		{
			object? tag = map.GetOrDefault(key);
			if (tag is long[] longs)
				return (long[])longs.Clone();

			return defaultValue;
		}

		public void ListenForLongArray(string key, Action<long[]> consumer)
		{
			object? tag = map.GetOrDefault(key);
			if (tag is long[] longs)
				consumer.Invoke((long[])longs.Clone());
		}

		public NbtMap? GetCompound(string key)
		{
			return GetCompound(key, EMPTY);
		}

		public NbtMap? GetCompound(string key, NbtMap? defaultValue)
		{
			object? tag = map.GetOrDefault(key);
			if (tag is NbtMap nm)
				return nm;

			return defaultValue;
		}

		public void ListenForCompound(string key, Action<NbtMap> consumer)
		{
			object? tag = map.GetOrDefault(key);
			if (tag is NbtMap nm)
				consumer.Invoke(nm);
		}


		public override bool Equals(object? o)
		{
			if (o == this)
				return true;

			if (o is not NbtMap m || m.Count != Count)
				return false;

			if (hashCodeGenerated && m.hashCodeGenerated && hashCode != ((NbtMap)o).hashCode)
				return false;

			try
			{
				foreach (var e in map)
				{
					string key = e.Key;
					object value = e.Value;
					if (value == null)
					{
						if (!(m.Get(key) == null && m.ContainsKey(key)))
							return false;
					}
					else
					{
						if (!ObjectUtils.DeepEquals(value, m.Get(key)))
							return false;
					}
				}
			}
			catch
			{
				return false;
			}

			return true;
		}

		public override int GetHashCode()
		{
			if (hashCodeGenerated)
				return hashCode;

			int h = 0;
			foreach (var item in map)
				h += item.GetHashCode();

			hashCode = h;
			hashCodeGenerated = true;
			return h;
		}

		public override string ToString()
		{
			return MapToString(map);
		}

		internal static string MapToString(IDictionary<string, object> map)
		{
			if (map.Count == 0)
				return "{}";

			StringBuilder sb = new StringBuilder();
			sb.Append('{').Append('\n');

			IEnumerator<KeyValuePair<string, object>> enumerator = map.GetEnumerator();
			enumerator.MoveNext();

			for (; ; )
			{
				var e = enumerator.Current;
				string key = e.Key;
				string value = NbtUtils.ToString(e.Value);

				string str = NbtUtils.Indent("\"" + key + "\": " + value);
				sb.Append(str);
				if (!enumerator.MoveNext())
					return sb.Append('\n').Append('}').ToString();

				sb.Append(',').Append('\n');
			}
		}
	}
}
