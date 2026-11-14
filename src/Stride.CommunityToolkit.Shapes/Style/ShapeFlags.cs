namespace Stride.CommunityToolkit.Shapes;

/// <summary>
/// The flag bits of a shape record as the shader reads them; ShapeShader.sdsl lists the same nine.
/// </summary>
[Flags]
internal enum ShapeFlags
{
    /// <summary>Only a band along the outline is kept.</summary>
    Hollow = 1,

    /// <summary>The ends of an arc are rounded.</summary>
    RoundCaps = 2,

    /// <summary>The fill runs from the fill colour to the gradient colour along the gradient direction.</summary>
    Gradient = 4,

    /// <summary>Radius and band depth are in pixels.</summary>
    PixelRadius = 8,

    /// <summary>The points are a polyline run.</summary>
    Polyline = 16,

    /// <summary>The glow adds light rather than covering what is behind it.</summary>
    AdditiveGlow = 32,

    /// <summary>The points are 3D and the run is stroked on screen.</summary>
    Space = 64,

    /// <summary>The fill is multiplied by the batch's fill source.</summary>
    Textured = 128,

    /// <summary>Position and points are pixels from the top left of the viewport, not a plane in the world.</summary>
    Screen = 256,
}