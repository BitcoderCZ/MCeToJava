// <copyright file="EntityConverter.Sheep.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using MCeToJava.Models.MCE;
using SharpNBT;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace MCeToJava.Entities;

internal static partial class EntityConverter
{
	private static class Sheep
	{
		public static void Convert(Entity entity, CompoundTag tag)
		{
			tag.Add(new ByteTag("Color", entity.Name switch
			{
				"genoa:flecked_sheep" => 12, // brown
				"genoa:fuzzy_sheep" => 0, // white
				"genoa:inky_sheep" => 8, // light gray
				"genoa:long_nosed_sheep" => 12, // brown
				"genoa:patched_sheep" => 0, // white
				"genoa:rainbow_sheep" => 14, // red
				"genoa:rocky_sheep" => 7, // gray
				_ => 0, // white
			}));

			if (entity.ExtraData is not null && entity.ExtraData.TryGetValue("is_sheared", out JsonNode? isShearedNode))
			{
				var valueKind = isShearedNode.GetValueKind();

				// for some reason can be eather true/false or interger, why???
				if (valueKind == JsonValueKind.True || valueKind == JsonValueKind.False)
				{
					tag.Add(new ByteTag("Sheared", isShearedNode.GetValue<bool>()));
				}
				else if (valueKind == JsonValueKind.Number)
				{
					tag.Add(new ByteTag("Sheared", isShearedNode.GetValue<float>() != 0f));
				}
				else
				{
					tag.Add(new ByteTag("Sheared", false));
				}
			}
			else
			{
				tag.Add(new ByteTag("Sheared", false));
			}
		}
	}
}