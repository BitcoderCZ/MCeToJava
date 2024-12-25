using MCeToJava.Utils;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json.Serialization;

namespace MCeToJava.NBT;

internal sealed class NbtMap : IEnumerable<KeyValuePair<string, object>>
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
#pragma warning disable IDE0028 // Simplify collection initialization
		=> new NbtMapBuilder();
#pragma warning restore IDE0028

	public NbtMapBuilder ToBuilder()
		=> NbtMapBuilder.From(this);

	public bool ContainsKey(string key)
		=> map.ContainsKey(key);

	public bool ContainsKey(string key, NbtType type)
		=> map.TryGetValue(key, out object? o) && o.GetType() == type.TagClass;

	public bool TryGetValue(string key, [MaybeNullWhen(false)] out object? value)
		=> map.TryGetValue(key, out value);

	public object? Get(string key)
		=> NbtUtils.CloneObject(map.GetOrDefault(key));

	public bool GetBool(string key)
		=> GetBool(key, false);

	public bool GetBool(string key, bool defaultValue)
	{
		object? tag = map.GetOrDefault(key);
		return tag is byte b ? b != 0 : defaultValue;
	}

	public void ListenForBool(string key, Action<bool> consumer)
	{
		object? tag = map.GetOrDefault(key);
		if (tag is byte b)
			consumer.Invoke(b != 0);
	}

	public byte GetByte(string key)
		=> GetByte(key, 0);

	public byte GetByte(string key, byte defaultValue)
	{
		object? tag = map.GetOrDefault(key);
		return tag is byte b ? b : defaultValue;
	}

	public void ListenForByte(string key, Action<byte> consumer)
	{
		object? tag = map.GetOrDefault(key);
		if (tag is byte b)
			consumer.Invoke(b);
	}

	public short GetShort(string key)
		=> GetShort(key, 0);

	public short GetShort(string key, short defaultValue)
	{
		object? tag = map.GetOrDefault(key);
		return tag is short s ? s : defaultValue;
	}

	public void ListenForShort(string key, Action<short> consumer)
	{
		object? tag = map.GetOrDefault(key);
		if (tag is short s)
			consumer.Invoke(s);
	}

	public int GetInt(string key)
		=> GetInt(key, 0);

	public int GetInt(string key, int defaultValue)
	{
		object? tag = map.GetOrDefault(key);
		return tag is int i ? i : defaultValue;
	}

	public void ListenForInt(string key, Action<int> consumer)
	{
		object? tag = map.GetOrDefault(key);
		if (tag is int i)
			consumer.Invoke(i);
	}

	public long GetLong(string key)
		=> GetLong(key, 0L);

	public long GetLong(string key, long defaultValue)
	{
		object? tag = map.GetOrDefault(key);
		return tag is long l ? l : defaultValue;
	}

	public void ListenForLong(string key, Action<long> consumer)
	{
		object? tag = map.GetOrDefault(key);
		if (tag is long l)
			consumer.Invoke(l);
	}

	public float GetFloat(string key)
		=> GetFloat(key, 0F);

	public float GetFloat(string key, float defaultValue)
	{
		object? tag = map.GetOrDefault(key);
		return tag is float f ? f : defaultValue;
	}

	public void ListenForFloat(string key, Action<float> consumer)
	{
		object? tag = map.GetOrDefault(key);
		if (tag is float f)
			consumer.Invoke(f);
	}

	public double GetDouble(string key)
		=> GetDouble(key, 0.0);

	public double GetDouble(string key, double defaultValue)
	{
		object? tag = map.GetOrDefault(key);
		return tag is double d ? d : defaultValue;
	}

	public void ListenForDouble(string key, Action<double> consumer)
	{
		object? tag = map.GetOrDefault(key);
		if (tag is double d)
			consumer.Invoke(d);
	}

	public string? GetString(string key)
		=> Getstring(key, string.Empty);

	public string? Getstring(string key, string? defaultValue)
	{
		object? tag = map.GetOrDefault(key);
		return tag is string s ? s : defaultValue;
	}

	public void ListenForstring(string key, Action<string> consumer)
	{
		object? tag = map.GetOrDefault(key);
		if (tag is string s)
			consumer.Invoke(s);
	}

	public byte[]? GetByteArray(string key)
		=> GetByteArray(key, []);

	public byte[]? GetByteArray(string key, byte[]? defaultValue)
	{
		object? tag = map.GetOrDefault(key);
		return tag is byte[] bytes ? (byte[])bytes.Clone() : defaultValue;
	}

	public void ListenForByteArray(string key, Action<byte[]> consumer)
	{
		object? tag = map.GetOrDefault(key);
		if (tag is byte[] bytes)
			consumer.Invoke((byte[])bytes.Clone());
	}

	public int[]? GetIntArray(string key)
		=> GetIntArray(key, []);

	public int[]? GetIntArray(string key, int[]? defaultValue)
	{
		object? tag = map.GetOrDefault(key);
		return tag is int[] ints ? (int[])ints.Clone() : defaultValue;
	}

	public void ListenForIntArray(string key, Action<int[]> consumer)
	{
		object? tag = map.GetOrDefault(key);
		if (tag is int[] ints)
			consumer.Invoke((int[])ints.Clone());
	}

	public long[]? GetLongArray(string key)
		=> GetLongArray(key, []);

	public long[]? GetLongArray(string key, long[]? defaultValue)
	{
		object? tag = map.GetOrDefault(key);
		return tag is long[] longs ? (long[])longs.Clone() : defaultValue;
	}

	public void ListenForLongArray(string key, Action<long[]> consumer)
	{
		object? tag = map.GetOrDefault(key);
		if (tag is long[] longs)
			consumer.Invoke((long[])longs.Clone());
	}

	public NbtMap? GetCompound(string key)
		=> GetCompound(key, EMPTY);

	public NbtMap? GetCompound(string key, NbtMap? defaultValue)
	{
		object? tag = map.GetOrDefault(key);
		return tag is NbtMap nm ? nm : defaultValue;
	}

	public void ListenForCompound(string key, Action<NbtMap> consumer)
	{
		object? tag = map.GetOrDefault(key);
		if (tag is NbtMap nm)
			consumer.Invoke(nm);
	}

	public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
		=> map.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator()
		=> map.GetEnumerator();

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
		=> MapToString(map);

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
