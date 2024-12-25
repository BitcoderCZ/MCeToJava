using System.Collections;

namespace MCeToJava.NBT;

internal sealed class NbtList : IList
{
	public static readonly NbtList EMPTY = new NbtList(NbtType.END);

	private readonly Array _array;

	public NbtList(NbtType type, ICollection collection)
	{
		ArgumentNullException.ThrowIfNull(type);
		Type = type;
		_array = Array.CreateInstance(type.TagClass, collection.Count);
		collection.CopyTo(_array, 0);
	}

	public NbtList(NbtType tagClass, params object[] array)
	{
		ArgumentNullException.ThrowIfNull(Type, "tagClass");
		Type = tagClass;
		_array = (Array)array.Clone();
	}

	public NbtType Type { get; }

	public bool IsFixedSize => true;

	public bool IsReadOnly => true;

	public int Count => _array.Length;

	public bool IsSynchronized => false;

	public object SyncRoot => null!;

	public object? this[int index]
	{
		get => Get(index);
		set => throw new InvalidOperationException();
	}

	public object Get(int index)
		=> index >= 0 && index < _array.Length
			? NbtUtils.Clone(_array.GetValue(index)!)
			: throw new IndexOutOfRangeException("Expected 0-" + (_array.Length - 1) + ". Got " + index);

	public int Add(object? value)
		=> throw new InvalidOperationException();

	public void Clear()
		=> throw new InvalidOperationException();

	public bool Contains(object? value)
		=> Array.IndexOf(_array, value) != -1;

	public int IndexOf(object? value)
		=> Array.IndexOf(_array, value);

	public void Insert(int index, object? value)
		=> throw new InvalidOperationException();

	public void Remove(object? value)
		=> throw new InvalidOperationException();

	public void RemoveAt(int index)
		=> throw new InvalidOperationException();

	public void CopyTo(Array array, int index)
		=> _array.CopyTo(array, index);

	public IEnumerator GetEnumerator()
		=> _array.GetEnumerator();
}
