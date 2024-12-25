// <copyright file="EntityConverter.EntityInfo.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using MCeToJava.Models.MCE;
using SharpNBT;
using System.Collections.Frozen;

namespace MCeToJava.Entities;

internal static partial class EntityConverter
{
	private readonly struct EntityInfo
	{
		public static readonly FrozenDictionary<string, EntityInfo> Info = new Dictionary<string, EntityInfo>
		{
			["minecraft:chicken"] = new(EntityCategories.CanBreed | EntityCategories.Mob, 4f, Chicken.Convert),
			["minecraft:cow"] = new(EntityCategories.CanBreed | EntityCategories.Mob, 10f),
			["minecraft:llama"] = new(EntityCategories.CanBreed | EntityCategories.Mob, 15f, Llama.Convert),
			["minecraft:pig"] = new(EntityCategories.CanBreed | EntityCategories.Mob, 10f, Pig.Convert),
			["minecraft:rabbit"] = new(EntityCategories.CanBreed | EntityCategories.Mob, 3f, Rabbit.Convert),
			["minecraft:sheep"] = new(EntityCategories.CanBreed | EntityCategories.Mob, 8f, Sheep.Convert),
			["minecraft:skeleton"] = new(EntityCategories.Mob, 20f, Skeleton.Convert),
			["minecraft:snow_golem"] = new(EntityCategories.Mob, 4f, SnowGolem.Convert),
			["minecraft:spider"] = new(EntityCategories.Mob, 16f),
			["minecraft:squid"] = new(EntityCategories.Mob, 10f),
			["minecraft:witch"] = new(EntityCategories.Mob | EntityCategories.RaidMob, 26f, Witch.Convert),
			["minecraft:wolf"] = new(EntityCategories.CanAngry | EntityCategories.CanTame | EntityCategories.CanBreed | EntityCategories.Mob, 26f, Wolf.Convert),
			["minecraft:zombie"] = new(EntityCategories.Mob | EntityCategories.Zombie, 20f),
		}.ToFrozenDictionary();

		public readonly EntityCategories Categories;
		public readonly float Health;
		public readonly Action<Entity, CompoundTag>? ConvertFunc;

		public EntityInfo(EntityCategories categories, float health, Action<Entity, CompoundTag>? convertFunc = null)
		{
			Categories = categories;
			Health = health;
			ConvertFunc = convertFunc;
		}
	}
}