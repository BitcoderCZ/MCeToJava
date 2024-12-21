﻿namespace MCeToJava.Utils;

internal static class DictionaryExtensions
{
	public static void AddRange<TKey, TValue>(this IDictionary<TKey, TValue> dic, IDictionary<TKey, TValue> dicToAdd)
	{
		foreach (var item in dicToAdd)
		{
			dic[item.Key] = item.Value;
		}
	}

	public static TValue? GetOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dic, TKey key)
	{
		if (dic.TryGetValue(key, out TValue? value)) return value;
		else return default;
	}
	public static TValue? GetOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dic, TKey key, TValue? defaultValue)
	{
		if (dic.TryGetValue(key, out TValue? value)) return value;
		else return defaultValue;
	}

	public static TValue? JavaRemove<TKey, TValue>(this IDictionary<TKey, TValue> dic, TKey key)
	{
		TValue? value;

		if (!dic.TryGetValue(key, out value))
		{
			value = default;
		}

		dic.Remove(key);

		return value;
	}

	public static TValue? ComputeIfAbsent<TKey, TValue>(this IDictionary<TKey, TValue> dic, TKey key, Func<TKey, TValue?> mappingFunction)
	{
		if (dic.TryGetValue(key, out TValue? value))
		{
			return value;
		}
		else
		{
			TValue? newValue = mappingFunction(key);

			if (newValue is null)
			{
				return default;
			}
			else
			{
				dic.Add(key, newValue);
				return newValue;
			}
		}
	}

	public static void RemoveIf<TKey, TValue>(this IDictionary<TKey, TValue> dic, Predicate<KeyValuePair<TKey, TValue>> predicate)
	{
		List<TKey> toRemove = new List<TKey>();

		foreach (var item in dic)
		{
			if (predicate(item))
			{
				toRemove.Add(item.Key);
			}
		}

		for (int i = 0; i < toRemove.Count; i++)
		{
			dic.Remove(toRemove[i]);
		}
	}

	public static void Merge<TKey, TValue>(this IDictionary<TKey, TValue> dic, TKey key, TValue value, Func<TValue, TValue, TValue> remappingFunction)
	{
		if (!dic.TryGetValue(key, out TValue? currentValue) || currentValue == null)
			dic[key] = value;
		else
		{
			TValue res = remappingFunction(currentValue, value);

			if (res == null) dic.Remove(key);
			else dic[key] = res;
		}
	}
}
