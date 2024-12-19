using System.IO.Compression;

namespace MCeToJava.Utils
{
	internal static class IOUtils
	{
		public static bool IsDirectory(this ZipArchiveEntry entry)
			=> entry.FullName.EndsWith('/') || entry.FullName.EndsWith('\\') || entry.Name == string.Empty;
	}
}
