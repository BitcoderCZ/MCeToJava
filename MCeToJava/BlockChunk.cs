﻿// <copyright file="BlockChunk.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using MCeToJava.NBT;
using MCeToJava.Registry;
using MCeToJava.Utils;
using Serilog;
using SharpNBT;
using System.Diagnostics;

namespace MCeToJava;

// https://minecraft.wiki/w/Chunk_format
internal sealed class BlockChunk
{
	public const int SolidAirId = int.MinValue;

	public readonly int ChunkX;
	public readonly int ChunkZ;

	// bedrock ids
	public readonly int[] Blocks = new int[16 * 256 * 16];
	public readonly List<NbtMap> BlockEntities = [];

	private const int BlockPerSubChunk = 16 * 16 * 16;

	private static readonly ListTag PostProcessingTag;

	static BlockChunk()
	{
		PostProcessingTag = new ListTag("PostProcessing", TagType.List, 24);

		ListTag postProcessingList = new ListTag(null, TagType.Short, 16 * 16 * 16);

		for (short i = 0; i < 16 * 16 * 16; i++)
		{
			postProcessingList.Add(new ShortTag(null, i)); // x | y << 4 | z << 8
		}

		for (int i = 0; i < 24; i++)
		{
			PostProcessingTag.Add(postProcessingList);
		}
	}

	public BlockChunk(int x, int z)
	{
		ChunkX = x;
		ChunkZ = z;

		Array.Fill(Blocks, BedrockBlocks.AirId);
	}

	public CompoundTag ToTag(string biome, bool updateBlocks, ILogger logger)
	{
		CompoundTag tag = new CompoundTag(null)
		{
			["xPos"] = new IntTag("xPos", ChunkX),
			["zPos"] = new IntTag("zPos", ChunkZ),

			["Status"] = new StringTag("Status", updateBlocks ? "minecraft:features" : "minecraft:full"), // "minecraft:features" - "proto chunk", needed for PostProcessing and light
			["DataVersion"] = new IntTag("DataVersion", 3700),
		};

		ListTag sections = new ListTag("sections", TagType.Compound);
		tag["sections"] = sections;

		// init sections
		for (sbyte i = -5; i <= 20; i++)
		{
			CompoundTag section = new CompoundTag(null)
			{
				["Y"] = new ByteTag("Y", unchecked((byte)i)),
			};

			if (!updateBlocks)
			{
				byte[] skylight = GC.AllocateUninitializedArray<byte>(2048);
				Array.Fill<byte>(skylight, 255);
				section["SkyLight"] = new ByteArrayTag("SkyLight", skylight);
			}

			if (i != -5 && i != 20)
			{
				CompoundTag biomes = new CompoundTag("biomes");

				ListTag biomePalette = new ListTag("palette", TagType.String)
				{
					new StringTag(null, biome),
				};

				biomes["palette"] = biomePalette;

				section["biomes"] = biomes;

				CompoundTag blockStates = new CompoundTag("block_states");

				ListTag statePalette = new ListTag("palette", TagType.Compound)
				{
					new CompoundTag(null, [new StringTag("Name", "fountain:solid_air")]),
				};

				blockStates["palette"] = statePalette;

				section["block_states"] = blockStates;
			}

			sections.Add(section);
		}

		for (int subchunkY = 0; subchunkY < 16; subchunkY++)
		{
			int sectionIndex = subchunkY + 4 + 1; // Java world height starts at -64, plus one section for bottommost lighting

			int chunkOffset = subchunkY * 16;

			CompoundTag sectionTag = (CompoundTag)sections[sectionIndex];

			CompoundTag blockStatesTag = (CompoundTag)sectionTag["block_states"];

			ListTag paletteTag = (ListTag)blockStatesTag["palette"];

			paletteTag.Clear();

			Dictionary<int, int> bedrockPalette = [];
			int[] blocks = GC.AllocateUninitializedArray<int>(BlockPerSubChunk);

			for (int x = 0; x < 16; x++)
			{
				for (int y = 0; y < 16; y++)
				{
					for (int z = 0; z < 16; z++)
					{
						int id = Blocks[(((x * 256) + (y + chunkOffset)) * 16) + z];
						blocks[(y * 256) + (z * 16) + x] = bedrockPalette.ComputeIfAbsent(id, _ => bedrockPalette.Count);
					}
				}
			}

			foreach (var (id, _) in bedrockPalette)
			{
				string? nameAndState = id == SolidAirId ? "fountain:solid_air" : JavaBlocks.GetNameAndState(id);
				paletteTag.Add(WritePaletteEntry(nameAndState ?? JavaBlocks.GetNameAndState(BedrockBlocks.AirId)));
			}

			Debug.Assert(bedrockPalette.Count > 0, "Palette shouldn't be empty at this point.");
			if (bedrockPalette.Count > 1)
			{
				blockStatesTag["data"] = WriteBitArray(blocks, bedrockPalette.Count, "data");
			}
		}

		ListTag blockEntities = new ListTag("block_entities", TagType.Compound);

		foreach (var blockEntity in BlockEntities)
		{
			if (!ValidateBlockEntity(blockEntity, logger))
			{
				continue;
			}

			CompoundTag entityTag = new CompoundTag(null);

			string entityType = ((string)blockEntity.Map["id"]).ToLowerInvariant(); // validated in Converter

			entityTag["id"] = new StringTag("id", entityType);
			entityTag["keepPacked"] = new ByteTag("keepPacked", false);
			entityTag["components"] = new CompoundTag("components");

			foreach (var (key, value) in blockEntity.Map)
			{
				var itemTag = NbtUtils.CreateTag(key, value);

				if (itemTag is not null && IsValidBlockEntityValue(key, value, entityType))
				{
					entityTag[key] = itemTag;
				}
			}

			blockEntities.Add(entityTag);
		}

		tag["block_entities"] = blockEntities;

		if (updateBlocks)
		{
			tag["PostProcessing"] = PostProcessingTag;
		}

		return tag;
	}

