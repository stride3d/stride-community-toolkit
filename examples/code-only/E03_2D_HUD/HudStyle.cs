using Stride.Core.Mathematics;

namespace E03_2D_HUD;

/// <summary>
/// How the next shapes are drawn: the whole of the batch's captured state in one value. What is
/// left out is off, so no draw inherits a stale glow or dash from the one before.
/// </summary>
/// <param name="Border">The outline's width, in pixels. 0 for none.</param>
/// <param name="FillAlpha">The fill's opacity. 0 for an outline alone.</param>
/// <param name="Fill">The fill's colour, or <c>null</c> for the colour the shape is drawn with.</param>
public readonly record struct HudStyle(float Border, float FillAlpha, Color? Fill = null)
{
    /// <summary>The glow's width, in pixels. 0 for none.</summary>
    public float Glow { get; init; }

    /// <summary>The glow's colour, or <c>null</c> for the colour the shape is drawn with.</summary>
    public Color? GlowColour { get; init; }

    /// <summary>Whether the glow adds light instead of blending over what is behind it.</summary>
    public bool Additive { get; init; }

    /// <summary>The length of a dash, in pixels. 0 for a solid outline.</summary>
    public float Dash { get; init; }

    /// <summary>The gap between two dashes, in pixels.</summary>
    public float Gap { get; init; }

    /// <summary>How far the dashes are moved along the outline, in pixels.</summary>
    public float Phase { get; init; }

    /// <summary>The colour the fill runs to, or <c>null</c> for a fill in one colour.</summary>
    public Color? GradientTo { get; init; }

    /// <summary>The direction the gradient runs in, or <c>null</c> for top to bottom.</summary>
    public Vector2? GradientAlong { get; init; }

    /// <summary>The opacity of the whole shape, on top of the canvas's own.</summary>
    public float Opacity { get; init; } = 1f;

    /// <summary>Whether the fill is the glass pattern instead of a flat colour.</summary>
    public bool Textured { get; init; }
}