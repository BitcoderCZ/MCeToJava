// <copyright file="EntityConverter.Llama.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using MCeToJava.Models.MCE;
using SharpNBT;

namespace MCeToJava.Entities;

internal static partial class EntityConverter
{
	private static class Llama
	{
		public static void Convert(Entity entity, CompoundTag tag)
		{
			tag.Add(new ByteTag("Bred", false));
			tag.Add(new ByteTag("ChestedHorse", false));
			tag.Add(new ByteTag("EatingHaystack", false));
			tag.Add(new IntTag("Strength", 3));
			tag.Add(new ByteTag("Tame", false)); // TODO?
			tag.Add(new IntTag("Temper", 50)); // TODO?
			tag.Add(new IntTag("Variant", entity.Name switch
			{
				"genoa:jolly_llama" => 2, // brown
				_ => 0, // creamy
			}));
		}
	}
}
