using Stride.Core.Mathematics;

namespace E03_2D_HUD;

/// <summary>
/// Small lights in the band between the frame and the panels. A run of them leaves each corner
/// along each of its two edges, and the corner itself has none. The runs have different lengths,
/// picked once from a seed, so the four corners do not match. The spacing is the same everywhere,
/// across a corner too.
/// </summary>
public sealed class FrameLights
{
    private const float Spacing = 0.4f;
    private const float Radius = 0.05f;
    private const int Shortest = 5;
    private const int Longest = 10;

    // How far from the corner a run starts. The first lights of a corner's two runs face each other
    // across the diagonal, so this puts them one spacing apart, like any other two neighbours.
    private static readonly float Start = Spacing / MathF.Sqrt(2f);

    private static readonly Vector2[] Corners = [new(1f, 1f), new(-1f, 1f), new(-1f, -1f), new(1f, -1f)];

    // For each corner, how many lights run along its horizontal edge and along its vertical edge
    private readonly (int Across, int Up)[] _runs;

    /// <summary>Constructs the lights.</summary>
    /// <param name="seed">The same seed gives the same runs.</param>
    public FrameLights(int seed)
    {
        var random = new Random(seed);

        _runs = [.. Corners.Select(_ => (random.Next(Shortest, Longest + 1), random.Next(Shortest, Longest + 1)))];
    }

    /// <summary>Draws the lights.</summary>
    /// <param name="canvas">The canvas to draw on.</param>
    /// <param name="frame">The frame.</param>
    /// <param name="inset">How far inside the frame the lights sit: half the padding puts them in the middle of the band.</param>
    public void Draw(HudCanvas canvas, HudRect frame, float inset)
    {
        for (var i = 0; i < Corners.Length; i++)
        {
            var side = Corners[i];
            var corner = frame.Center + side * (frame.Size / 2f - new Vector2(inset));

            for (var step = 0; step < _runs[i].Across; step++)
            {
                DrawLight(canvas, corner - new Vector2(side.X * (Start + step * Spacing), 0f), step);
            }

            for (var step = 0; step < _runs[i].Up; step++)
            {
                DrawLight(canvas, corner - new Vector2(0f, side.Y * (Start + step * Spacing)), step);
            }
        }
    }

    /// <summary>One light. It is dimmer the further it is from its corner, and it breathes slowly.</summary>
    private static void DrawLight(HudCanvas canvas, Vector2 position, int step)
    {
        var theme = canvas.Theme;
        var colour = theme.For(HudRole.Frame);

        // Each light breathes out of step with its neighbours: the phase comes from where it is
        var breath = 0.75f + 0.25f * MathF.Sin(canvas.Time * 1.3f + position.X * 2f + position.Y * 3f);
        var fade = 1f - 0.6f * step / Longest;

        canvas.Style(0f, 0.9f, colour, glow: 3f, glowColour: theme.Glow, opacity: breath * fade, additive: true);
        canvas.Shapes.DrawSolidCircle(position, Radius, colour);
    }
}