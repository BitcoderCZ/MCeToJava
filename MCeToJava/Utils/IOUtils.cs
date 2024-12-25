// <copyright file="IOUtils.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using System.IO.Compression;

namespace MCeToJava.Utils;

internal static class IOUtils
{
	public static bool IsDirectory(this ZipArchiveEntry entry)
		=> entry.FullName.EndsWith('/') || entry.FullName.EndsWith('\\') || entry.Name == string.Empty;
}
