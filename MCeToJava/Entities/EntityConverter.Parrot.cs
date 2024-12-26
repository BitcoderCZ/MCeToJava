// <copyright file="EntityConverter.Parrot.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using MCeToJava.Models.MCE;
using SharpNBT;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace MCeToJava.Entities;

internal static partial class EntityConverter
{
	private static class Parrot
	{
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Must match a delegate signature.")]
		public static void Convert(Entity entity, CompoundTag tag)
		{
			if (entity.ExtraData is not null && entity.ExtraData.TryGetValue("variant", out var variantNode) && variantNode.GetValueKind() == JsonValueKind.Number)
			{
				int val = variantNode.GetValue<int>();

				if (val >= 0 && val <= 4)
				{
					tag.Add(new IntTag("Variant", val));
					return;
				}
			}

			tag.Add(new IntTag("Variant", 0));
		}
	}
}