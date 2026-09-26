using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;
using System.Runtime.InteropServices;

namespace Stride.CommunityToolkit.Shapes;

/// <summary>
/// Immediate-mode drawing of filled convex shapes whose outline stays a constant number of pixels
/// wide at any zoom, distance or window size, because the shader measures it per fragment from a
/// signed distance function instead of building it as geometry. "Pixels" here means pixels on a
/// 100% display: the widths follow the display's scale by default (see <see cref="AutoScale"/>),
/// so they are the same size to the eye everywhere.
/// </summary>
/// <remarks>
/// <para>
/// Shapes are flat, but they can sit anywhere in 3D: on a plane you choose, facing the camera, or
/// swung about an axis so a capsule reads as a thick 3D line. Every shape submitted in a frame goes
/// out in a single instanced draw call, however many there are.
/// </para>
/// <para>
/// Submit shapes every frame from your update logic; they are drawn once, blended in submission
/// order, and the batch resets itself after rendering. Register with <c>game.AddShapeBatch()</c>.
/// </para>
/// <para>
/// <see cref="BorderWidth"/>, <see cref="Fill"/>, <see cref="Glow"/>, <see cref="Dash"/>,
/// <see cref="Gradient"/>, <see cref="Opacity"/>, <see cref="DepthFade"/>, <see cref="Textured"/> and <see cref="Screen"/> are current state, captured by each draw call
/// as it is made, so you can change them between calls the way you would with a sprite batch.
/// </para>
/// </remarks>
public sealed partial class ShapeBatch : RenderObject
{
    internal readonly List<ShapeInstance> Instances = [];

    // Every submitted shape's points, one run after another, each already in its shape's
    // normalized space; an instance says where its run starts
    internal readonly List<Vector2> Points = [];

    // Every space run.s points, a world position with the stroke radius beside it in w, in
    // submission order; the record says where its piece starts
    internal readonly List<Vector4> SpacePoints = [];

    // Where this batch's records and points start in the frame's shared buffers; the render
    // feature sets both when it gathers every batch for upload
    internal int InstanceBase { get; set; }

    internal int PointBase { get; set; }

    internal int SpacePointBase { get; set; }

    // A polyline longer than this is split into runs that share an end point. The pixel stage
    // tests every segment of a run for every fragment of its quad, so the cap bounds the cost of a
    // very long run; where two runs meet, the shared round cap is drawn twice, which shows only
    // under an opacity below one.
    private const int PolylineRunLength = 64;

    // Scratch for closing a polyline, so a long run costs no allocation per frame
    private readonly List<Vector2> _run = [];
    private readonly List<Vector3> _spaceRun = [];

    /// <summary>
    /// How many shapes have been submitted so far this frame. Resets to zero once the batch is
    /// drawn, so read it after your own submissions and before the frame ends.
    /// </summary>
    public int Count => Instances.Count;

    /// <summary>
    /// Outline width in on-screen pixels, constant at any zoom or distance. The Box2D testbed uses
    /// 3; set 0 for a borderless fill. Captured by each draw call as it is made.
    /// </summary>
    public float BorderWidth { get; set; } = 3f;

    /// <summary>
    /// How the interior is painted: the fill's own colour, or <c>null</c> for the outline colour the
    /// testbed way, and its intensity. See <see cref="ShapeFill"/>. Captured by each draw call as it
    /// is made.
    /// </summary>
    public ShapeFill Fill { get; } = new();

    /// <summary>
    /// A soft glow outside the outline - width in pixels, and a colour or <c>null</c> for the
    /// outline's. See <see cref="ShapeGlow"/>. Captured by each draw call as it is made.
    /// </summary>
    public ShapeGlow Glow { get; } = new();

    /// <summary>
    /// The dash pattern along outlines - length, gap and phase in on-screen pixels. A length of 0,
    /// the default, draws solid. Circles, arcs and lines dash; polygons stay solid. See
    /// <see cref="DashPattern"/>. Captured by each draw call as it is made.
    /// </summary>
    public DashPattern Dash { get; } = new();

    /// <summary>
    /// A gradient across the fill: the colour it runs to and the direction. <c>Gradient.Color</c>
    /// left <c>null</c>, the default, is a flat fill. See <see cref="FillGradient"/>. Captured by
    /// each draw call as it is made.
    /// </summary>
    public FillGradient Gradient { get; } = new();