	private static CompoundTag WritePaletteEntry(ReadOnlySpan<char> name)
	{
		Debug.Assert(name.Length > 0, "Block name shouldn't be empty.");

		CompoundTag tag = new CompoundTag(null);

		int bracketIndex = name.IndexOf('[');

		if (bracketIndex == -1)
		{
			tag["Name"] = new StringTag("Name", new string(name));
			return tag;
		}

		tag["Name"] = new StringTag("Name", new string(name[..bracketIndex]));

		name = name[(bracketIndex + 1)..^1];

		CompoundTag properties = new CompoundTag("Properties");
		tag["Properties"] = properties;

		while (true)
		{
			int commaIndex = name.IndexOf(',');

			if (commaIndex == -1)
			{
				commaIndex = name.Length;
			}

			int equalsIndex = name.IndexOf('=');
			Debug.Assert(equalsIndex != -1, "Name should now contain '='.");
			Debug.Assert(equalsIndex < commaIndex, "'=' should be before ','.");

			string propName = new string(name[..equalsIndex]);
			string propVal = new string(name[(equalsIndex + 1)..commaIndex]);

			properties.Add(new StringTag(propName, propVal));

			if (commaIndex == name.Length)
			{
				break;
			}

			name = name[(commaIndex + 1)..];
		}

		return tag;
	}

	private static LongArrayTag WriteBitArray(int[] data, int maxValue, string tagName)
	{
		int bits = 4;
		for (int bits1 = 4; bits1 <= 64; bits1++)
		{
			if (maxValue <= (1 << bits1))
			{
				bits = bits1;
				break;
			}
		}

		int valuesPerLong = 64 / bits;
		long[] longArray = new long[(data.Length + valuesPerLong - 1) / valuesPerLong];

		int dataIndex = 0;
		for (int i = 0; i < longArray.Length; i++)
		{
			long value = 0;
			for (int j = 0; j < valuesPerLong; j++)
			{
				if (dataIndex >= data.Length)
				{
					break;
				}

				value |= (data[dataIndex++] & ((1L << bits) - 1)) << (j * bits);
			}

			longArray[i] = value;
		}

		return new LongArrayTag(tagName, longArray);
	}

	private static bool ValidateBlockEntity(NbtMap blockEntity, ILogger logger)
	{
		return Contains("id") && Contains("x") && Contains("y") && Contains("z");

		bool Contains(string name)
		{
			bool c = blockEntity.ContainsKey(name);
			if (!c)
			{
				logger.Warning($"Invalid block entity: Doesn't contain '{name}'.");
			}

			return c;
		}
	}

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Might be used when more block entities are supported.")]
	private static bool IsValidBlockEntityValue(string name, object value, string entityType)
	{
#pragma warning disable IDE0066 // Convert switch statement to expression
		switch (name)
		{
			// case "id": // added separately
			case "keepPacked":
			case "x":
			case "y":
			case "z":
			case "components":
				return true;
			default:
				return false;
		}
#pragma warning restore IDE0066 // Convert switch statement to expression
	}
}
