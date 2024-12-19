using SharpNBT;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCeToJava.Utils
{
	internal static class NbtBuilder
	{
		public sealed class Compound
		{
			private readonly LinkedList<Tag> tags = new();

			public Compound()
			{
			}

			public CompoundTag Build(string? name)
			{
				CompoundTag tag = new CompoundTag(name);
				foreach (var item in tags)
				{
					tag[item.Name!] = item;
				}

				return tag;
			}

			public Compound Put(string name, int value)
			{
				IntTag tag = new IntTag(name, value);
				tags.AddLast(tag);
				return this;
			}

			public Compound Put(string name, byte value)
			{
				ByteTag tag = new ByteTag(name, value);
				tags.AddLast(tag);
				return this;
			}

			public Compound Put(string name, short value)
			{
				ShortTag tag = new ShortTag(name, value);
				tags.AddLast(tag);
				return this;
			}

			public Compound Put(string name, long value)
			{
				LongTag tag = new LongTag(name, value);
				tags.AddLast(tag);
				return this;
			}

			public Compound Put(string name, float value)
			{
				FloatTag tag = new FloatTag(name, value);
				tags.AddLast(tag);
				return this;
			}

			public Compound Put(string name, double value)
			{
				DoubleTag tag = new DoubleTag(name, value);
				tags.AddLast(tag);
				return this;
			}

			public Compound Put(string name, string value)
			{
				StringTag tag = new StringTag(name, value);
				tags.AddLast(tag);
				return this;
			}

			public Compound Put(string name, int[] value)
			{
				IntArrayTag tag = new IntArrayTag(name, value);
				tags.AddLast(tag);
				return this;
			}

			public Compound Put(string name, byte[] value)
			{
				ByteArrayTag tag = new ByteArrayTag(name, value);
				tags.AddLast(tag);
				return this;
			}

			public Compound Put(string name, long[] value)
			{
				LongArrayTag tag = new LongArrayTag(name, value);
				tags.AddLast(tag);
				return this;
			}

			public Compound Put(string name, Compound value)
			{
				CompoundTag tag = value.Build(name);
				tags.AddLast(tag);
				return this;
			}

			public Compound Put(string name, List value)
			{
				ListTag tag = value.Build(name);
				tags.AddLast(tag);
				return this;
			}
		}

		public sealed class List
		{
			private readonly TagType type;
			private readonly LinkedList<Tag> tags = new();

			public List(TagType type)
			{
				this.type = type;
			}

			public ListTag Build(string? name)
			{
				ListTag tag = new ListTag(name, type);
				foreach (var item in tags)
				{
					tag.Add(item);
				}

				return tag;
			}

			public List Add(int value)
			{
				IntTag tag = new IntTag(null, value);
				tags.AddLast(tag);
				return this;
			}

			public List Add(byte value)
			{
				ByteTag tag = new ByteTag(null, value);
				tags.AddLast(tag);
				return this;
			}

			public List Add(short value)
			{
				ShortTag tag = new ShortTag(null, value);
				tags.AddLast(tag);
				return this;
			}

			public List Add(long value)
			{
				LongTag tag = new LongTag(null, value);
				tags.AddLast(tag);
				return this;
			}

			public List Add(float value)
			{
				FloatTag tag = new FloatTag(null, value);
				tags.AddLast(tag);
				return this;
			}

			public List Add(double value)
			{
				DoubleTag tag = new DoubleTag(null, value);
				tags.AddLast(tag);
				return this;
			}

			public List Add(string value)
			{
				StringTag tag = new StringTag(null, value);
				tags.AddLast(tag);
				return this;
			}

			public List Add(int[] value)
			{
				IntArrayTag tag = new IntArrayTag(null, value);
				tags.AddLast(tag);
				return this;
			}

			public List Add(byte[] value)
			{
				ByteArrayTag tag = new ByteArrayTag(null, value);
				tags.AddLast(tag);
				return this;
			}

			public List Add(long[] value)
			{
				LongArrayTag tag = new LongArrayTag(null, value);
				tags.AddLast(tag);
				return this;
			}

			public List Add(Compound value)
			{
				CompoundTag tag = value.Build(null);
				tags.AddLast(tag);
				return this;
			}

			public List Add(List value)
			{
				ListTag tag = value.Build(null);
				tags.AddLast(tag);
				return this;
			}
		}
	}
}
