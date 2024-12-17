using MCeToJava.Exceptions;
using MCeToJava.Models.MCE;
using MCeToJava.Utils;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCeToJava
{
	internal sealed class Converter
	{
		private const int CHUNK_RADIUS = 2;

		public static void Convert(Buildplate buildplate)
		{
			BuildplateModel? model = U.DeserializeJson<BuildplateModel>(System.Convert.FromBase64String(buildplate.Model));

			if (model is null)
			{
				throw new ConvertException("Invalid json - buildplate is null.");
			}

			if (model.FormatVersion != 1)
			{
				throw new ConvertException($"Unsupported version '{model.FormatVersion}', only version 1 is supported.");
			}

			WorldData zip = new WorldData();
			// produce a zip file
		}
	}
}
