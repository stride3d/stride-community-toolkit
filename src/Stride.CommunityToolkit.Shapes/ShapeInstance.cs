using Stride.Core.Mathematics;
using System.Runtime.InteropServices;

namespace Stride.CommunityToolkit.Shapes;

/// <summary>
/// The GPU-facing layout of one shape, mirrored field for field by the shader's ShapeData. The
/// field order is the wire format, so do not reorder it without changing ShapeShader.sdsl to match.
/// </summary>
/// <remarks>
/// <para>
/// Laid out in 16-byte groups, each a <see cref="Vector4"/>, four 4-byte values or one of the named
/// groups below, which is the alignment structured buffers want and what keeps the shader's reads
/// simple. Where a group has a spare slot it carries a related scalar - the plane axes carry the
/// border width and fill alpha, the slice the glow width, the gradient the opacity - rather than
/// padding.
/// </para>
/// <para>
/// The points are not here. They live in the batch's point buffer, already shifted by
/// <see cref="Center"/> and divided by <see cref="LocalScale"/> into the shape's normalized space,
/// and the record says where its run starts and how long it is - so a shape has as many points as
/// it needs, and the pixel stage reads them straight from the buffer.
/// </para>
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
internal readonly struct ShapeInstance
{
    /// <summary>Bit 2 of the GPU flags: the fill runs from <see cref="FillColor"/> to <see cref="GradientColor"/> along the gradient direction.</summary>
    internal const int GradientFlag = 4;

    /// <summary>Bit 5 of the GPU flags: the glow adds light rather than covering what is behind it.</summary>
    internal const int AdditiveGlowFlag = 32;

    /// <summary>The fill is multiplied by the batch's fill source.</summary>
    internal const int TexturedFlag = 128;

    /// <summary>Position and points are pixels from the top left of the viewport, not a plane in the world.</summary>
    internal const int ScreenFlag = 256;

    /// <summary>xyz: world position of the local origin; w: plane mode.</summary>
    public readonly Vector4 Position;

    /// <summary>xyz: the plane's X axis; w: border width in pixels.</summary>
    public readonly Vector4 AxisX;

    /// <summary>xyz: the plane's Y axis; w: fill alpha.</summary>
    public readonly Vector4 AxisY;

    /// <summary>Centre of the points' bounding box, in the plane's local units: what the points were shifted by.</summary>
    public readonly Vector2 Center;

    /// <summary>The rounding radius plus half the widest extent: what the shifted points were divided by.</summary>
    public readonly float LocalScale;

    /// <summary>A uniform scale on the world footprint.</summary>
    public readonly float Scale;

    /// <summary>Where the shape's points start in the batch's point buffer.</summary>
    public readonly int PointOffset;

    /// <summary>How many points the shape has.</summary>
    public readonly int Count;

    /// <summary>The rounding radius, in world units, or in pixels when the slice says so.</summary>
    public readonly float Radius;

    public readonly int Flags;

    public readonly Color Color;
    public readonly Color FillColor;
    public readonly Color GlowColor;
    public readonly Color GradientColor;

    // The rest, one named group each. Nothing on the CPU reads them back, so they are private;
    // the shader reads the bytes.

    private readonly SliceData _slice;
    private readonly DashData _dash;
    private readonly GradientData _gradient;

    internal ShapeInstance(in ShapePlane plane, in ShapeStyle style, in ShapeSlice slice, in ShapePointRun run, float radius, float scale)
    {
        Position = new Vector4(plane.Origin, (float)plane.Mode);
        AxisX = new Vector4(plane.AxisX, style.BorderWidth);
        AxisY = new Vector4(plane.AxisY, style.FillAlpha);
        Center = run.Center;
        LocalScale = run.LocalScale;
        Scale = scale;
        PointOffset = run.Offset;
        Count = run.Count;
        Radius = radius;
        Flags = slice.Flags | (style.Gradient.Enabled ? GradientFlag : 0) | (style.Glow.Additive ? AdditiveGlowFlag : 0) | (style.Textured ? TexturedFlag : 0) | (style.Screen ? ScreenFlag : 0);
        Color = style.Color;
        FillColor = style.FillColor;
        GlowColor = style.Glow.Color;
        GradientColor = style.Gradient.Color;
        _slice = new SliceData(slice.RingWidth, slice.StartAngle, slice.SweepAngle, style.Glow.Width);
        _dash = new DashData(style.Dash.Length, style.Dash.Gap, style.Dash.Phase, slice.RunOffset);
        _gradient = new GradientData(style.Gradient.Direction, style.Opacity, style.DepthFade);
    }

    /// <summary>Which part of the shape is kept, plus the glow width in the spare slot.</summary>
    [StructLayout(LayoutKind.Sequential)]
    private readonly struct SliceData
    {
        /// <summary>How deep the band reaches inward from the outline, in world units, when hollow.</summary>
        public readonly float RingWidth;

        /// <summary>Where the angular cut starts, radians from the plane's X axis, counter-clockwise.</summary>
        public readonly float AngleStart;

        /// <summary>How far the cut extends; 0 means no cut.</summary>
        public readonly float AngleSweep;

        /// <summary>Outer glow width in pixels; 0 for none.</summary>
        public readonly float GlowWidth;

        internal SliceData(float ringWidth, float angleStart, float angleSweep, float glowWidth)
        {
            RingWidth = ringWidth;
            AngleStart = angleStart;
            AngleSweep = angleSweep;
            GlowWidth = glowWidth;
        }
    }

    /// <summary>The dash pattern, in pixels.</summary>
    [StructLayout(LayoutKind.Sequential)]
    private readonly struct DashData
    {
        /// <summary>Length of each dash; 0 for solid.</summary>
        public readonly float Length;

        /// <summary>Gap between dashes.</summary>
        public readonly float Gap;

        /// <summary>Where the pattern starts.</summary>
        public readonly float Phase;

        /// <summary>For a polyline, the arc length along the whole run at its first point, in world units.</summary>
        public readonly float RunOffset;

        internal DashData(float length, float gap, float phase, float runOffset)
        {
            Length = length;
            Gap = gap;
            Phase = phase;
            RunOffset = runOffset;
        }
    }

    /// <summary>The fill gradient's direction, plus the opacity and the soft depth fade in the spare slots.</summary>
    [StructLayout(LayoutKind.Sequential)]
    private readonly struct GradientData
    {
        /// <summary>The direction the gradient runs in, in the plane's local axes.</summary>
        public readonly Vector2 Direction;

        /// <summary>A multiplier on every alpha the shape produces.</summary>
        public readonly float Opacity;

        /// <summary>Distance in world units over which the shape fades out as it nears scene geometry; 0 for a hard cut.</summary>
        public readonly float DepthFade;

        internal GradientData(Vector2 direction, float opacity, float depthFade)
        {
            Direction = direction;
            Opacity = opacity;
            DepthFade = depthFade;
        }
    }
}