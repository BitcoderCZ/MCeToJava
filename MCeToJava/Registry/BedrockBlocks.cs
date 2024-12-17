﻿using MCeToJava.NBT;
using MCeToJava.Utils;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MCeToJava.Registry
{
	internal static class BedrockBlocks
	{
		private static readonly Dictionary<BlockNameAndState, int> stateToIdMap = new();
		private static readonly Dictionary<int, BlockNameAndState> idToStateMap = new();

		public static int AIR { get; private set; }
		public static int WATER { get; private set; }

		public static void Load(JsonArray root)
		{
			foreach (var element in root)
			{
				JsonObject obj = element!.AsObject();

				int id = obj["id"]!.GetValue<int>();
				string name = obj["name"]!.GetValue<string>();
				Dictionary<string, object> state = new();
				JsonObject stateObject = obj["state"]!.AsObject();

				foreach (var item in stateObject)
				{
					JsonNode stateElement = item.Value!;
					if (stateElement.GetValueKind() == JsonValueKind.String)
					{
						state[item.Key] = stateElement.GetValue<string>();
					}
					else
					{
						state[item.Key] = stateElement.GetValue<int>();
					}
				}
				BlockNameAndState blockNameAndState = new BlockNameAndState(name, state);
				if (!stateToIdMap.TryAdd(blockNameAndState, id))
				{
					Log.Warning($"Duplicate Bedrock block name/state {name}");
				}

				if (!idToStateMap.TryAdd(id, blockNameAndState))
				{
					Log.Warning($"Duplicate Bedrock block ID {id}");
				}
			}

			AIR = getId("minecraft:air", new());
			Dictionary<string, object> hashMap = new();
			hashMap["liquid_depth"] = 0;
			WATER = getId("minecraft:water", hashMap);
		}

		public static int getId(string name, Dictionary<string, object> state)
		{
			BlockNameAndState blockNameAndState = new BlockNameAndState(name, state);
			return stateToIdMap.GetOrDefault(blockNameAndState, -1);
		}

		public static string? GetName(int id)
		{
			if (idToStateMap.TryGetValue(id, out var blockNameAndState))
			{
				return blockNameAndState.Name;
			}
			else
			{
				return null;
			}
		}

		public static Dictionary<string, object>? GetState(int id)
		{
			if (idToStateMap.TryGetValue(id, out var blockNameAndState))
			{
				Dictionary<string, object> state = new();
				foreach (var item in blockNameAndState.State)
				{
					state[item.Key] = item.Value;
				}

				return state;
			}
			else
			{
				return null;
			}
		}

		public static NbtMap? GetStateNbt(int id)
		{
			if (!idToStateMap.TryGetValue(id, out var blockNameAndState))
			{
				return null;
			}

			NbtMapBuilder builder = NbtMap.builder();
			foreach (var (key, value) in blockNameAndState.State)
			{
				switch (value)
				{
					case string str:
						builder.putString(key, str);
						break;
					case int i:
						builder.putInt(key, i);
						break;
					default:
						Debug.Fail("Invalid type.");
						break;
				}
			}

			return builder.build();
		}

		[DebuggerDisplay("{DebuggerDisplay}")]
		private class BlockNameAndState
		{
			public readonly string Name;
			public readonly Dictionary<string, object> State;

			public BlockNameAndState(string name, Dictionary<string, object> state)
			{
				Name = name;
				State = state;
			}

			private string DebuggerDisplay => Name;

			public override bool Equals(object? obj)
			{
				return obj is BlockNameAndState other && Name == other.Name && State.SequenceEqual(other.State);
			}

			public override int GetHashCode()
			{
				unchecked // Overflow is fine, just wrap
				{
					int hash = 17 * Name.GetHashCode();
					foreach (var kvp in State)
					{
						hash = hash * 23 + kvp.Key.GetHashCode();
						hash = hash * 23 + (kvp.Value?.GetHashCode() ?? 0);
					}
					return hash;
				}
			}
		}
	}
}
