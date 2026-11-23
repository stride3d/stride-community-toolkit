using Box2D.NET;
using Stride.Core.Mathematics;

namespace Stride.CommunityToolkit.Box2D;

/// <summary>Box2D's hex colours as Stride colours.</summary>
internal static class DebugDrawColors
{
    /// <summary>A <c>0xRRGGBB</c> value, opaque.</summary>
    internal static Color ToColor(B2HexColor color)
    {
        var value = (int)color;

        return new Color((byte)(value >> 16), (byte)(value >> 8), (byte)value);
    }
}