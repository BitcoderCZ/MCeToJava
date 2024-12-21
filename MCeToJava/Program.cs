using CommandLineParser;
using CommandLineParser.Attributes;
using CommandLineParser.Commands;
using MCeToJava.Exceptions;
using MCeToJava.Models;
using MCeToJava.Models.MCE;
using MCeToJava.Registry;
using MCeToJava.Utils;
using Serilog;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace MCeToJava;

internal class Program
{
	private static bool registryInitialized = false;

	[CommandName("convert")]
	[HelpText("Converts a Project Earth (Minecraft Earth) buildplate to a Java world.")]
	private sealed class ConvertCommand : ConsoleCommand
	{
		[Required]
		[Argument("in-path")]
		[HelpText("Path to the buildplate json.")]
		public string? InPath { get; set; }

		[Argument("out-path")]
		[HelpText("Path to converted java world zip.")]
		public string OutPath { get; set; } = "converted_world.zip";

		[Option("biome")]
		public string Biome { get; set; } = "minecraft:plains";

		[Option('t', "target")]
		[HelpText("The target to export to, additional files are generated depending on this setting.")]
		public ExportTarget ExportTarget { get; set; } = ExportTarget.Vienna;

		[Option("night")]
		[HelpText("If the world's time be at night.")]
		public bool Night { get; set; } = false;

		[Option("world-name")]
		[HelpText("Name of the exported world.")]
		[DependsOn(nameof(ExportTarget), ExportTarget.Java)]
		public string WorldName { get; set; } = "Buildplate";

		public override int Run()
		{
			if (string.IsNullOrEmpty(InPath))
			{
				Console.WriteLine("Invalid in-path");
				return ErrorCode.CliParseError;
			}

			Log.Information($"Converting '{Path.GetFullPath(InPath)}'");

			string buildplateText;
			try
			{
				buildplateText = File.ReadAllText(InPath);
			}
			catch (FileNotFoundException fileNotFound)
			{
				Console.WriteLine($"File '{fileNotFound.FileName}' wasn't found.");
				return ErrorCode.FileNotFound;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Failed to read input file: {ex}");
				return ErrorCode.UnknownError;
			}

			Buildplate? buildplate;
			try
			{
				buildplate = U.DeserializeJson<Buildplate>(buildplateText);

				if (buildplate is null)
				{
					throw new ConvertException("Invalid json - null.");
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Failed to parse input file: {ex}");
				return ErrorCode.UnknownError;
			}

			try
			{
				InitRegistry();
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Failed to initialize block registry: {ex}");
				return ErrorCode.UnknownError;
			}

			WorldData data;
			try
			{
				data = Converter.Convert(buildplate, ExportTarget, Biome, Night, WorldName);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Failed to convert buildplate: {ex}");
				return ErrorCode.UnknownError;
			}

			Log.Information("Writing output zip");

			try
			{
				using (FileStream fs = new FileStream(OutPath, FileMode.Create, FileAccess.Write, FileShare.Read))
				{
					data.WriteToStream(fs);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Failed to write output file: {ex}");
				return ErrorCode.UnknownError;
			}

			Log.Information("Done");

			return ErrorCode.Success;
		}
	}

	static int Main(string[] args)
	{
#if DEBUG
		Debugger.Launch();
#endif
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

		return CommandParser.ParseAndRun(args, new ParseOptions(), typeof(ConvertCommand), []);
	}

	private static void InitRegistry()
	{
		if (registryInitialized)
		{
			return;
		}

		Log.Information("Initializing registry");
		BedrockBlocks.Load(JsonSerializer.Deserialize<JsonArray>(ReadFile("blocks_bedrock.json"))!);

		JavaBlocks.Load(
			JsonSerializer.Deserialize<JsonArray>(ReadFile("blocks_java.json"))!,
			JsonSerializer.Deserialize<JsonArray>(ReadFile("blocks_java_nonvanilla.json"))!
		);

		registryInitialized = true;

		string ReadFile(string fileName)
		{
			return File.ReadAllText(Path.Combine("Data", fileName));
		}
	}
}
