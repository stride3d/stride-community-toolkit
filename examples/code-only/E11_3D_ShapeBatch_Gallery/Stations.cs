using Example.Common.Galleries;
using Stride.CommunityToolkit.Rendering.Text;
using Stride.CommunityToolkit.Shapes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering.Compositing;

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
        new("Disc", "filled discs on the ground, pulsing", nameof(ShapeBatch.DrawDisc), Update: ShapeStations.Disc),
        new("Ring", "the disc with no fill, a marker that tints nothing", nameof(ShapeBatch.DrawRing), Update: ShapeStations.Ring, Pillars: 1, Anchor: new Vector3(-3.5f, 0.3f, 0.4f)),
        new("Polygon", "any convex polygon on any plane, a decal", nameof(ShapeBatch.DrawSolidPolygon), Update: ShapeStations.Polygon),
        new("Rectangle", "rounded corners, on the ground and standing up", nameof(ShapeBatch.DrawRectangle), Update: ShapeStations.Rectangle, Anchor: new Vector3(0f, 0.3f, -2f)),
        new("Sector", "pie wedges, a donut chart, a field of view", nameof(ShapeBatch.DrawSector), Update: ShapeStations.Sector, Pillars: 1, Anchor: new Vector3(1.5f, 0.3f, 0.5f)),
        new("Annulus and arc", "rings with width, progress bars bent round", nameof(ShapeBatch.DrawArc), Update: ShapeStations.AnnulusAndArc, Anchor: new Vector3(-3f, 0.3f, 0f)),
        new("Line", "capsules swung to face the camera: thick 3D lines", nameof(ShapeBatch.DrawLine), Update: StrokeStations.Line, Pillars: 2, Anchor: new Vector3(0f, 7f, 0f)),
        new("Wire box", "twelve lines, a selection volume", nameof(ShapeBatch.DrawWireBox), Update: StrokeStations.WireBox, Pillars: 1, Anchor: new Vector3(-3.5f, 5.5f, -1.5f)),
        new("Polyline", "a run of points as one stroke with round joins", nameof(ShapeBatch.DrawPixelPolyline), Update: StrokeStations.Polyline, Anchor: new Vector3(0f, 1.5f, -3f)),
        new("Space stroke", "3D points stroked on screen, no plane, no geometry", nameof(ShapeBatch.DrawPolyline), Update: StrokeStations.SpaceStroke, Pillars: 1, Anchor: new Vector3(-3.5f, 5.2f, -1.5f)),
        new("Billboard", "markers that keep their shape from any angle", nameof(ShapeBatch.DrawBillboardCircle), Update: ShapeStations.Billboard, Pillars: 2, Anchor: new Vector3(0f, 6f, 0f)),
        new("Distance proof", "a corridor of rings: they shrink, their outlines do not", nameof(ShapeBatch.DrawRing), Update: ShapeStations.DistanceProof, Anchor: new Vector3(0f, 0.4f, -6f)),
        new("HUD panel with text", "a glowing panel with world text on it", nameof(ShapeBatch.DrawRectangle), Setup: ShapeStations.HudPanelSetup, Update: ShapeStations.HudPanel, Anchor: new Vector3(0f, 1.2f, -1f)),
        new("Text overflow", "a shape never clips the text on it; wrap it yourself", nameof(WorldTextComponent.Text), Setup: ShapeStations.TextOverflowSetup, Update: ShapeStations.TextOverflow, Anchor: new Vector3(-2.4f, 1.5f, -1f)),
        new("Fill colour", "the outline's colour, its own colour, or no border", nameof(ShapeBatch.Fill), Update: EffectStations.FillColour),
        new("Glow", "a halo outside the outline, in pixels, for contrast or neon", nameof(ShapeBatch.Glow), Update: EffectStations.Glow, Anchor: new Vector3(0f, 2.4f, -1f)),
        new("Dash", "dashes in pixels, one ring still and the rest turning", nameof(ShapeBatch.Dash), Update: EffectStations.Dash, Anchor: new Vector3(0f, 0.3f, 1.5f)),
        new("Gradient", "a fill that runs to a colour, or fades to nothing", nameof(ShapeBatch.Gradient), Update: EffectStations.Gradient, Anchor: new Vector3(0f, 3.4f, 0f)),
        new("Opacity", "border, fill and glow dimmed together", nameof(ShapeBatch.Opacity), Update: EffectStations.Opacity, Anchor: new Vector3(-3.6f, 3f, 0f)),
        new("Depth fade", "a shape that melts into geometry instead of cutting off", nameof(ShapeBatch.DepthFade), Update: EffectStations.DepthFade, Pillars: 1, Anchor: new Vector3(-3.5f, 0.6f, -0.4f)),
        new("Overlay batch", "two batches: the overlay ring shows through the pillar", nameof(ShapeBatchExtensions.AddShapeBatch), Update: EffectStations.OverlayBatch, Pillars: 1, Anchor: new Vector3(-3.5f, 4.8f, -1.5f)),
        new("Screen shapes", "a HUD in pixels from the same batch, shown while you stand here", nameof(ShapeBatch.Screen), Update: EffectStations.ScreenHud),
        new("Textured fill", "a picture inside an outlined shape", nameof(ShapeBatch.FillSource), Update: EffectStations.TexturedFill, Anchor: new Vector3(-2.5f, 0.6f, 0f)),
        new("Scrolling texture", "the picture tiled and moving, one offset a frame", nameof(ShapeBatch.FillWith), Update: EffectStations.ScrollingTexture, Anchor: new Vector3(0f, 0.6f, 0f)),
        new("Shader fill", "a fill computed by a shader class of your own, no texture", nameof(ShapeBatch.FillSource), Update: EffectStations.ShaderFill, Anchor: new Vector3(0f, 0.6f, 0f)),
        new("Second camera in a panel", "a live view from another camera, inside a shape", nameof(RenderTextureSceneRenderer), Setup: EffectStations.MirrorSetup, Update: EffectStations.Mirror, Pillars: 1, Anchor: new Vector3(-2.5f, 1f, -1f)),
        new("Picking", "which shape is under the mouse: the batch knows, from what it drew", nameof(ShapeBatch.TryPick), Setup: EffectStations.PickingSetup, Update: EffectStations.Picking, Anchor: new Vector3(0f, 0.3f, 1.5f)),
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

/// <summary>The lettering the HUD stations share: world text in the HUD blue with a glow, standing in the station's upright plane.</summary>
internal static class HudLabel
{
    /// <summary>Adds a label to the station's scene and returns its component, for the station to keep and write to.</summary>
    /// <param name="s">The station the label belongs to.</param>
    /// <param name="key">What the label is, for its entity's name.</param>
    /// <param name="height">The height of the whole block of text, in world units.</param>
    /// <param name="local">Where it stands, in station coordinates.</param>
    /// <param name="text">The words to start with.</param>
    /// <param name="fontSize">The size the glyphs are rasterised at.</param>
    /// <param name="glow">The width of the glow, in pixels.</param>
    public static WorldTextComponent Add(ShapeStation s, string key, float height, Vector3 local, string text = "", int fontSize = 48, float glow = 4f)
    {
        var label = new WorldTextComponent
        {
            Text = text,
            FontSize = fontSize,
            Height = height,
            TextColor = Palette.HudBlue,
            GlowColor = new Color(0, 140, 255, 170),
            GlowSize = glow,
            Alignment = TextAlignment.Center,
            Billboard = false,
        };

        new Entity($"Station {s.Number} {key}", s.At(local), s.FacingRotation()) { label }.Scene = s.Scene;

        return label;
    }
}