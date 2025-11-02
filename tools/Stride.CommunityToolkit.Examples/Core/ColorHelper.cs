using System.Drawing;

namespace Stride.CommunityToolkit.Examples.Core;

public static class ColorHelper
{
    private const int MaxChannelValue = 255;

    public static Color Lighten(Color color, float by)
    {
        by = Math.Clamp(by, -1f, 1f);

        int r = (int)Math.Clamp(color.R + MaxChannelValue * by, 0, MaxChannelValue);
        int g = (int)Math.Clamp(color.G + MaxChannelValue * by, 0, MaxChannelValue);
        int b = (int)Math.Clamp(color.B + MaxChannelValue * by, 0, MaxChannelValue);

        return Color.FromArgb(r, g, b);
    }
}