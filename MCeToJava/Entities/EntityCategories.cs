// <copyright file="EntityCategories.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCeToJava.Entities;

[Flags]
internal enum EntityCategories
{
	Mob = 1 << 0,
	CanBreed = 1 << 1,
	Zombie = 1 << 2,
	RaidMob = 1 << 3,
}