using MathUtils.Vectors;

namespace MCeToJava.Models.MCE;

internal record BlockEntity(int Type, int3 Position, JsonNbtConverter.JsonNbtTag Data);
