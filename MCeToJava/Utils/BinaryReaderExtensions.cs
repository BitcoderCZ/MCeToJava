using System.Buffers.Binary;
using System.Diagnostics;

namespace MCeToJava.Utils;

internal static class BinaryReaderExtensions
{
	public static uint ReadUInt32BigEndian(this BinaryReader reader)
	{
		Span<byte> buffer = stackalloc byte[sizeof(uint)];
		reader.ReadChecked(buffer);
		uint res1 = BinaryPrimitives.ReadUInt32BigEndian(buffer);

		buffer.Reverse();
		uint res2 = BitConverter.ToUInt32(buffer);

		Debug.Assert(res1 == res2); // if this doesn't fail, remove res2
		return res1;
	}

	private static void ReadChecked(this BinaryReader reader, Span<byte> buffer)
	{
		int read = reader.Read(buffer);

		if (read != buffer.Length)
		{
			throw new EndOfStreamException($"{buffer.Length} bytes required from stream, but only {read} returned.");
		}
	}
}
