﻿using MCeToJava.Utils;
using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace MCeToJava.NBT;

internal sealed class NbtMapBuilder : IDictionary<string, object>
{
	private readonly Dictionary<string, object> _map = [];

	public ICollection<string> Keys => _map.Keys;

	public ICollection<object> Values => _map.Values;

	public int Count => _map.Count;

	public bool IsReadOnly => false;

	public object this[string key]
	{
		get => _map[key];
		set => _map[key] = value;
	}

	public static NbtMapBuilder From(NbtMap map)
	{
		NbtMapBuilder builder = [];
		builder._map.AddRange(map.Map);
		return builder;
	}

	public void Add(string key, object value)
	{
		ArgumentNullException.ThrowIfNull(value, nameof(value));

		if (value is bool b)
		{
			value = (byte)(b ? 1 : 0);
		}

		NbtType.FromClass(value.GetType()); // Make sure value is valid
		this[key] = NbtUtils.Clone(value);
	}

	public void Add(KeyValuePair<string, object> item)
		=> Add(item.Key, item.Value);

	public void Clear()
		=> _map.Clear();

	public bool Contains(KeyValuePair<string, object> item)
		=> _map.Contains(item);

	public bool ContainsKey(string key)
		=> _map.ContainsKey(key);

	public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
		=> ((ICollection<KeyValuePair<string, object>>)_map).CopyTo(array, arrayIndex);

	public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
		=> _map.GetEnumerator();

	public bool Remove(string key)
		=> _map.Remove(key);

	public bool Remove(KeyValuePair<string, object> item)
		=> ((ICollection<KeyValuePair<string, object>>)_map).Remove(item);

	public bool TryGetValue(string key, [MaybeNullWhen(false)] out object value)
		=> _map.TryGetValue(key, out value);

	IEnumerator IEnumerable.GetEnumerator()
		=> _map.GetEnumerator();

	public NbtMapBuilder PutBoolean(string name, bool value)
	{
		Add(name, (byte)(value ? 1 : 0));
		return this;
	}

	public NbtMapBuilder PutByte(string name, byte value)
	{
		Add(name, value);
		return this;
	}

	public NbtMapBuilder PutByteArray(string name, byte[] value)
	{
		Add(name, value);
		return this;
	}

	public NbtMapBuilder PutDouble(string name, double value)
	{
		Add(name, value);
		return this;
	}

	public NbtMapBuilder PutFloat(string name, float value)
	{
		Add(name, value);
		return this;
	}

	public NbtMapBuilder PutIntArray(string name, int[] value)
	{
		Add(name, value);
		return this;
	}

	public NbtMapBuilder PutLongArray(string name, long[] value)
	{
		Add(name, value);
		return this;
	}

	public NbtMapBuilder PutInt(string name, int value)
	{
		Add(name, value);
		return this;
	}

	public NbtMapBuilder PutLong(string name, long value)
	{
		Add(name, value);
		return this;
	}

	public NbtMapBuilder PutShort(string name, short value)
	{
		Add(name, value);
		return this;
	}

	public NbtMapBuilder PutString(string name, string value)
	{
		Add(name, value);
		return this;
	}

	public NbtMapBuilder PutCompound(string name, NbtMap value)
	{
		Add(name, value);
		return this;
	}

	public NbtMapBuilder PutList(string name, NbtType type, params object[] values)
	{
		Add(name, new NbtList(type, values));
		return this;
	}

	public NbtMapBuilder PutList(string name, NbtType type, IList list)
	{
		if (list is not NbtList)
		{
			list = new NbtList(type, list);
		}

		Add(name, list);
		return this;
	}

	public NbtMapBuilder Rename(string oldName, string newName)
	{
		if (TryGetValue(oldName, out object? o))
		{
			Remove(oldName);
			Add(newName, o);
		}

		return this;
	}

	public NbtMap Build()
		=> Count == 0 ? NbtMap.EMPTY : new NbtMap(this);

	public override string ToString()
		=> NbtMap.MapToString(this);
}
