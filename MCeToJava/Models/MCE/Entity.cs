using MathUtils.Vectors;

namespace MCeToJava.Models.MCE;

internal record Entity(string Name, float3 Position, float2 Rotation, float3 ShadowPosition, float ShadowSize, int OverlayColor, int ChangeColor, int MultiplicitiveTintChangeColor, Dictionary<string, object>? ExtraData, string SkinData, bool IsPersonaSkin);
