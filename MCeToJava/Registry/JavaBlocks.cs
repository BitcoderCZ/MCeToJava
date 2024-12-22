using MCeToJava.NBT;
using Serilog;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace MCeToJava.Registry;

internal static class JavaBlocks
{
	private static readonly Dictionary<int, string> idToNameAndState = new();

	private static readonly Dictionary<int, string> bedrockToNameAndState = new();

	private static readonly Dictionary<string, string> nameToDefaultNameAndState = new();

	public static void Load(JsonArray vanillaRoot, JsonArray nonvanillaRoot)
	{
		foreach (var item in vanillaRoot)
		{
			JsonObject obj = item!.AsObject();
			int id = obj["id"]!.GetValue<int>();
			string nameAndSate = obj["name"]!.GetValue<string>();

			if (!idToNameAndState.TryAdd(id, nameAndSate))
			{
				Log.Warning($"[registry] Duplicate Java block ID {id}");
			}

			try
			{
				BedrockMapping? bedrockMapping = ReadBedrockMapping(obj["bedrock"]!.AsObject(), vanillaRoot);

				if (bedrockMapping is null)
				{
					continue;
				}

				bedrockToNameAndState.TryAdd(bedrockMapping.Id, nameAndSate);
				int bracketIndex = nameAndSate.IndexOf('[');
				nameToDefaultNameAndState.TryAdd(bracketIndex == -1 ? nameAndSate : nameAndSate.Substring(0, bracketIndex), nameAndSate);
			}
			catch (BedrockMappingFailException ex)
			{
				Log.Warning($"[registry] Cannot find Bedrock block for Java block {nameAndSate}: {ex}");
			}
		}

		foreach (var item in nonvanillaRoot)
		{
			JsonObject obj = item!.AsObject();

			string baseName = obj["name"]!.GetValue<string>();

			List<string> stateNames = [];
			JsonArray statesArray = obj["states"]!.AsArray();

			foreach (var stateElement in statesArray)
			{
				JsonObject stateObject = stateElement!.AsObject();

				string stateName = stateObject["name"]!.GetValue<string>();
				stateNames.Add(stateName);

				string name = baseName + stateName;

				try
				{
					BedrockMapping? bedrockMapping = ReadBedrockMapping(stateObject["bedrock"]!.AsObject(), null);

					if (bedrockMapping is null)
					{
						continue;
					}

					bedrockToNameAndState.TryAdd(bedrockMapping.Id, baseName);
				}
				catch (BedrockMappingFailException ex)
				{
					Log.Warning($"[registry] Cannot find Bedrock block for Java block {name}: {ex}");
				}
			}
		}
	}

