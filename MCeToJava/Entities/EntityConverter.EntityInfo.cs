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
			["minecraft:rabbit"] = new(EntityCategories.CanBreed | EntityCategories.Mob, 3, Rabbit.Convert),
			["minecraft:witch"] = new(EntityCategories.Mob | EntityCategories.RaidMob, 20f),
			["minecraft:zombie"] = new(EntityCategories.CanBreed | EntityCategories.Mob | EntityCategories.Zombie, 20f),
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
