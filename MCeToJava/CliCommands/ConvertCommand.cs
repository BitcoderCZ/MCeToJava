// <copyright file="ConvertCommand.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using CommandLineParser;
using CommandLineParser.Attributes;
using CommandLineParser.Commands;
using FluentResults;
using MCeToJava.Exceptions;
using MCeToJava.Models;
using Serilog;

namespace MCeToJava.CliCommands;

[CommandName("convert")]
[HelpText("Converts a Project Earth (Minecraft Earth) buildplate to a Java world.")]
internal sealed class ConvertCommand : ConsoleCommand
{
	[Required]
	[Argument("in-path")]
	[HelpText("Path to the buildplate json.")]
	public string? InPath { get; set; }

	[Argument("out-path")]
	[HelpText("Path of the converted java world zip.")]
	public string OutPath { get; set; } = "converted_world.zip";

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
			Result result = Converter.ConvertFile(InPath, OutPath, null, new Converter.Options(Log.Logger, ExportTarget, Biome, WorldName)).ConfigureAwait(false).GetAwaiter().GetResult();

			return result.IsSuccess
				? ErrorCode.Success
				: result.HasError<ErrorCodeError>(out var errors)
				? errors.First().ErrorCode
				: ErrorCode.UnknownError;
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