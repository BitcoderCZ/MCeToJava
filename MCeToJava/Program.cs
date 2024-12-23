using CommandLineParser;
using MCeToJava.CliCommands;
using Serilog;
using System.Diagnostics;

namespace MCeToJava;

internal class Program
{
	public static ILogger FileOnlyLogger = new LoggerConfiguration()
			.WriteTo.File("logs/debug.txt", rollingInterval: RollingInterval.Day, rollOnFileSizeLimit: true, fileSizeLimitBytes: 8338607, outputTemplate: "{Timestamp:HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
			.MinimumLevel.Debug()
			.CreateLogger();

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

		return CommandParser.ParseAndRun(args, new ParseOptions(), null, [typeof(ConvertCommand), typeof(ConvertDirCommand)]);
	}
}
