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
