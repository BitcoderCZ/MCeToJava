// <copyright file="ParallelUtils.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace MCeToJava.Utils;

internal static class ParallelUtils
{
#if DEBUG
	public static readonly ParallelOptions DefaultOptions = new ParallelOptions() { MaxDegreeOfParallelism = 1 };
#else
	public static readonly ParallelOptions DefaultOptions = new ParallelOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount };
#endif
}
