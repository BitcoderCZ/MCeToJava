// <copyright file="EntityCategories.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace MCeToJava.Entities;

[Flags]
internal enum EntityCategories
{
	Mob = 1 << 0,
	CanBreed = 1 << 1,
	Zombie = 1 << 2,
	RaidMob = 1 << 3,
}