using MathUtils.Vectors;

namespace MCeToJava.Models.MCE
{
	internal record Entity(string Name, float3 position, float2 rotation, float3 shadowPosition, float shadowSize, int overlayColor, int changeColor, int multiplicitiveTintChangeColor, Dictionary<string, object>? extraData, string skinData, bool isPersonaSkin);
}
