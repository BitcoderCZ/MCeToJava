using MathUtils.Vectors;
using MCeToJava.NBT;

namespace MCeToJava.Models.MCE;

internal record BlockEntity(int Type, int3 Position, JsonNbtConverter.JsonNbtTag Data);