    /// <summary>
    /// A multiplier on everything a shape draws - border, fill and glow alike - from 0 to 1. The
    /// default 1 changes nothing. Captured by each draw call as it is made.
    /// </summary>
    /// <remarks>
    /// This is how a widget goes disabled or fades in: one assignment, rather than an alpha edit on
    /// each of its colours. It multiplies the alpha the colours already carry, so a fill at half
    /// alpha under an opacity of a half draws at a quarter.
    /// </remarks>
    public float Opacity { get; set; } = 1f;

    /// <summary>
    /// Distance in world units over which a shape fades out as it approaches scene geometry, instead
    /// of cutting off at the depth test. The default 0 keeps the hard cut. Captured by each draw call
    /// as it is made.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A disc on an uneven floor fades where the floor rises through it; a ring crossing a wall
    /// dissolves into the wall rather than being sliced by it. The same idea as soft particles: the
    /// shader compares its own distance from the camera with the scene's at the same pixel.
    /// </para>
    /// <para>
    /// It needs the depth the forward renderer resolves before the transparent stage, which is on by
    /// default; where a compositor turns that off the fade has nothing to read and the shape keeps
    /// its hard cut. On an overlay batch a fragment behind the surface fades to nothing too, which
    /// makes the fade a soft depth test of its own.
    /// </para>
    /// </remarks>
    public float DepthFade { get; set; }

    /// <summary>
    /// The fill source every textured shape in this batch samples: one of Stride's material nodes,
    /// such as a <see cref="ComputeTextureColor"/> with a texture, a scale, an offset and address
    /// modes, a blend of two nodes, or a custom shader class. <c>null</c>, the default, is a plain
    /// batch. See <see cref="FillWith"/> for the common case.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The sample multiplies the fill colour, alpha included: a white fill shows the image as it is,
    /// a tint darkens it, <see cref="ShapeFill.Alpha"/> fades it and a <see cref="Gradient"/> still
    /// runs over it. The border and the glow are untouched. The texture spans the shape's bounding
    /// box, (0,0) at the top left; a thick border reaches a little past the box, where the node's
    /// address mode decides what shows.
    /// </para>
    /// <para>
    /// It is one source per batch, because a shader composition is resolved when the effect is
    /// built. Assigning a different node reloads the effect once; changing a node's own properties
    /// - its texture, scale or offset - is picked up the next frame with no reload, which is how a
    /// scrolling stripe animates. Shapes drawn with <see cref="Textured"/> off ignore it.
    /// </para>
    /// </remarks>
    public IComputeColor? FillSource { get; set; }

    /// <summary>
    /// Whether draw calls sample <see cref="FillSource"/>. Defaults to <see langword="true"/>, so
    /// setting a fill source textures everything; turn it off around the shapes that should keep
    /// a flat fill, the way any other state is switched between draw calls. Without a fill source
    /// it does nothing.
    /// </summary>
    public bool Textured { get; set; } = true;

    /// <summary>
    /// Fills the batch's textured shapes with a texture: the common case of <see cref="FillSource"/>.
    /// </summary>
    /// <param name="texture">The texture to sample, or a render target another camera draws into.</param>
    /// <param name="scale">How many times the texture repeats across the shape's bounding box; the default 1 fits it once.</param>
    /// <param name="offset">Where the texture starts, in texture units; animate it for a scrolling fill.</param>
    /// <param name="addressMode">
    /// What shows beyond the texture's edges - beyond the bounding box, or past one repeat. The
    /// default <see cref="TextureAddressMode.Clamp"/> extends the edge pixels, right for a picture;
    /// <see cref="TextureAddressMode.Wrap"/> tiles, for stripes and patterns.
    /// </param>
    /// <returns>The node it installed, so its properties can be changed later.</returns>
    public ComputeTextureColor FillWith(Texture texture, Vector2? scale = null, Vector2? offset = null, TextureAddressMode addressMode = TextureAddressMode.Clamp)
    {
        ArgumentNullException.ThrowIfNull(texture);

        var node = new ComputeTextureColor(texture, TextureCoordinate.Texcoord0, scale ?? Vector2.One, offset ?? Vector2.Zero)
        {
            AddressModeU = addressMode,
            AddressModeV = addressMode,
        };

        FillSource = node;

        return node;
    }

