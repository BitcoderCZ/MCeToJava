// <copyright file="BedrockBlocks.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using MCeToJava.NBT;
using MCeToJava.Utils;
using Serilog;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace MCeToJava.Registry;

internal static class BedrockBlocks
{
	private static readonly Dictionary<BlockNameAndState, int> StateToIdMap = [];
	private static readonly Dictionary<int, BlockNameAndState> IdToStateMap = [];
	private static readonly Dictionary<string, int> NameToId = [];

	public static int AirId { get; private set; }

	public static int WaterId { get; private set; }

	public static void Load(JsonArray root)
	{
		foreach (var element in root)
		{
			JsonObject obj = element!.AsObject();

			int id = obj["id"]!.GetValue<int>();
			string name = obj["name"]!.GetValue<string>();
			Dictionary<string, object> state = [];
			JsonObject stateObject = obj["state"]!.AsObject();

			foreach (var item in stateObject)
			{
				JsonNode stateElement = item.Value!;
				state[item.Key] = stateElement.GetValueKind() == JsonValueKind.String
					? stateElement.GetValue<string>()
					: stateElement.GetValue<int>();
			}

			BlockNameAndState blockNameAndState = new BlockNameAndState(name, state);
			if (!StateToIdMap.TryAdd(blockNameAndState, id))
			{
				Log.Warning($"[registry] Duplicate Bedrock block name/state {name}");
			}

			NameToId.TryAdd(name, id);

			if (!IdToStateMap.TryAdd(id, blockNameAndState))
			{
				Log.Warning($"[registry] Duplicate Bedrock block ID {id}");
			}
		}

		AirId = GetId("minecraft:air", []);
		Dictionary<string, object> hashMap = new()
		{
			["liquid_depth"] = 0,
		};
		WaterId = GetId("minecraft:water", hashMap);
	}

	public static int GetId(string name)
		=> name == "fountain:solid_air"
			? Chunk.SolidAirId
			: NameToId.GetOrDefault(name, -1);

	public static int GetId(string name, Dictionary<string, object> state)
	{
		BlockNameAndState blockNameAndState = new BlockNameAndState(name, state);
		return StateToIdMap.GetOrDefault(blockNameAndState, -1);
	}

	public static string? GetName(int id)
		=> IdToStateMap.TryGetValue(id, out var blockNameAndState)
		? blockNameAndState.Name
		: null;

	// not needed
	public static Dictionary<string, object>? GetState(int id)
	{
		if (IdToStateMap.TryGetValue(id, out var blockNameAndState))
		{
			Dictionary<string, object> state = [];
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

	// not needed
	public static NbtMap? GetStateNbt(int id)
	{
		if (!IdToStateMap.TryGetValue(id, out var blockNameAndState))
		{
			return null;
		}

		NbtMapBuilder builder = NbtMap.CreateBuilder();
		foreach (var (key, value) in blockNameAndState.State)
		{
			switch (value)
			{
				case string str:
					builder.PutString(key, str);
					break;
				case int i:
					builder.PutInt(key, i);
					break;
				default:
					Debug.Fail("Invalid type.");
					break;
			}
		}

		return builder.Build();
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
			=> obj is BlockNameAndState other && Name == other.Name && State.SequenceEqual(other.State);

		public override int GetHashCode()
		{
			// TODO: use HashCode?

			// Overflow is fine, just wrap
			unchecked
			{
				int hash = 17 * Name.GetHashCode();
				foreach (var kvp in State)
				{
					hash = (hash * 23) + kvp.Key.GetHashCode();
					hash = (hash * 23) + (kvp.Value?.GetHashCode() ?? 0);
				}

				return hash;
			}
		}
	}
}
