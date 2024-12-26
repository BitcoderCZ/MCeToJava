// <copyright file="EntityConverter.Slime.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using MCeToJava.Models.MCE;
using SharpNBT;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace MCeToJava.Entities;

internal static partial class EntityConverter
{
	private static class Slime
	{
		public static void Convert(Entity entity, CompoundTag tag)
		{
			tag.Add(new ByteTag("wasOnGround", true));

			if (entity.Name == "genoa:tropical_slime")
			{
				tag["Health"] = new FloatTag("Health", 16f);
				tag.Add(new IntTag("Size", 1)); // medium
				return;
			}

			// also has variant in extra data (saw one with '2') but not sure what that's used for
			if (entity.ExtraData is not null)
			{
				if (entity.ExtraData.TryGetValue("slime_size", out JsonNode? slimeSizeNode) && slimeSizeNode.GetValueKind() == JsonValueKind.Number)
				{
					float size = slimeSizeNode.GetValue<float>();
					if (size < 0f)
					{
						size = 0f;
					}

					tag.Add(new IntTag("Size", (int)size));
					return;
				}
			}

			tag.Add(new IntTag("Size", 1)); // medium
		}
	}
}