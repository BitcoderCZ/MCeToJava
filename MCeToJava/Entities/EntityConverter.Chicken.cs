// <copyright file="EntityConverter.Chicken.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using MCeToJava.Models.MCE;
using SharpNBT;

namespace MCeToJava.Entities;

internal static partial class EntityConverter
{
	private static class Chicken
	{
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Must match a delegate signature.")]
		public static void Convert(Entity entity, CompoundTag tag)
		{
			tag.Add(new IntTag("EggLayTime", Random.Shared.Next(6000, 12000)));
			tag.Add(new ByteTag("IsChickenJockey", false));
		}
	}
}