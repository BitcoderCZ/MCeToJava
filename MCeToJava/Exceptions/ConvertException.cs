// <copyright file="ConvertException.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace MCeToJava.Exceptions;

internal sealed class ConvertException : Exception
{
	public ConvertException()
		: base()
	{
	}

	public ConvertException(string? message)
		: base(message)
	{
	}
}
