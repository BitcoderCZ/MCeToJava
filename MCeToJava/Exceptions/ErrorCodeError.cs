using FluentResults;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCeToJava.Exceptions
{
	internal sealed class ErrorCodeError : Error
	{
		public ErrorCodeError(string message, int errorCode)
			: base(message)
		{
			if (errorCode == MCeToJava.ErrorCode.Success)
			{
				throw new ArgumentException($"{nameof(errorCode)} cannot be {nameof(MCeToJava.ErrorCode)}.{nameof(MCeToJava.ErrorCode.Success)}.", nameof(errorCode));
			}

			ErrorCode = errorCode;
		}

		public int ErrorCode { get; }
	}
}
