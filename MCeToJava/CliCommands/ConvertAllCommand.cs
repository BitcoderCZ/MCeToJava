// <copyright file="ConvertAllCommand.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using CommandLineParser.Attributes;
using CommandLineParser.Commands;
using MCeToJava.Models;
using Serilog;
using Serilog.Core;

namespace MCeToJava.CliCommands;

[CommandName("convert-all")]
[HelpText("Converts multiple Project Earth (Minecraft Earth) buildplates to a Java worlds.")]
internal sealed class ConvertAllCommand : ConsoleCommand
{
	[Required]
	[Argument("files")]
	[HelpText("Paths to the buildplate jsons.")]
	public string[] Files { get; set; } = [];

	[Argument("out-dir")]
	[HelpText("Path of the directory that the converted worlds will be placed into.")]
	public string OutDir { get; set; } = "converted";

	[Option("biome")]
	public string Biome { get; set; } = "minecraft:plains";

	[Option('t', "target")]
	[HelpText("The target to export to, additional files are generated depending on this setting.")]
	public ConvertTarget ConvertTarget { get; set; } = ConvertTarget.Vienna;

	[Option("world-name")]
	[HelpText("Name of the exported world.")]
	[DependsOn(nameof(ConvertTarget), ConvertTarget.Java)]
	public string WorldName { get; set; } = "Buildplate";

	public override int Run()
	{
		try
		{
			if (Files.Length == 0)
			{
				Log.Information("No files specified.");
				return ErrorCode.Success;
			}

			var options = new Converter.Options(Logger.None, ConvertTarget, Biome, WorldName);

			return Converter.ConvertFiles(Files, OutDir, options).ConfigureAwait(false).GetAwaiter().GetResult();
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