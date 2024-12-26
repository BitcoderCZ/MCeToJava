// <copyright file="EntityConverter.Salmon.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using MCeToJava.Models.MCE;
using SharpNBT;

namespace MCeToJava.Entities;

internal static partial class EntityConverter
{
	private static class Salmon
	{
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Must match a delegate signature.")]
		public static void Convert(Entity entity, CompoundTag tag)
		{
			tag.Add(new ByteTag("FromBucket", false)); // TODO?
			tag.Add(new StringTag("type", "medium")); // has a 'variant', but seems to always be 0
		}
	}
}