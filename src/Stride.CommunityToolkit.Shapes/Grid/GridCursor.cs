using Stride.CommunityToolkit.Rendering.Text;
using Stride.Core.Mathematics;
using System.Globalization;

namespace Stride.CommunityToolkit.Shapes;

/// <summary>
/// The mouse position in both spaces, written beside the mouse: the pixel it is on, and the world
/// point under it on the grid's plane.
/// </summary>
internal static class GridCursor
{
    internal static void Draw(GridCanvas canvas, in GridView view, Vector2 mouse)
    {
        if (mouse.X < 0f || mouse.X > 1f || mouse.Y < 0f || mouse.Y > 1f) return;

        var pixel = mouse * view.Size;
        var culture = CultureInfo.InvariantCulture;
        var text = string.Create(culture, $"screen {pixel.X:0}, {pixel.Y:0}");

        if (canvas.IsGround(view))
        {
            if (view.OnPlaneY(mouse, out var ground)) text += string.Create(culture, $"\nworld {ground.X:0.00}, 0, {ground.Y:0.00}");
        }
        else if (view.OnPlaneZ(mouse, out var flat))
        {
            text += string.Create(culture, $"\nworld {flat.X:0.00}, {flat.Y:0.00}");
        }

        // Below and to the right of the mouse, or on the other side near the window's far edges
        var right = pixel.X < view.Size.X - 170f;
        var below = pixel.Y < view.Size.Y - 60f;
        var anchor = below
            ? (right ? TextAnchor.TopLeft : TextAnchor.TopRight)
            : (right ? TextAnchor.BottomLeft : TextAnchor.BottomRight);

        var label = canvas.Label(text, pixel + new Vector2(right ? 16f : -10f, below ? 18f : -8f), anchor, Color.White);

        label.EnableBackground = true;
    }
}