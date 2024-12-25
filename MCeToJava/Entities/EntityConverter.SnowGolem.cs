// <copyright file="EntityConverter.SnowGolem.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using MCeToJava.Models.MCE;
using SharpNBT;

namespace MCeToJava.Entities;

internal static partial class EntityConverter
{
	private static class SnowGolem
	{
#pragma warning disable IDE0022 // Use expression body for method
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Must match a delegate signature.")]
		public static void Convert(Entity entity, CompoundTag tag)
		{
			// only melon golem exists in earth
			tag.Add(new ByteTag("Pumpkin", true));
		}
#pragma warning restore IDE0022 // Use expression body for method
	}
}