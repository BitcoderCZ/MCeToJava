using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCeToJava.Exceptions
{
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
}
