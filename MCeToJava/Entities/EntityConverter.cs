// <copyright file="EntityConverter.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using MathUtils.Vectors;
using MCeToJava.Models;
using MCeToJava.Models.MCE;
using MCeToJava.Utils;
using Serilog;
using SharpNBT;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MCeToJava.Entities;

internal static partial class EntityConverter
{
	private static readonly FrozenDictionary<string, string> EarthNameToJava = new Dictionary<string, string>()
	{
		["genoa:amber_chicken"] = "minecraft:chicken",
		["genoa:bronzed_chicken"] = "minecraft:chicken",
		["genoa:cluck_shroom"] = "minecraft:chicken",
		["genoa:fancy_chicken"] = "minecraft:chicken",
		["genoa:gold_crested_chicken"] = "minecraft:chicken",
		["genoa:midnight_chicken"] = "minecraft:chicken",
		["genoa:skewbald_chicken"] = "minecraft:chicken",
		["genoa:stormy_chicken"] = "minecraft:chicken",
		["genoa:albino_cow"] = "minecraft:cow",
		["genoa:ashen_cow"] = "minecraft:cow",
		["genoa:cookie_cow"] = "minecraft:cow",
		["genoa:cream_cow"] = "minecraft:cow",
		["genoa:dairy_cow"] = "minecraft:cow",
		["genoa:moo_bloom"] = "minecraft:cow",
		["genoa:moolip"] = "minecraft:cow",
		["genoa:pinto_cow"] = "minecraft:cow",
		["genoa:sunset_cow"] = "minecraft:cow",
		["genoa:umbra_cow"] = "minecraft:cow",
		["genoa:wooly_cow"] = "minecraft:cow",
		["genoa:rabbit"] = "minecraft:rabbit",
		["genoa:jolly_llama"] = "minecraft:llama",
		["genoa:dried_mud_pig"] = "minecraft:pig", // the page redirects to normal Muddy Pig, so I'm not sure if this is even real
		["genoa:mottled_pig"] = "minecraft:pig",
		["genoa:mud_pig"] = "minecraft:pig",
		["genoa:pale_pig"] = "minecraft:pig",
		["genoa:piebald_pig"] = "minecraft:pig",
		["genoa:pink_footed_pig"] = "minecraft:pig",
		["genoa:sooty_pig"] = "minecraft:pig",
		["genoa:spotted_pig"] = "minecraft:pig",
		["genoa:bold_striped_rabbit"] = "minecraft:rabbit",
		["genoa:freckled_rabbit"] = "minecraft:rabbit",
		["genoa:harelequin_rabbit"] = "minecraft:rabbit",
		["genoa:jumbo_rabbit"] = "minecraft:rabbit",
		["genoa:muddy_foot_rabbit"] = "minecraft:rabbit",
		["genoa:vested_rabbit"] = "minecraft:rabbit",
		["genoa:flecked_sheep"] = "minecraft:sheep",
		["genoa:fuzzy_sheep"] = "minecraft:sheep",
		["genoa:inky_sheep"] = "minecraft:sheep",
		["genoa:long_nosed_sheep"] = "minecraft:sheep",
		["genoa:patched_sheep"] = "minecraft:sheep",
		["genoa:rainbow_sheep"] = "minecraft:sheep",
		["genoa:rocky_sheep"] = "minecraft:sheep",
		["genoa:horned_sheep"] = "minecraft:sheep",
		["genoa:melon_golem"] = "minecraft:snow_golem",
		["genoa:glow_squid"] = "minecraft:squid",
		["genoa:skeleton_wolf"] = "minecraft:wolf",
	}.ToFrozenDictionary();

	// list of the custom mobs currently supported by fountain
	private static readonly FrozenSet<string> MappedEntities = new string[]
	{
		"genoa:amber_chicken",
		"genoa:bronzed_chicken",
		"genoa:gold_crested_chicken",
		"genoa:midnight_chicken",
		"genoa:skewbald_chicken",
		"genoa:stormy_chicken",

		"genoa:albino_cow",
		"genoa:ashen_cow",
		"genoa:cookie_cow",
		"genoa:cream_cow",
		"genoa:dairy_cow",
		"genoa:pinto_cow",
		"genoa:sunset_cow",
		"genoa:umbra_cow",
		"genoa:wooly_cow",

		"genoa:mottled_pig",
		"genoa:pale_pig",
		"genoa:piebald_pig",
		"genoa:pink_footed_pig",
		"genoa:sooty_pig",
		"genoa:spotted_pig",

		"genoa:bold_striped_rabbit",
		"genoa:freckled_rabbit",
		"genoa:harelequin_rabbit",
		"genoa:muddy_foot_rabbit",
		"genoa:vested_rabbit",

		"genoa:flecked_sheep",
		"genoa:inky_sheep",
		"genoa:long_nosed_sheep",
		"genoa:patched_sheep",
		"genoa:rainbow_sheep",
		"genoa:rocky_sheep",

		"genoa:genoa_slime",
		"genoa:genoa_slime_half",
		"genoa:genoa_slime_quarter",
		"genoa:tropical_slime",
	}.ToFrozenSet();

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

	private static readonly ImmutableArray<Tag> CanAngryTags =
	[
		new IntTag("AngerTime", 0),
	];

	private static readonly ImmutableArray<Tag> CanTameTags =
	[
		new ByteTag("Sitting", false), // TODO
	];
	#endregion

	public static CompoundTag? Convert(Entity entity, ConvertTarget target, ILogger logger)
	{
		if (entity.Name == "minecraft:persona_mob")
		{
			return null; // map to villager?
		}

		string javaName = EarthNameToJava.GetValueOrDefault(entity.Name) ?? entity.Name;

		if (!EntityInfo.Info.TryGetValue(javaName, out var info))
		{
			logger.Warning($"No info defined for entity '{entity.Name}'.");
			return null;
		}

		CompoundTag tag = new CompoundTag(null);

		WriteSharedTags(tag, target == ConvertTarget.Vienna && MappedEntities.Contains(entity.Name) ? entity.Name : javaName, entity.Position, entity.Rotation);

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

		if (EnumUtils.HasFlag(in info.Categories, EntityCategories.CanAngry))
		{
			WriteCanAngryTags(tag);
		}

		if (EnumUtils.HasFlag(in info.Categories, EntityCategories.CanTame))
		{
			WriteCanTameTags(tag);
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

	private static void WriteCanAngryTags(CompoundTag tag)
	{
		foreach (var item in CanAngryTags)
		{
			tag.Add(item);
		}
	}

	private static void WriteCanTameTags(CompoundTag tag)
	{
		foreach (var item in CanTameTags)
		{
			tag.Add(item);
		}
	}
}
