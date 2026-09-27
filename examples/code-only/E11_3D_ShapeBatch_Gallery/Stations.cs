using Example.Common.Galleries;
using Stride.CommunityToolkit.Rendering.Text;
using Stride.CommunityToolkit.Shapes;
using Stride.Rendering.Compositing;
using Stride.Core.Mathematics;

namespace E11_3D_ShapeBatch_Gallery;

/// <summary>
/// The exhibits, in the order the ring shows them: simplest first, one ShapeBatch idea each. Every
/// station is one static method that draws in station coordinates and knows nothing about the
/// ring, so it can be copied into a game as it is; the few that need something built first have a
/// setup method beside them. Add a station here and the gallery grows to fit.
/// </summary>
public static class Stations
{
    public static IReadOnlyList<Exhibit<ShapeStation>> All { get; } =
    [
        new("Disc", "filled discs on the ground, pulsing", nameof(ShapeBatch.DrawDisc), ShapeStations.Disc),
        new("Ring", "the disc with no fill, a marker that tints nothing", nameof(ShapeBatch.DrawRing), ShapeStations.Ring, Pillars: 1, Anchor: new Vector3(-3.5f, 0.3f, 0.4f)),
        new("Polygon", "any convex polygon on any plane, a decal", nameof(ShapeBatch.DrawSolidPolygon), ShapeStations.Polygon),
        new("Rectangle", "rounded corners, on the ground and standing up", nameof(ShapeBatch.DrawRectangle), ShapeStations.Rectangle, Anchor: new Vector3(0f, 0.3f, -2f)),
        new("Sector", "pie wedges, a donut chart, a field of view", nameof(ShapeBatch.DrawSector), ShapeStations.Sector, Pillars: 1, Anchor: new Vector3(1.5f, 0.3f, 0.5f)),
        new("Annulus and arc", "rings with width, progress bars bent round", nameof(ShapeBatch.DrawArc), ShapeStations.AnnulusAndArc, Anchor: new Vector3(-3f, 0.3f, 0f)),
        new("Line", "capsules swung to face the camera: thick 3D lines", nameof(ShapeBatch.DrawLine), StrokeStations.Line, Pillars: 2, Anchor: new Vector3(0f, 7f, 0f)),
        new("Wire box", "twelve lines, a selection volume", nameof(ShapeBatch.DrawWireBox), StrokeStations.WireBox, Pillars: 1, Anchor: new Vector3(-3.5f, 5.5f, -1.5f)),
        new("Polyline", "a run of points as one stroke with round joins", nameof(ShapeBatch.DrawPixelPolyline), StrokeStations.Polyline, Anchor: new Vector3(0f, 1.5f, -3f)),
        new("Space stroke", "3D points stroked on screen, no plane, no geometry", nameof(ShapeBatch.DrawPolyline), StrokeStations.SpaceStroke, Pillars: 1, Anchor: new Vector3(-3.5f, 5.2f, -1.5f)),
        new("Billboard", "markers that keep their shape from any angle", nameof(ShapeBatch.DrawBillboardCircle), ShapeStations.Billboard, Pillars: 2, Anchor: new Vector3(0f, 6f, 0f)),
        new("Distance proof", "a corridor of rings: they shrink, their outlines do not", nameof(ShapeBatch.DrawRing), ShapeStations.DistanceProof, Anchor: new Vector3(0f, 0.4f, -6f)),
        new("HUD panel with text", "a glowing panel with world text on it", nameof(ShapeBatch.DrawRectangle), ShapeStations.HudPanel, ShapeStations.HudPanelSetup, Anchor: new Vector3(0f, 1.2f, -1f)),
        new("Text overflow", "a shape never clips the text on it; wrap it yourself", nameof(WorldTextComponent.Text), ShapeStations.TextOverflow, ShapeStations.TextOverflowSetup, Anchor: new Vector3(-2.4f, 1.5f, -1f)),
        new("Fill colour", "the outline's colour, its own colour, or no border", nameof(ShapeBatch.Fill), EffectStations.FillColour),
        new("Glow", "a halo outside the outline, in pixels, for contrast or neon", nameof(ShapeBatch.Glow), EffectStations.Glow, Anchor: new Vector3(0f, 2.4f, -1f)),
        new("Dash", "dashes in pixels, one ring still and the rest turning", nameof(ShapeBatch.Dash), EffectStations.Dash, Anchor: new Vector3(0f, 0.3f, 1.5f)),
        new("Gradient", "a fill that runs to a colour, or fades to nothing", nameof(ShapeBatch.Gradient), EffectStations.Gradient, Anchor: new Vector3(0f, 3.4f, 0f)),
        new("Opacity", "border, fill and glow dimmed together", nameof(ShapeBatch.Opacity), EffectStations.Opacity, Anchor: new Vector3(-3.6f, 3f, 0f)),
        new("Depth fade", "a shape that melts into geometry instead of cutting off", nameof(ShapeBatch.DepthFade), EffectStations.DepthFade, Pillars: 1, Anchor: new Vector3(-3.5f, 0.6f, -0.4f)),
        new("Overlay batch", "two batches: the overlay ring shows through the pillar", nameof(ShapeBatchExtensions.AddShapeBatch), EffectStations.OverlayBatch, Pillars: 1, Anchor: new Vector3(-3.5f, 4.8f, -1.5f)),
        new("Screen shapes", "a HUD in pixels from the same batch, shown while you stand here", nameof(ShapeBatch.Screen), EffectStations.ScreenHud),
        new("Textured fill", "a picture inside an outlined shape", nameof(ShapeBatch.FillSource), EffectStations.TexturedFill, Anchor: new Vector3(-2.5f, 0.6f, 0f)),
        new("Scrolling texture", "the picture tiled and moving, one offset a frame", nameof(ShapeBatch.FillWith), EffectStations.ScrollingTexture, Anchor: new Vector3(0f, 0.6f, 0f)),
        new("Shader fill", "a fill computed by a shader class of your own, no texture", nameof(ShapeBatch.FillSource), EffectStations.ShaderFill, Anchor: new Vector3(0f, 0.6f, 0f)),
        new("Second camera in a panel", "a live view from another camera, inside a shape", nameof(RenderTextureSceneRenderer), EffectStations.Mirror, EffectStations.MirrorSetup, Pillars: 1, Anchor: new Vector3(-2.5f, 1f, -1f)),
        new("Picking", "which shape is under the mouse: the batch knows, from what it drew", nameof(ShapeBatch.TryPick), EffectStations.Picking, EffectStations.PickingSetup, Anchor: new Vector3(0f, 0.3f, 1.5f)),
    ];

}

/// <summary>The few constants every station shares, so a copied method brings its own numbers.</summary>
internal static class Palette
{
    /// <summary>Shapes lying on the ground sit a hair above it, so they never fight the floor's depth.</summary>
    public const float Lift = 0.02f;

    /// <summary>The HUD look shared by the panel stations and the index board.</summary>
    public static readonly Color HudBlue = new(110, 200, 255);
    public static readonly Color HudFill = new(4, 14, 30);
    public static readonly Color HudGlow = new(0, 150, 255, 160);
}