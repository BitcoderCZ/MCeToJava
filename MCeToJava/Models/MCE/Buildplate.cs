using MathUtils.Vectors;
using System.Text.Json.Serialization;

namespace MCeToJava.Models.MCE;

internal record Buildplate(Guid Id, string ETag, DateTime LastUpdated, bool IsModified, bool Locked, long NumberOfBlocks, long RequiredLevel, Guid TemplateId, Buildplate.Gamemode Type, string Model, int3 Offset, Buildplate.Flat2 Dimension, double BlocksPerMeter, Buildplate.Orientation SurfaceOrientation, int Order)
{
	[JsonConverter(typeof(JsonStringEnumConverter))]
	public enum Gamemode
	{
		Survival,
	}

	[JsonConverter(typeof(JsonStringEnumConverter))]
	public enum Orientation
	{
		Horizontal,
		Vertical,
	}

	public record struct Flat2(int X, int Z);
}
