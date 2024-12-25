// <copyright file="EntityConverter.Rabbit.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using MCeToJava.Models.MCE;
using SharpNBT;

namespace MCeToJava.Entities;

internal static partial class EntityConverter
{
	private static class Rabbit
	{
		public static void Convert(Entity entity, CompoundTag tag)
		{
			tag.Add(new IntTag("MoreCarrotTicks", 0));

			// not sure that the names/ids are correct
			tag.Add(new IntTag("RabbitType", entity.Name switch
			{
				"genoa:bold_striped_rabbit" => 2, // black
				"genoa:rabbit" => 4, // "gold", just "rabbit" according to the wiki - https://minecraft.wiki/w/Earth:Desert_Rabbit
				"genoa:freckled_rabbit" => 1, // white
				"genoa:harelequin_rabbit" => 0, // brown
				"genoa:jumbo_rabbit" => 0, // brown
				"genoa:muddy_foot_rabbit" => 1, // white
				"genoa:vested _rabbit" => 3, // white_splotched
				_ => 0, // brown
			}));
		}
	}
}
