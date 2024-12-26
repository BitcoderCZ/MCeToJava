// <copyright file="EntityConverter.IronGolem.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using MCeToJava.Models.MCE;
using SharpNBT;

namespace MCeToJava.Entities;

internal static partial class EntityConverter
{
	private static class IronGolem
	{
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Must match a delegate signature.")]
		public static void Convert(Entity entity, CompoundTag tag)
			=> new ByteTag("PlayerCreated", false); // TODO?
	}
}