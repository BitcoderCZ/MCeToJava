using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MCeToJava.Utils
{
	internal static class BinaryReaderExtensions
	{
		public static uint ReadUInt32BE(this BinaryReader reader)
		{
			Span<byte> buffer = stackalloc byte[sizeof(uint)];
			reader.ReadChecked(buffer);
			buffer.Reverse();
			return BitConverter.ToUInt32(buffer);
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
}