    /// <summary>
    /// Whether draw calls place their shapes in pixels on the screen rather than in the world:
    /// coordinates from the top left of the viewport, Y down, like a sprite, and drawn over
    /// everything else in the batch whatever its depth test says. Defaults to
    /// <see langword="false"/>. Captured by each draw call as it is made, so a HUD and the world it
    /// sits over come from one batch, in submission order.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <c>Vector2</c> overloads are the natural ones here; a <c>Vector3</c> position's Z is
    /// ignored, and a plane's axes are read as pixel axes. A thick line lies flat on the screen and a
    /// billboard is axis-aligned. Because Y runs down, a positive angle turns clockwise on screen,
    /// as it does for a sprite. Space strokes are not screen shapes.
    /// </para>
    /// <para>
    /// The coordinates are the display's scaled pixels when <see cref="AutoScale"/> is on, so a
    /// layout is the same size to the eye on every display, and <see cref="Corner"/> and
    /// <see cref="ScreenSize"/> speak the same units. A pixel-measured width, a dash or a glow
    /// means exactly what it means in the world.
    /// </para>
    /// </remarks>
    public bool Screen { get; set; }

    /// <summary>
    /// A rectangle of the screen, in pixels from the top left, that screen shapes are placed
    /// relative to: a chart draws in its own coordinates and lands where the rectangle is. The
    /// default <c>null</c> is the whole viewport. The rectangle offsets and sizes
    /// <see cref="Corner"/>; nothing is clipped to it.
    /// </summary>
    public RectangleF? Viewport { get; set; }

    /// <summary>
    /// The viewport's size in the pixels screen shapes use - the display's scaled pixels when
    /// <see cref="AutoScale"/> is on. Zero for a batch that was not registered through
    /// <c>game.AddShapeBatch()</c> and so has no window to ask.
    /// </summary>
    public Vector2 ScreenSize => ScreenSizeSource?.Invoke() ?? Vector2.Zero;

    /// <summary>
    /// The pixel position of a corner of what screen shapes draw into - the <see cref="Viewport"/>
    /// rectangle when one is set, otherwise the screen - so a widget is placed as a corner plus an
    /// offset rather than by a hardcoded resolution.
    /// </summary>
    /// <param name="corner">Which corner.</param>
    /// <returns>The corner, in the same coordinates the draw calls take.</returns>
    public Vector2 Corner(ScreenCorner corner)
        => corner.At(Viewport is { } viewport ? new Vector2(viewport.Width, viewport.Height) : ScreenSize);

    // Where the window's size comes from, in scaled pixels; set by AddShapeBatch
    internal Func<Vector2>? ScreenSizeSource { private get; set; }

    /// <summary>
    /// Whether the pixel-measured widths - border, glow, dashes, pixel lines - follow the display's
    /// scale, so a 2-pixel border is the same width to the eye on a 150% laptop as on a 100%
    /// monitor. Defaults to <see langword="true"/>.
    /// </summary>
    /// <remarks>
    /// The figure comes from <see cref="Rendering.DisplayScale"/>, shared with everything else in the
    /// toolkit that draws in pixels, and is re-read when the window moves to another monitor. Turn
    /// it off to get exactly the pixels asked for - a screenshot at a known size, or a game applying
    /// its own UI-scale setting through <see cref="Rendering.DisplayScale.Override"/> and nothing
    /// else should compound it. World-unit sizes are never affected either way.
    /// </remarks>
    public bool AutoScale { get; set; } = true;

    /// <summary>
    /// Whether shapes are tested against the depth buffer, so scene geometry can occlude them. They
    /// never write depth. The default is <c>false</c>, which draws them as an overlay on top of
    /// everything - what you want for gizmos and 2D scenes, but not for decals on the ground.
    /// </summary>
    /// <remarks>
    /// This applies to the whole batch. Call <c>game.AddShapeBatch()</c> a second time for a batch
    /// with the other setting when you need both in one scene.
    /// </remarks>
    public bool DepthTest { get; set; }

    /// <summary>Called by the render feature once the batch is drawn; the next frame starts empty, and the drawn frame's tagged shapes become what a pick is answered from.</summary>
    internal void Reset()
    {
        Instances.Clear();
        Points.Clear();
        SpacePoints.Clear();
        HandOverPicks();
    }

    /// <summary>A stroke with no area: a hollow band of zero depth, which is what a ring or an arc is.</summary>
    private static readonly ShapeSlice Stroke = new(Hollow: true, RingWidth: 0f, StartAngle: 0f, SweepAngle: 0f, RoundCaps: false);

    private void AddSector(in ShapePlane plane, float radius, float innerRadius, float startAngle, float sweepAngle, Color color)
    {
        if (radius <= 0f || innerRadius >= radius || !ShapeSlice.TryNormalizeSweep(ref startAngle, ref sweepAngle)) return;

        var slice = new ShapeSlice(Hollow: innerRadius > 0f, RingWidth: radius - innerRadius, startAngle, sweepAngle, RoundCaps: false);

        Add([Vector2.Zero], plane, CurrentStyle(color), slice, radius, 1f);
    }

