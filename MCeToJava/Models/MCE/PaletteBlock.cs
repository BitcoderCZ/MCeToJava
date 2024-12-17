using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCeToJava.Models.MCE
{
	internal struct PaletteBlock
	{
		// afaik ushort, but might actually be uint, idk...
		public ushort Data;
		public string Name;
	}
}
