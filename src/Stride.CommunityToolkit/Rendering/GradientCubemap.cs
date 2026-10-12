using Stride.Graphics;

namespace Stride.CommunityToolkit.Rendering;

/// <summary>
/// Builds a cubemap whose colour depends only on the direction's elevation: a gradient sky with no texture asset.
/// </summary>
public static class GradientCubemap
{
    /// <summary>
    /// Creates the cubemap: <paramref name="horizon"/> at the horizon, blending to <paramref name="zenith"/> above
    /// and <paramref name="nadir"/> below.
    /// </summary>
    /// <param name="device">The graphics device the texture is created on.</param>
    /// <param name="zenith">The colour straight up.</param>
    /// <param name="horizon">The colour at the horizon.</param>
    /// <param name="nadir">The colour straight down.</param>
    /// <param name="size">The width of each face, in pixels; the gradient is smooth, so a small face is enough.</param>
    /// <returns>An immutable cube texture usable as a <see cref="Stride.Engine.BackgroundComponent"/> texture and as a skybox source.</returns>
    public static Texture Create(GraphicsDevice device, Color zenith, Color horizon, Color nadir, int size = 64)
    {
        ArgumentNullException.ThrowIfNull(device);
        ArgumentOutOfRangeException.ThrowIfLessThan(size, 1);

        var faces = new Color[6][];

        for (var face = 0; face < 6; face++)
        {
            var pixels = new Color[size * size];

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    // Pixel centre in [-1, 1] on the face, then the direction through it
                    var u = (x + 0.5f) / size * 2f - 1f;
                    var v = (y + 0.5f) / size * 2f - 1f;
                    var direction = Direction(face, u, v);

                    direction.Normalize();

                    pixels[y * size + x] = ColorAt(direction.Y, zenith, horizon, nadir);
                }
            }

            faces[face] = pixels;
        }

        return Texture.NewCube(device, size, PixelFormat.R8G8B8A8_UNorm, faces);
    }

    // Face order and orientation as Direct3D lays a cubemap out: +X, -X, +Y, -Y, +Z, -Z, with v running down
    private static Vector3 Direction(int face, float u, float v) => face switch
    {
        0 => new Vector3(1f, -v, -u),
        1 => new Vector3(-1f, -v, u),
        2 => new Vector3(u, 1f, v),
        3 => new Vector3(u, -1f, -v),
        4 => new Vector3(u, -v, 1f),
        _ => new Vector3(-u, -v, -1f),
    };

    private static Color ColorAt(float elevation, Color zenith, Color horizon, Color nadir)
    {
        // Eased so the horizon band stays wide, as a real sky's does
        var t = MathF.Sqrt(MathF.Abs(elevation));

        return elevation >= 0f ? Color.Lerp(horizon, zenith, t) : Color.Lerp(horizon, nadir, t);
    }
}