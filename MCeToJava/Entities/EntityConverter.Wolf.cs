// <copyright file="EntityConverter.Wolf.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using MCeToJava.Models.MCE;
using SharpNBT;

namespace MCeToJava.Entities;

internal static partial class EntityConverter
{
	private static class Wolf
	{
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Must match a delegate signature.")]
		public static void Convert(Entity entity, CompoundTag tag)
		{
			// only skeleton wolf exists in earth
			tag.Add(new ByteTag("CollarColor", (byte)14));
			tag.Add(new StringTag("variant", "minecraft:pale"));
		}
	}
}