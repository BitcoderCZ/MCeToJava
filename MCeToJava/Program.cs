using MCeToJava.Exceptions;
using MCeToJava.Models.MCE;
using MCeToJava.Utils;
using Serilog.Events;
using Serilog;
using System.Diagnostics;
using System.Text.Json;
using MCeToJava.Registry;
using System.Text.Json.Nodes;
using SharpNBT;

namespace MCeToJava
{
	internal class Program
	{
		static void Main(string[] args)
		{
			// TODO: add cli option to change the biome
			// TODO: add cli option to export to normal world - generate level.dat - void generator or for fountain - generate buildplate_metadata.json
			// TODO: optimize Chunk by not adding data when there is only 1 palette entry

			Log.Logger = new LoggerConfiguration()
				.WriteTo.Console()
				.WriteTo.File("logs/debug.txt", rollingInterval: RollingInterval.Day, rollOnFileSizeLimit: true, fileSizeLimitBytes: 8338607, outputTemplate: "{Timestamp:HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
				.MinimumLevel.Debug()
				.CreateLogger();

#if RELEASE
			AppDomain.CurrentDomain.UnhandledException += (object sender, UnhandledExceptionEventArgs e) =>
			{
				Log.Fatal($"Unhandeled exception: {e.ExceptionObject}");
				Log.CloseAndFlush();
				Environment.Exit(1);
			};
#endif

			WorldData world;
			using (FileStream fs = new FileStream("java_input", FileMode.Open, FileAccess.Read, FileShare.Read))
				world = new WorldData(fs);

			var a = world.GetChunkNBT(-1, 0);

			var s = (ListTag)a["sections"];

			var bs = s.SelectMany(tag =>
			{
				if (tag is CompoundTag ct && ct.ContainsKey("block_states"))
				{
					return (ListTag)((CompoundTag)ct["block_states"])["palette"];
				}

				return [];
			});

			var s1 = s[0];
			var s2 = s[^2];

			InitRegistry();

			Buildplate? buildplate = U.DeserializeJson<Buildplate>(File.ReadAllText("input"));

			if (buildplate is null)
			{
				throw new ConvertException("Invalid json - buildplate is null.");
			}

			WorldData data = Converter.Convert(buildplate);
			using (FileStream fs = new FileStream("out.zip", FileMode.Create, FileAccess.Write, FileShare.Read))
			{
				data.WriteToStream(fs);
			}
			
			using (FileStream fs = new FileStream("out.zip", FileMode.Open, FileAccess.Read, FileShare.Read))
				data = new WorldData(fs);
			
			var a2 = data.GetChunkNBT(0, 0);

			Console.WriteLine("Done");
			Console.ReadKey();
		}

		private static void InitRegistry()
		{
			BedrockBlocks.Load(JsonSerializer.Deserialize<JsonArray>(File.ReadAllText(GetPath("blocks_bedrock.json")))!);

			JavaBlocks.Load(
				JsonSerializer.Deserialize<JsonArray>(File.ReadAllText(GetPath("blocks_java.json")))!,
				JsonSerializer.Deserialize<JsonArray>(File.ReadAllText(GetPath("blocks_java_nonvanilla.json")))!
			);

			string GetPath(string fileName)
			{
				return Path.Combine("Data", fileName);
			}
		}
	}
}