    private void AddArc(in ShapePlane plane, float radius, float startAngle, float sweepAngle, Color color, float width)
    {
        if (radius <= 0f || !ShapeSlice.TryNormalizeSweep(ref startAngle, ref sweepAngle)) return;

        // The band straddles the radius, so its outer edge is half a width beyond it
        var halfWidth = MathF.Max(width, 0f) * 0.5f;
        var slice = new ShapeSlice(Hollow: true, RingWidth: width, startAngle, sweepAngle, RoundCaps: true);

        // A stroke has no area to fill; a band takes the current fill like any other shape
        Add([Vector2.Zero], plane, halfWidth > 0f ? CurrentStyle(color) : OutlineStyle(color), slice, radius + halfWidth, 1f);
    }

    /// <summary>The colours, border, fill and glow as they stand right now, which is what a draw call captures.</summary>
    private ShapeStyle CurrentStyle(Color color)
    {
        // No explicit fill colour: the testbed's own formula, where FillAlpha scales the outline
        // colour's brightness as well as its opacity. Keeping it verbatim is what makes the Box2D
        // examples match the testbed side by side.
        if (Fill.Color is not { } fill)
        {
            // The gradient's far end gets the same treatment as the near one: scaled by the fill
            // alpha in the shader, so the two ends dim together
            return new(color, color, BorderWidth, Fill.Alpha, Glow.Capture(color), Dash.Capture(), CaptureGradient(color), Opacity, DepthFade, TexturedNow, Screen);
        }

        // An explicit fill colour is used as given. Dimming its brightness the testbed way would
        // turn a chosen colour into a muddy version of itself, so the fill alpha scales opacity only.
        var near = WithFillAlpha(fill);
        var gradient = Gradient.Color is { } to ? new GradientStyle(true, WithFillAlpha(to), Gradient.Direction) : new GradientStyle(false, near, Gradient.Direction);

        return new(color, near, BorderWidth, 1f, Glow.Capture(color), Dash.Capture(), gradient, Opacity, DepthFade, TexturedNow, Screen);
    }

    /// <summary>A screen shape drawn into a <see cref="Viewport"/> rectangle is moved to it here; every other shape is placed as given.</summary>
    private ShapePlane Placed(in ShapePlane plane, in ShapeStyle style)
        => style.Screen && Viewport is { } viewport ? plane with { Origin = plane.Origin + new Vector3(viewport.X, viewport.Y, 0f) } : plane;

    /// <summary>The current style with the fill turned off, for shapes that are all outline.</summary>
    private ShapeStyle OutlineStyle(Color color) => new(color, color, BorderWidth, 0f, Glow.Capture(color), Dash.Capture(), new GradientStyle(false, color, Gradient.Direction), Opacity, DepthFade, TexturedNow, Screen);

    /// <summary>The current style with the fill turned off and its own outline width.</summary>
    private ShapeStyle OutlineStyle(Color color, float borderWidth) => new(color, color, borderWidth, 0f, Glow.Capture(color), Dash.Capture(), new GradientStyle(false, color, Gradient.Direction), Opacity, DepthFade, TexturedNow, Screen);

    /// <summary>
    /// The current style with the fill turned all the way up, for shapes that are drawn solid. A
    /// gradient still applies: a line that fades out along its length is a leader line.
    /// </summary>
    private ShapeStyle SolidStyle(Color color) => new(color, color, BorderWidth, 1f, Glow.Capture(color), Dash.Capture(), CaptureGradient(color), Opacity, DepthFade, TexturedNow, Screen);

    /// <summary>Whether the next draw call is textured: asked for, and with a fill source to sample.</summary>
    private bool TexturedNow => Textured && FillSource is not null;

    /// <summary>The gradient as a draw call captures it, its far colour taken as given - the shader scales it by the fill alpha.</summary>
    private GradientStyle CaptureGradient(Color fallback) => new(Gradient.Color is not null, Gradient.Color ?? fallback, Gradient.Direction);

    /// <summary>An explicit colour with the fill alpha applied to its opacity only.</summary>
    private Color WithFillAlpha(Color colour) => new(colour.R, colour.G, colour.B, (byte)Math.Clamp(colour.A * Fill.Alpha, 0f, 255f));

