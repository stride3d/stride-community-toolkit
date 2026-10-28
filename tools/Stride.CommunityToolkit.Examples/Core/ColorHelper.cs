using System.Drawing;

namespace Stride.CommunityToolkit.Examples.Core;

public static class ColorHelper
{
    private const int MaxChannelValue = 255;

    /// <summary>Moves every channel by the given share of the full range: positive lightens, negative darkens.</summary>
    public static Color Lighten(Color color, float by)
    {
        by = Math.Clamp(by, -1f, 1f);

        return Color.FromArgb(Channel(color.R, by), Channel(color.G, by), Channel(color.B, by));
    }

    private static int Channel(byte value, float by) => (int)Math.Clamp(value + MaxChannelValue * by, 0, MaxChannelValue);
}