// <copyright file="EntityChunk.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using MCeToJava.Entities;
using MCeToJava.Models;
using MCeToJava.Models.MCE;
using Serilog;
using SharpNBT;

namespace MCeToJava;

// https://minecraft.wiki/w/Entity_format
internal sealed class EntityChunk
{
	public readonly int ChunkX;
	public readonly int ChunkZ;

	public readonly List<Entity> Entities = [];

	public EntityChunk(int x, int z)
	{
		ChunkX = x;
		ChunkZ = z;
	}

	public CompoundTag ToTag(ConvertTarget target, ILogger logger)
	{
		CompoundTag tag = new CompoundTag(null)
		{
			["Position"] = new IntArrayTag("Position", [
				new IntTag(null, ChunkX),
				new IntTag(null, ChunkZ),
			]),
			["DataVersion"] = new IntTag("DataVersion", 3700),
		};

		ListTag entities = new ListTag("Entities", TagType.Compound, Entities.Count);

		foreach (var entity in Entities)
		{
			var entityNbt = EntityConverter.Convert(entity, target, logger);

			if (entityNbt is null)
			{
				continue;
			}

			entities.Add(entityNbt);
		}

		tag["Entities"] = entities;

		return tag;
	}
}
