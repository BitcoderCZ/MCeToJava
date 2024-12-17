using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCeToJava.Utils
{
	internal static class IOUtils
	{
		public static bool IsDirectory(this ZipArchiveEntry entry)
			=> entry.FullName.EndsWith('/') || entry.FullName.EndsWith('\\') || entry.Name == string.Empty;
	}
}
