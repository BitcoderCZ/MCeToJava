// <copyright file="EntityConverter.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using MathUtils.Vectors;
using MCeToJava.Models.MCE;
using MCeToJava.Utils;
using Serilog;
using SharpNBT;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MCeToJava.Entities;

internal static partial class EntityConverter
{
	private static readonly FrozenDictionary<string, string> ToJavaName = new Dictionary<string, string>()
	{
		["genoa:bold_striped_rabbit"] = "minecraft:rabbit",
		["genoa:viler_witch"] = "minecraft:witch",
	}.ToFrozenDictionary();

	#region Tags
	private static readonly ImmutableArray<Tag> SharedTags =
	[
		new ShortTag("Air", (short)300),
		new FloatTag("FallDistance", 0f),
		new ShortTag("Fire", (short)-20),
		new ByteTag("Glowing", false),
		new ByteTag("HasVisualFire", false),
		new ByteTag("Invulnerable", false),
		new ListTag("Motion", TagType.Double, [new DoubleTag(null, 0d), new DoubleTag(null, 0d), new DoubleTag(null, 0d)]),
		new ByteTag("NoGravity", false),
		new ByteTag("OnGround", false),
		new ListTag("Passengers", TagType.Compound, 0),
		new IntTag("PortalCooldown", 300),
		new ByteTag("Silent", false),
		new ListTag("Tags", TagType.String, 0),
	];

	private static readonly ImmutableArray<Tag> MobTags =
	[
		new FloatTag("AbsorptionAmount", 0f),
		new ListTag("ArmorDropChances", TagType.Float, [new FloatTag(null, 0.25f), new FloatTag(null, 0.25f), new FloatTag(null, 0.25f), new FloatTag(null, 0.25f)]),
		new ListTag("ArmorItems", TagType.Compound, 0),
		new ListTag("attributes", TagType.Compound, 0),
		new FloatTag("body_armor_drop_chance", 0.25f),
		new ShortTag("DeathTime", 0),
		new ByteTag("FallFlying", false),
		new IntTag("HurtByTimestamp", 0),
		new ShortTag("HurtTime", 0),
		new ListTag("HandDropChances", TagType.Float, [new FloatTag(null, 1f), new FloatTag(null, 1f)]),
		new ListTag("HandItems", TagType.Compound, 0),
		new ByteTag("LeftHanded", false), // ???
		new ByteTag("NoAI", false),
		new ByteTag("PersistenceRequired", true),
	];

	private static readonly ImmutableArray<Tag> CanBreadTags =
	[
		new IntTag("Age", 0),
		new IntTag("ForcedAge", 0),
		new IntTag("InLove", 0),
	];

	private static readonly ImmutableArray<Tag> ZombieTags =
	[
		new ByteTag("CanBreakDoors", true),
		new IntTag("DrownedConversionTime", -1),
		new IntTag("InWaterTime", -1),
		new ByteTag("IsBaby", false),
	];

	private static readonly ImmutableArray<Tag> RaidMobTags =
	[
		new ByteTag("CanJoinRaid", false),
		new ByteTag("PatrolLeader", false),
		new ByteTag("Patrolling", false),
	];
	#endregion

	public static CompoundTag? Convert(Entity entity, ILogger logger)
	{
		string javaName = ToJavaName.GetValueOrDefault(entity.Name) ?? entity.Name;

		if (!EntityInfo.Info.TryGetValue(javaName, out var info))
		{
			logger.Warning($"No info defined for entity '{entity.Name}'.");
			return null;
		}

		CompoundTag tag = new CompoundTag(null);

		WriteSharedTags(tag, javaName, entity.Position, entity.Rotation);

		if (EnumUtils.HasFlag(in info.Categories, EntityCategories.Mob))
		{
			WriteMobTags(tag, info);
		}

		if (EnumUtils.HasFlag(in info.Categories, EntityCategories.CanBreed))
		{
			WriteCanBreedTags(tag);
		}

		if (EnumUtils.HasFlag(in info.Categories, EntityCategories.Zombie))
		{
			WriteZombieTags(tag);
		}

		if (EnumUtils.HasFlag(in info.Categories, EntityCategories.RaidMob))
		{
			WriteRaidMobTags(tag);
		}

		info.ConvertFunc?.Invoke(entity, tag);

		return tag;
	}

	private static void WriteSharedTags(CompoundTag tag, string id, double3 pos, float2 rot)
	{
		tag.Add(new StringTag("id", id));
		tag.Add(new ListTag("Pos", TagType.Double, [new DoubleTag(null, pos.X), new DoubleTag(null, pos.Y), new DoubleTag(null, pos.Z)]));
		tag.Add(new ListTag("Rotation", TagType.Float, [new FloatTag(null, rot.X), new FloatTag(null, rot.Y)]));

		Guid uuid = Guid.NewGuid(); // big endian

		Span<byte> uuidBytes = stackalloc byte[16];

		bool writeSucceeded = uuid.TryWriteBytes(uuidBytes);
		Debug.Assert(writeSucceeded, $"Writing {nameof(uuid)} to {nameof(uuidBytes)} should always succeed.");

		ListTag uuidTag = new ListTag("UUID", TagType.Int, 4);
		foreach (int i in MemoryMarshal.Cast<byte, int>(uuidBytes))
		{
			uuidTag.Add(new IntTag(null, i));
		}

		Debug.Assert(uuidTag.Count == 4, $"{nameof(uuidTag)} should have 4 items.");

		tag["UUID"] = uuidTag;

		foreach (var item in SharedTags)
		{
			tag.Add(item);
		}
	}

	private static void WriteMobTags(CompoundTag tag, EntityInfo info)
	{
		tag.Add(new ByteTag("CanPickUpLoot", false)); // TODO
		tag.Add(new FloatTag("Health", info.Health));

		foreach (var item in MobTags)
		{
			tag.Add(item);
		}
	}

	private static void WriteCanBreedTags(CompoundTag tag)
	{
		foreach (var item in CanBreadTags)
		{
			tag.Add(item);
		}
	}

	private static void WriteZombieTags(CompoundTag tag)
	{
		foreach (var item in ZombieTags)
		{
			tag.Add(item);
		}
	}

	private static void WriteRaidMobTags(CompoundTag tag)
	{
		foreach (var item in RaidMobTags)
		{
			tag.Add(item);
		}
	}
}
