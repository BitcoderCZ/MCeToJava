// <copyright file="EntityConverter.Spider.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using MCeToJava.Models.MCE;
using SharpNBT;

namespace MCeToJava.Entities;

internal static partial class EntityConverter
{
	private static class Spider
	{
		public static void Convert(Entity entity, CompoundTag tag)
		{
			if (entity.Name == "genoa:bone_spider")
			{
				tag["Health"] = new FloatTag("Health", 32f); // normal spider has 16
			}
		}
	}
}