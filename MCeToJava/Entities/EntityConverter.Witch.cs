// <copyright file="EntityConverter.Witch.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using MCeToJava.Models.MCE;
using SharpNBT;

namespace MCeToJava.Entities;

internal static partial class EntityConverter
{
	private static class Witch
	{
		public static void Convert(Entity entity, CompoundTag tag)
		{
			if (entity.Name == "genoa:viler_witch")
			{
				tag["Health"] = new FloatTag("Health", 30f); // normally 26
			}
		}
	}
}
