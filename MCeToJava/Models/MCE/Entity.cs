﻿// <copyright file="Entity.cs" company="BitcoderCZ">
// Copyright (c) BitcoderCZ. All rights reserved.
// </copyright>

using MathUtils.Vectors;
using System.Text.Json.Nodes;

namespace MCeToJava.Models.MCE;

internal record Entity(string Name, double3 Position, float2 Rotation, float3 ShadowPosition, float ShadowSize, int OverlayColor, int ChangeColor, int MultiplicitiveTintChangeColor, Dictionary<string, JsonNode>? ExtraData, string SkinData, bool IsPersonaSkin);
