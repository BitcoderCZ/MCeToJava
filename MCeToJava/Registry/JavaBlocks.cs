using MCeToJava.NBT;
using MCeToJava.Utils;
using Serilog;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MCeToJava.Registry
{
	internal static class JavaBlocks
	{
		private static readonly Dictionary<int, string> map = new();
		private static readonly Dictionary<string, List<string>> nonVanillaStatesList = new();

		private static readonly Dictionary<int, BedrockMapping> bedrockMap = new();
		private static readonly Dictionary<string, BedrockMapping> bedrockMapByName = new();
		private static readonly Dictionary<string, BedrockMapping> bedrockNonVanillaMap = new();

		public static void Load(JsonArray vanillaRoot, JsonArray nonvanillaRoot)
		{
			foreach (var item in vanillaRoot)
			{
				JsonObject obj = item!.AsObject();
				int id = obj["id"]!.GetValue<int>();
				string name = obj["name"]!.GetValue<string>();

				if (!map.TryAdd(id, name))
				{
					Log.Warning($"Duplicate Java block ID {id}");
				}

				try
				{
					BedrockMapping? bedrockMapping = readBedrockMapping(obj["bedrock"]!.AsObject(), vanillaRoot);

					if (bedrockMapping is null)
					{
						continue;
					}

					bedrockMap[id] = bedrockMapping;
					bedrockMapByName[name] = bedrockMapping;
				}
				catch (BedrockMappingFailException ex)
				{
					Log.Warning($"Cannot find Bedrock block for Java block {name}: {ex}");
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

					String name = baseName + stateName;

					try
					{
						BedrockMapping? bedrockMapping = readBedrockMapping(stateObject["bedrock"]!.AsObject(), null);

						if (bedrockMapping is null)
						{
							continue;
						}

						bedrockNonVanillaMap[name] = bedrockMapping;
					}
					catch (BedrockMappingFailException ex)
					{
						Log.Warning($"Cannot find Bedrock block for Java block {name}: {ex}");
					}
				}

				if (!nonVanillaStatesList.TryAdd(baseName, stateNames))
				{
					Log.Warning($"Duplicate Java non-vanilla block name {baseName}");
				}
			}
		}

		/// <exception cref="BedrockMappingFailException"></exception>
		private static BedrockMapping? readBedrockMapping(JsonObject bedrockMappingObject, JsonArray? javaBlocksArray)
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

			int id = BedrockBlocks.getId(name, state);
			if (id == -1)
			{
				throw new BedrockMappingFailException("Cannot find Bedrock block with provided name and state");
			}

			bool waterlogged = bedrockMappingObject.ContainsKey("waterlogged") ? bedrockMappingObject["waterlogged"]!.GetValue<bool>() : false;

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
										NbtMapBuilder builder = NbtMap.builder();
										builder.putString("name", element["name"]!.GetValue<string>());
										if (element.ContainsKey("state"))
										{
											NbtMapBuilder stateBuilder = NbtMap.builder();
											foreach (var (key, stateElement) in element["state"]!.AsObject())
											{
												switch (stateElement!.GetValueKind())
												{
													case JsonValueKind.String:
														stateBuilder.putString(key, stateElement.GetValue<string>());
														break;
													case JsonValueKind.True:
														stateBuilder.putInt(key, 1);
														break;
													case JsonValueKind.False:
														stateBuilder.putInt(key, 0);
														break;
													default:
														stateBuilder.putInt(key, stateElement.GetValue<int>());
														break;
												}
											}

											builder.putCompound("states", stateBuilder.build());
										}

										contents = builder.build();
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

		public static int getMaxVanillaBlockId()
		{
			return map.Count > 0 ? map.Keys.Max() : -1;
		}

		public static List<string>? getStatesForNonVanillaBlock(string name)
		{
			if (nonVanillaStatesList.TryGetValue(name, out var states))
			{
				return states;
			}
			else
			{
				return null;
			}
		}

		/*[Obsolete]
		public static string? getName(int id)
		{
			return getName(id, null);
		}

		[Obsolete]
		public static BedrockMapping? getBedrockMapping(int javaId)
		{
			return getBedrockMapping(javaId, null);
		}*/

		public static string? getName(int id/*, FabricRegistryManager? fabricRegistryManager*/)
		{
			if (map.TryGetValue(id, out string? name))
			{
				return name;
			}
			/*else if (fabricRegistryManager != null)
			{
				return fabricRegistryManager.GetBlockName(id);
			}*/
			else
			{
				return null;
			}
		}

		public static BedrockMapping? getBedrockMapping(int javaId/*, FabricRegistryManager? fabricRegistryManager*/)
		{
			if (bedrockMap.TryGetValue(javaId, out var bedrockMapping))
			{
				return bedrockMapping;
			}
			/*else if (fabricRegistryManager != null)
			{
				string fabricName = fabricRegistryManager.GetBlockName(javaId);
				if (fabricName != null)
				{
					bedrockMapping = bedrockNonVanillaMap.GetOrDefault(fabricName, null);
				}
			}*/
			else
			{
				return null;
			}
		}

		public static BedrockMapping? getBedrockMapping(string javaName)
		{
			if (bedrockMapByName.TryGetValue(javaName, out var bedrockMapping) || bedrockNonVanillaMap.TryGetValue(javaName, out bedrockMapping))
			{
				return bedrockMapping;
			}
			else
			{
				return null;
			}
		}

		internal sealed class BedrockMapping
		{
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
}