using MCeToJava.Models;
using MCeToJava.NBT;
using MCeToJava.Registry;
using MCeToJava.Utils;
using SharpNBT;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MCeToJava
{
    // https://minecraft.wiki/w/Chunk_format
    internal sealed class Chunk
    {
        private const int BlockPerSubChunk = 16 * 16 * 16;

        public readonly int chunkX;
        public readonly int chunkZ;

        // bedrock ids
        public readonly int[] blocks = new int[16 * 256 * 16];
        public readonly NbtMap?[] blockEntities = new NbtMap[16 * 256 * 16];

        public Chunk(int x, int z)
        {
            chunkX = x;
            chunkZ = z;

            Array.Fill(blocks, BedrockBlocks.AIR);
        }

        public CompoundTag ToTag(ExportTarget exportTarget)
        {
            CompoundTag tag = new CompoundTag(null);

            tag["xPos"] = new IntTag("xPos", chunkX);
            tag["zPos"] = new IntTag("zPos", chunkZ);

            tag["Status"] = new StringTag("Status", "minecraft:full");
            tag["DataVersion"] = new IntTag("DataVersion", 3700);
            tag["isLightOn"] = new ByteTag("isLightOn", 1);

            ListTag sections = new ListTag("sections", TagType.Compound);
            tag["sections"] = sections;

            // init sections
            for (sbyte i = -5; i <= 20; i++)
            {
                CompoundTag section = new CompoundTag(null);

                section["Y"] = new ByteTag("Y", unchecked((byte)i));

                byte[] skylight = GC.AllocateUninitializedArray<byte>(2048);
                Array.Fill<byte>(skylight, 255);
                section["SkyLight"] = new ByteArrayTag("SkyLight", skylight);

                if (i != -5 && i != 20)
                {
                    CompoundTag biomes = new CompoundTag("biomes");

                    ListTag biomePalette = new ListTag("palette", TagType.String)
                    {
                        new StringTag(null, "minecraft:plains")
                    };

                    biomes["palette"] = biomePalette;

                    section["biomes"] = biomes;

                    CompoundTag blockStates = new CompoundTag("block_states");

                    ListTag statePalette = new ListTag("palette", TagType.Compound)
                    {
                        new CompoundTag(null, [new StringTag("Name", "fountain:solid_air")])
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
                            int id = this.blocks[(x * 256 + (y + chunkOffset)) * 16 + z];
                            blocks[y * 256 + z * 16 + x] = bedrockPalette.ComputeIfAbsent(id, _ => bedrockPalette.Count);
                        }
                    }
                }

                foreach (var (id, _) in bedrockPalette)
                {
                    string? nameAndState = JavaBlocks.GetNameAndState(id);
                    paletteTag.Add(WritePaletteEntry(nameAndState ?? JavaBlocks.GetNameAndState(BedrockBlocks.AIR), exportTarget));
                }

                blockStatesTag["data"] = WriteBitArray(blocks, bedrockPalette.Count, "data");
            }

            return tag;
        }

        /// <exception cref="Exception"></exception>
        private static CompoundTag WritePaletteEntry(ReadOnlySpan<char> name, ExportTarget exportTarget)
        {
            Debug.Assert(name.Length > 0);
            if (exportTarget != ExportTarget.Vienna && name.StartsWith("fountain"))
            {
                name = "minecraft:air";
            }

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
                Debug.Assert(equalsIndex != -1);
                Debug.Assert(equalsIndex < commaIndex);

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
                    if (dataIndex >= data.Length) break;

                    value |= ((long)data[dataIndex++] & ((1L << bits) - 1)) << (j * bits);
                }
                longArray[i] = value;
            }

            return new LongArrayTag(tagName, longArray);
        }
    }
}
