// <copyright file="ConvertDirCommand.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using CommandLineParser.Attributes;
using CommandLineParser.Commands;
using FluentResults;
using MCeToJava.Models;
using Serilog;
using Serilog.Core;
using Spectre.Console;
using System.Collections.Concurrent;

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

			try
			{
				Converter.InitRegistry(Log.Logger);
			}
			catch (Exception ex)
			{
				Log.Error($"Failed to initialize block registry: {ex}");
				return ErrorCode.UnknownError;
			}

			var options = new Converter.Options(Logger.None, ExportTarget, Biome, WorldName);

			ConcurrentBag<(string Path, Result Result)> failedFiles = [];

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

					await Task.WhenAll(files.Select(async (path, index) =>
					{
						await semaphore.WaitAsync().ConfigureAwait(false);

						string fileName = Path.GetFileName(path);
						var task = ctx.AddTaskBefore(fileName, filesTask, autoStart: false, maxValue: Converter.NumbProgressStages);

						Result result = await Converter.ConvertFile(path, Path.Combine(OutDir, fileName + ".zip"), task, options).ConfigureAwait(false);

						if (result.IsFailed)
						{
							task.Value = task.MaxValue;
							failedFiles.Add((path, result));
						}

						task.StopTask();
						filesTask.Increment(1);

						semaphore.Release();
					})).ConfigureAwait(false);
				}).Wait();

			if (failedFiles.Count > 0)
			{
				PrintFailedFiles(failedFiles);
				return ErrorCode.UnknownError;
			}

			return ErrorCode.Success;
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

	private static void PrintFailedFiles(ConcurrentBag<(string Path, Result Result)> failedFiles)
	{
		Console.WriteLine($"Failed to convert {failedFiles.Count} buildplate{(failedFiles.Count == 1 ? string.Empty : "s")}:");
		Console.WriteLine();

		foreach (var (path, result) in failedFiles)
		{
			Console.WriteLine($"{Path.GetFileName(path)} - {string.Join("; ", result.Errors.Select(static err =>
			{
				return err.Reasons.Count == 0
				? err.Message
				: err.Message + ": " + string.Join(", ", err.Reasons.Select(err => err.Message));
			}))}");
		}
	}
}