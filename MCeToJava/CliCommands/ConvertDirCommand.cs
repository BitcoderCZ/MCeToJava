using CommandLineParser.Attributes;
using CommandLineParser.Commands;
using MCeToJava.Models;
using Serilog;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCeToJava.CliCommands;

[CommandName("convert-dir")]
[HelpText("Converts all Project Earth (Minecraft Earth) buildplates in a directory to a Java worlds.")]
internal sealed class ConvertDirCommand : ConsoleCommand
{
	[Required]
	[Argument("in-dir")]
	[HelpText("Path to the directory containing buildplate jsons, no files besides buildplates should be in this directory.")]
	public string InDir { get; set; } = string.Empty;

	[Argument("out-dir")]
	[HelpText("Path of the directory that the converted worlds will be placed into.")]
	public string OutDir { get; set; } = "converted";

	[Option("biome")]
	public string Biome { get; set; } = "minecraft:plains";

	[Option('t', "target")]
	[HelpText("The target to export to, additional files are generated depending on this setting.")]
	public ExportTarget ExportTarget { get; set; } = ExportTarget.Vienna;

	[Option("night")]
	[HelpText("If the world's time should be day or night.")]
	public bool Night { get; set; } = false;

	[Option("world-name")]
	[HelpText("Name of the exported world.")]
	[DependsOn(nameof(ExportTarget), ExportTarget.Java)]
	public string WorldName { get; set; } = "Buildplate";

	public override int Run()
	{
		try
		{
			string[] files;
			try
			{
				files = Directory.GetFiles(InDir);
			}
			catch (Exception ex)
			{
				Log.Error($"Failed to get files: {ex}");
				return ErrorCode.UnknownError;
			}

			if (files.Length == 0)
			{
				Log.Information("No files in directory");
				return ErrorCode.Success;
			}

			if (!Directory.Exists(OutDir))
			{
				try
				{
					Directory.CreateDirectory(OutDir);
				}
				catch (Exception ex)
				{
					Log.Error($"Failed to create out-dir: {ex}");
					return ErrorCode.UnknownError;
				}
			}

			Converter.InitRegistry(Log.Logger);

			var options = new Converter.Options(Program.FileOnlyLogger, ExportTarget, Biome, Night, WorldName);

			int resErrCode = ErrorCode.Success;

			SemaphoreSlim semaphore = new SemaphoreSlim(Math.Max(Environment.ProcessorCount - 1, 1));
			
			AnsiConsole.Progress()
				.Columns(
					new TaskDescriptionColumn(),
					new ProgressBarColumn(),
					new PercentageColumn(),
					new RemainingTimeColumn(),
					new SpinnerColumn())
				.StartAsync(async ctx =>
				{
					var filesTask = ctx.AddTask("Convert buildplates", maxValue: files.Length);

					await Task.WhenAll(files.Select(async (file, index) =>
					{
						await semaphore.WaitAsync().ConfigureAwait(false);

						string fileName = Path.GetFileName(file);
						var task = ctx.AddTaskBefore(fileName, filesTask, autoStart: false, maxValue: Converter.NumbProgressStages);

						int errCode = await Converter.ConvertFile(file, Path.Combine(OutDir, fileName + ".zip"), task, options).ConfigureAwait(false);

						if (errCode != ErrorCode.Success)
						{
							task.Value = task.MaxValue;
							Interlocked.CompareExchange(ref resErrCode, ErrorCode.Success, errCode);
						}

						task.StopTask();
						filesTask.Increment(1);

						semaphore.Release();
					})).ConfigureAwait(false);
				}).Wait();

			return resErrCode;
		}
		catch (Exception ex)
		{
			Log.Error($"Unhandled exception: {ex}");
			return ErrorCode.UnknownError;
		}
		finally
		{
			Log.CloseAndFlush();
		}
	}
}