	/// <exception cref="BedrockMappingFailException"></exception>
	private static BedrockMapping? ReadBedrockMapping(JsonObject bedrockMappingObject, JsonArray? javaBlocksArray)
	{
		if (bedrockMappingObject.ContainsKey("ignore") && bedrockMappingObject["ignore"]!.GetValue<bool>())
		{
			return null;
		}

		string name = bedrockMappingObject["name"]!.GetValue<string>();

		Dictionary<string, object> state = new();
		if (bedrockMappingObject.ContainsKey("state"))
		{
			JsonObject stateObject = bedrockMappingObject["state"]!.AsObject();
			foreach (var (key, stateElement) in stateObject)
			{
				switch (stateElement!.GetValueKind())
				{
					case JsonValueKind.String:
						state[key] = stateElement.GetValue<string>();
						break;
					case JsonValueKind.True:
						state[key] = 1;
						break;
					case JsonValueKind.False:
						state[key] = 0;
						break;
					default:
						state[key] = stateElement.GetValue<int>();
						break;
				}
			}
		}

		int id = BedrockBlocks.GetId(name, state);
		if (id == -1)
		{
			throw new BedrockMappingFailException("Cannot find Bedrock block with provided name and state");
		}

		bool waterlogged = bedrockMappingObject.ContainsKey("waterlogged") && bedrockMappingObject["waterlogged"]!.GetValue<bool>();

		BedrockMapping.BlockEntityBase? blockEntity = null;
		if (bedrockMappingObject.ContainsKey("block_entity"))
		{
			JsonObject blockEntityObject = bedrockMappingObject["block_entity"]!.AsObject();
			string type = blockEntityObject["type"]!.GetValue<string>();

			switch (type)
			{
				case "bed":
					{
						string color = blockEntityObject["color"]!.GetValue<string>();
						blockEntity = new BedrockMapping.BedBlockEntity(type, color);
					}
					break;
				case "flower_pot":
					{
						NbtMap? contents = null;

						if (blockEntityObject.ContainsKey("contents") && blockEntityObject["contents"]!.GetValueKind() != JsonValueKind.Null)
						{
							string contentsName = blockEntityObject["contents"]!.GetValue<string>();

							if (javaBlocksArray is not null)
							{
								var element = javaBlocksArray
										.Where(element => element!.AsObject()["name"]!.GetValue<string>() == contentsName)
										.Select(element => element!.AsObject()["bedrock"]!.AsObject())
										.Where(element => !element.ContainsKey("ignore") || !element["ignore"]!.GetValue<bool>())
										.FirstOrDefault();

								if (element is not null)
								{
									NbtMapBuilder builder = NbtMap.CreateBuilder();
									builder.PutString("name", element["name"]!.GetValue<string>());
									if (element.ContainsKey("state"))
									{
										NbtMapBuilder stateBuilder = NbtMap.CreateBuilder();
										foreach (var (key, stateElement) in element["state"]!.AsObject())
										{
											switch (stateElement!.GetValueKind())
											{
												case JsonValueKind.String:
													stateBuilder.PutString(key, stateElement.GetValue<string>());
													break;
												case JsonValueKind.True:
													stateBuilder.PutInt(key, 1);
													break;
												case JsonValueKind.False:
													stateBuilder.PutInt(key, 0);
													break;
												default:
													stateBuilder.PutInt(key, stateElement.GetValue<int>());
													break;
											}
										}

										builder.PutCompound("states", stateBuilder.Build());
									}

									contents = builder.Build();
								}
							}
							if (contents == null)
							{
								throw new BedrockMappingFailException("Could not find contents for flower pot");
							}
						}
						blockEntity = new BedrockMapping.FlowerPotBlockEntity(type, contents);
					}
					break;
				case "moving_block":
					{
						blockEntity = new BedrockMapping.BlockEntityBase(type);
					}
					break;
				case "piston":
					{
						bool sticky = blockEntityObject["sticky"]!.GetValue<bool>();
						bool extended = blockEntityObject["extended"]!.GetValue<bool>();
						blockEntity = new BedrockMapping.PistonBlockEntity(type, sticky, extended);
					}
					break;
			}
		}

		BedrockMapping.ExtraDataBase? extraData = null;
		if (bedrockMappingObject.ContainsKey("extra_data"))
		{
			JsonObject extraDataObject = bedrockMappingObject["extra_data"]!.AsObject();
			string type = extraDataObject["type"]!.GetValue<string>();
			switch (type)
			{
				case "note_block":
					{
						int pitch = extraDataObject["pitch"]!.GetValue<int>();
						extraData = new BedrockMapping.NoteBlockExtraData(pitch);
					}
					break;
			}
		}

		return new BedrockMapping(id, waterlogged, blockEntity, extraData);
	}

	// not needed
	public static string? GetName(int id)
	{
		if (idToNameAndState.TryGetValue(id, out string? name))
		{
			return name;
		}
		else
		{
			return null;
		}
	}

	public static string? GetNameAndState(int bedrockId)
	{
		if (bedrockToNameAndState.TryGetValue(bedrockId, out string? nameAndState))
		{
			return nameAndState;
		}
		else
		{
			// fallback
			var name = BedrockBlocks.GetName(bedrockId);
			if (!string.IsNullOrEmpty(name) && nameToDefaultNameAndState.TryGetValue(name, out nameAndState))
			{
				return nameAndState;
			}
		}

		return null;
	}

	internal sealed class BedrockMapping
	{
		/// <summary>
		/// Bedrock id of the block.
		/// </summary>
		public readonly int Id;
		public readonly bool Waterlogged;
		public readonly BlockEntityBase? BlockEntity;
		public readonly ExtraDataBase? ExtraData;

		public BedrockMapping(int id, bool waterlogged, BlockEntityBase? blockEntity, ExtraDataBase? extraData)
		{
			Id = id;
			Waterlogged = waterlogged;
			BlockEntity = blockEntity;
			ExtraData = extraData;
		}

		internal class BlockEntityBase
		{
			public readonly string Type;

			public BlockEntityBase(string type)
			{
				Type = type;
			}
		}

		internal sealed class BedBlockEntity : BlockEntityBase
		{
			public readonly string Color;

			public BedBlockEntity(string type, string color)
				: base(type)
			{
				Color = color;
			}
		}

		internal sealed class FlowerPotBlockEntity : BlockEntityBase
		{
			public NbtMap? Contents;

			public FlowerPotBlockEntity(string type, NbtMap? contents)
				: base(type)
			{
				Contents = contents;
			}
		}

		internal sealed class PistonBlockEntity : BlockEntityBase
		{
			public readonly bool Sticky;
			public readonly bool Extended;

			public PistonBlockEntity(string type, bool sticky, bool extended)
				: base(type)
			{
				Sticky = sticky;
				Extended = extended;
			}
		}

		internal abstract class ExtraDataBase
		{
			protected ExtraDataBase()
			{
			}
		}

		internal sealed class NoteBlockExtraData : ExtraDataBase
		{
			public readonly int Pitch;

			public NoteBlockExtraData(int pitch)
			{
				Pitch = pitch;
			}
		}
	}

	private sealed class BedrockMappingFailException : Exception
	{
		public BedrockMappingFailException(string message)
			: base(message)
		{
		}
	}
}