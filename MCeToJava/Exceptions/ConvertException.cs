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
