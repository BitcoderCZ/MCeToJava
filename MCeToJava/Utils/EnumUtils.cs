using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MCeToJava.Utils;

internal static class EnumUtils
{
	// HasFlag without defensive copy
	public static unsafe bool HasFlag<T>(ref readonly T value, T flag)
		where T : Enum
	{
		ref T valRef = ref Unsafe.AsRef(in value);

		switch (Unsafe.SizeOf<T>())
		{
			case 1:
				{
					byte flagVal = Unsafe.As<T, byte>(ref flag);
					return (Unsafe.As<T, byte>(ref valRef) & flagVal) == flagVal;
				}

			case 2:
				{
					ushort flagVal = Unsafe.As<T, ushort>(ref flag);
					return (Unsafe.As<T, ushort>(ref valRef) & flagVal) == flagVal;
				}
				
			case 4:
				{
					uint flagVal = Unsafe.As<T, uint>(ref flag);
					return (Unsafe.As<T, uint>(ref valRef) & flagVal) == flagVal;
				}

			case 8:
				{
					ulong flagVal = Unsafe.As<T, ulong>(ref flag);
					return (Unsafe.As<T, ulong>(ref valRef) & flagVal) == flagVal;
				}

			default:
				Debug.Fail("Invalid enum size.");
				throw new Exception("Invalid enum size.");
		}
	}
}
