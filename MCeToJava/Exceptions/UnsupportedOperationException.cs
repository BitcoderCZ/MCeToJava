// <copyright file="UnsupportedOperationException.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

namespace MCeToJava.Exceptions;

internal sealed class UnsupportedOperationException : Exception
{
	public UnsupportedOperationException()
		: base()
	{
	}

	public UnsupportedOperationException(string? message)
		: base(message)
	{
	}
}
