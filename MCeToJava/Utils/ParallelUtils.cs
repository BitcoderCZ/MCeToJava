namespace MCeToJava.Utils;

internal static class ParallelUtils
{
#if DEBUG
	public static readonly ParallelOptions DefaultOptions = new ParallelOptions() { MaxDegreeOfParallelism = 1 };
#else
	public static readonly ParallelOptions DefaultOptions = new ParallelOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount };
#endif
}
