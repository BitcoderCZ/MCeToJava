using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCeToJava.Exceptions
{
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
}
