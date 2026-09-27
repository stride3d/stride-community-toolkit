using Example.Common.Galleries;
using Stride.CommunityToolkit.Shapes;

namespace E11_3D_ShapeBatch_Gallery;

/// <summary>The batches the gallery draws through, so a station can pick the one it is about.</summary>
/// <param name="Scene">Depth-tested: the ground and the pillars occlude these shapes.</param>
/// <param name="Overlay">Drawn over everything, the gizmo batch.</param>
/// <param name="Pictures">Depth-tested, filled with the gallery's picture, clamped at its edges.</param>
/// <param name="Stripes">Depth-tested, the same picture tiled four times across and free to scroll.</param>
/// <param name="Stripe">The node behind <see cref="Stripes"/>, whose offset a station animates.</param>
/// <param name="Shaded">Depth-tested, filled by a shader class of the gallery's own instead of a texture.</param>
/// <param name="Clock">The node composed into <see cref="Shaded"/>'s shader, whose X a station sets to the time.</param>
public sealed record GalleryBatches(
    ShapeBatch Scene,
    ShapeBatch Overlay,
    ShapeBatch Pictures,
    ShapeBatch Stripes,
    Stride.Rendering.Materials.ComputeColors.ComputeTextureColor Stripe,
    ShapeBatch Shaded,
    Stride.Rendering.Materials.ComputeColors.ComputeFloat4 Clock);

/// <summary>The three per-frame states the visitor changes with keys, applied to every station.</summary>
public sealed class GalleryStyle
{
    public float BorderWidth { get; set; } = 3f;
    public float FillAlpha { get; set; } = 0.45f;
    public float GlowWidth { get; set; }
}

/// <summary>
/// A gallery station with what a ShapeBatch exhibit needs on top of the frame: the batches, the
/// visitor's style, and the batch to draw through this frame.
/// </summary>
public sealed class ShapeStation : GalleryStation
{
    /// <summary>Every batch the gallery draws through; set once by the gallery's configure step.</summary>
    public GalleryBatches Batches { get; set; } = null!;

    /// <summary>The visitor's border, fill and glow, applied to every station.</summary>
    public GalleryStyle Style { get; set; } = null!;

    /// <summary>The batch to draw through this frame: the scene one, or the overlay when T says so.</summary>
    public ShapeBatch Shapes { get; set; } = null!;

    /// <summary>
    /// Puts the gallery's current style back on a batch: the visitor's border, fill and glow, and
    /// none of the per-draw states a previous station may have left on.
    /// </summary>
    /// <param name="batch">The batch to reset.</param>
    public void ResetStyle(ShapeBatch batch)
    {
        batch.BorderWidth = Style.BorderWidth;
        batch.Fill.Set(null, Style.FillAlpha);
        batch.Glow.Clear();
        // The visitor's glow is in each shape's own colour, so at a third of its strength: light around the stroke, not a fatter one
        batch.Glow.Width = Style.GlowWidth;
        batch.Glow.Strength = 0.35f;
        batch.Dash.Clear();
        batch.Gradient.Clear();
        batch.Opacity = 1f;
        batch.DepthFade = 0f;

        // Per-draw like the rest: a station that turned it off for a bracket would otherwise leave
        // every later panel in the batch untextured
        batch.Textured = true;
        batch.Screen = false;
        batch.Viewport = null;
    }

    /// <summary>Resets every batch of the gallery, before an exhibit draws.</summary>
    public void ResetAll()
    {
        ResetStyle(Batches.Scene);
        ResetStyle(Batches.Overlay);
        ResetStyle(Batches.Pictures);
        ResetStyle(Batches.Stripes);
        ResetStyle(Batches.Shaded);
    }
}