    /// <summary>
    /// Submits a run as one stroke, or as a few runs sharing their end points when it is very
    /// long, each carrying the arc length at which it starts so a dash pattern continues across them.
    /// </summary>
    private void AddPolyline(ReadOnlySpan<Vector2> points, in ShapePlane plane, in ShapeStyle style, float radius, bool closed)
    {
        if (points.Length < 2) return;

        _run.Clear();

        foreach (var point in points) _run.Add(point);

        if (closed) _run.Add(points[0]);

        var run = CollectionsMarshal.AsSpan(_run);
        var offset = 0f;

        for (var start = 0; start + 1 < run.Length; start += PolylineRunLength - 1)
        {
            var piece = run.Slice(start, Math.Min(PolylineRunLength, run.Length - start));

            Add(piece, plane, style, ShapeSlice.Whole with { Polyline = true, RunOffset = offset }, radius, 1f);

            for (var i = 0; i + 1 < piece.Length; i++) offset += Vector2.Distance(piece[i], piece[i + 1]);
        }
    }

    /// <summary>
    /// Records a space run: its points go to the space point buffer with the stroke radius beside
    /// each, in pieces of at most <see cref="PolylineRunLength"/> points that share an end point,
    /// and one record per piece flagged for the screen-space path. No plane and no normalization:
    /// the pixel stage projects the points as they are.
    /// </summary>
    private void AddSpacePolyline(ReadOnlySpan<Vector3> points, in ShapeStyle style, float radius, bool closed)
    {
        if (points.Length < 2) return;

        _spaceRun.Clear();

        foreach (var point in points) _spaceRun.Add(point);

        if (closed) _spaceRun.Add(points[0]);

        var run = CollectionsMarshal.AsSpan(_spaceRun);

        for (var start = 0; start + 1 < run.Length; start += PolylineRunLength - 1)
        {
            var piece = run.Slice(start, Math.Min(PolylineRunLength, run.Length - start));
            var offset = SpacePoints.Count;

            foreach (var point in piece) SpacePoints.Add(new Vector4(point, radius));

            Instances.Add(new ShapeInstance(SpacePlane, style, SpaceStroke, new ShapePointRun(offset, piece.Length, Vector2.Zero, 1f), radius, 1f));
            RecordSpacePick(piece, style, radius);
        }
    }

    /// <summary>A stand-in plane for a space run, which has none; the shader never reads it.</summary>
    private static readonly ShapePlane SpacePlane = new(Vector3.Zero, Vector3.UnitX, Vector3.UnitY, PlaneMode.Fixed);

    /// <summary>The slice of every space run: a polyline, stroked on screen.</summary>
    private static readonly ShapeSlice SpaceStroke = ShapeSlice.Whole with { Polyline = true, Space = true };

    /// <summary>
    /// Records one shape: its points shifted and scaled into the 2x2 quad the shader draws, so
    /// the pixel stage reads them ready to use, and the record that says where they are.
    /// </summary>
    private void Add(ReadOnlySpan<Vector2> vertices, in ShapePlane plane, in ShapeStyle style, in ShapeSlice slice, float radius, float scale)
    {
        var placed = Placed(plane, style);
        if (vertices.Length < 1)
            throw new ArgumentException("A shape needs at least one vertex.", nameof(vertices));

        // A pixel-measured radius is converted to world units on the GPU, at the shape's own
        // depth, and the scale with it - which only works out when there is nothing else to scale
        if (slice.PixelRadius && vertices.Length != 1)
            throw new ArgumentException("A shape with a pixel-measured radius is a single point.", nameof(vertices));

        var lower = vertices[0];
        var upper = vertices[0];

        for (var i = 1; i < vertices.Length; i++)
        {
            lower = Vector2.Min(lower, vertices[i]);
            upper = Vector2.Max(upper, vertices[i]);
        }

        var center = (lower + upper) * 0.5f;
        var extent = upper - lower;

        // The radius reaches beyond the furthest point either way, so radius plus half the widest
        // extent is the half-size of the square that holds the outline; the border and glow
        // get their room from the vertex stage's margin
        var localScale = radius + 0.5f * MathF.Max(extent.X, extent.Y);
        var invScale = 1f / MathF.Max(localScale, 0.000001f);

        var offset = Points.Count;

        for (var i = 0; i < vertices.Length; i++)
        {
            Points.Add(invScale * (vertices[i] - center));
        }

        Instances.Add(new ShapeInstance(placed, style, slice, new ShapePointRun(offset, vertices.Length, center, localScale), radius, scale));
        RecordPick(vertices, placed, style, slice, radius, scale);
    }
}