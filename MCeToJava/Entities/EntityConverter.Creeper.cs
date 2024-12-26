// <copyright file="EntityConverter.Creeper.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using MCeToJava.Models.MCE;
using SharpNBT;

namespace MCeToJava.Entities;

internal static partial class EntityConverter
{
	private static class Creeper
	{
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Must match a delegate signature.")]
		public static void Convert(Entity entity, CompoundTag tag)
		{
			tag.Add(new ByteTag("ExplosionRadius", 3));
			tag.Add(new ShortTag("Fuse", 30)); // TODO?
			tag.Add(new ByteTag("ignited", false)); // TODO?
			tag.Add(new ByteTag("powered", false)); // TODO?
		}
	}
}