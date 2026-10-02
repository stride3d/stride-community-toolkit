using Stride.Core.Mathematics;
using Stride.Graphics;

namespace E02_3D_Material_Gallery;

/// <summary>
/// Textures computed in C# and uploaded once: a height map, the normal map derived from it, a cel
/// ramp, a field of metal flakes, a mask of holes. No asset, no file - a <c>Color[]</c> and a
/// <c>Texture.New2D</c>. Every one is linear data, never sRGB.
/// </summary>
public static class RuntimeTextures
{
    /// <summary>Ripples from the centre with a little noise: a height map, 0 to 1 in every channel.</summary>
    public static Texture Ripples(GraphicsDevice device, int size = 256)
        => Texture.New2D(device, size, size, PixelFormat.R8G8B8A8_UNorm, RipplePixels(size));

    /// <summary>
    /// The normal map of a height map, by central differences, in the tangent-space convention a
    /// normal map holds: X and Y in red and green about 0.5, Z in blue, so it reads as XY with Z
    /// rebuilt like the pack's own maps.
    /// </summary>
    /// <param name="heights">The height map's pixels, any channel, row-major.</param>
    /// <param name="size">Its side.</param>
    /// <param name="strength">How steep a unit of height is.</param>
    public static Texture NormalFromHeight(GraphicsDevice device, Color[] heights, int size, float strength = 4f)
    {
        var pixels = new Color[size * size];

        float At(int x, int y) => heights[((y + size) % size) * size + (x + size) % size].R / 255f;

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var dx = (At(x + 1, y) - At(x - 1, y)) * strength;
                var dy = (At(x, y + 1) - At(x, y - 1)) * strength;
                var n = Vector3.Normalize(new Vector3(-dx, -dy, 1f));

                pixels[y * size + x] = new Color(n.X * 0.5f + 0.5f, n.Y * 0.5f + 0.5f, n.Z * 0.5f + 0.5f);
            }
        }

        return Texture.New2D(device, size, size, PixelFormat.R8G8B8A8_UNorm, pixels);
    }

    /// <summary>The pixels of a texture made here, for deriving another from it.</summary>
    public static Color[] RipplePixels(int size = 256)
    {
        var pixels = new Color[size * size];
        var random = new Random(7);

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var dx = x / (float)size - 0.5f;
                var dy = y / (float)size - 0.5f;
                var r = MathF.Sqrt(dx * dx + dy * dy);

                pixels[y * size + x] = Grey(0.5f + 0.4f * MathF.Sin(r * 60f) + 0.1f * ((float)random.NextDouble() - 0.5f));
            }
        }

        return pixels;
    }

    /// <summary>A one-row ramp of a few flat steps, what a cel shader looks the light up in: dark, mid, light.</summary>
    public static Texture Ramp(GraphicsDevice device, params float[] steps)
    {
        const int width = 64;
        var pixels = new Color[width];

        for (var x = 0; x < width; x++)
        {
            var step = Math.Min(steps.Length - 1, (int)(x / (float)width * steps.Length));

            pixels[x] = Grey(steps[step]);
        }

        return Texture.New2D(device, width, 1, PixelFormat.R8G8B8A8_UNorm, pixels);
    }

    /// <summary>Metal flakes: a normal map of small random tilts, the sparkle under a clear coat.</summary>
    public static Texture Flakes(GraphicsDevice device, int size = 128)
    {
        var pixels = new Color[size * size];
        var random = new Random(3);

        for (var i = 0; i < pixels.Length; i++)
        {
            var n = Vector3.Normalize(new Vector3(((float)random.NextDouble() - 0.5f) * 0.6f, ((float)random.NextDouble() - 0.5f) * 0.6f, 1f));

            pixels[i] = new Color(n.X * 0.5f + 0.5f, n.Y * 0.5f + 0.5f, n.Z * 0.5f + 0.5f);
        }

        return Texture.New2D(device, size, size, PixelFormat.R8G8B8A8_UNorm, pixels);
    }

    /// <summary>A grid of round holes: white where the surface stays, black where a cutoff removes it.</summary>
    public static Texture Holes(GraphicsDevice device, int size = 256, int across = 6, float radius = 0.3f)
    {
        var pixels = new Color[size * size];
        var cell = size / (float)across;

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var u = (x % cell) / cell - 0.5f;
                var v = (y % cell) / cell - 0.5f;

                pixels[y * size + x] = Grey(u * u + v * v < radius * radius ? 0f : 1f);
            }
        }

        return Texture.New2D(device, size, size, PixelFormat.R8G8B8A8_UNorm, pixels);
    }

    /// <summary>Streaks along X, as a brushed metal's glossiness varies: 0.55 to 0.9, mostly bright.</summary>
    public static Texture Brushed(GraphicsDevice device, int size = 256)
    {
        var pixels = new Color[size * size];
        var random = new Random(23);

        // Each row is one streak: a value of its own, then a little noise along it
        for (var y = 0; y < size; y++)
        {
            var streak = 0.6f + 0.3f * (float)random.NextDouble();

            for (var x = 0; x < size; x++)
            {
                pixels[y * size + x] = Grey(streak + 0.05f * ((float)random.NextDouble() - 0.5f));
            }
        }

        return Texture.New2D(device, size, size, PixelFormat.R8G8B8A8_UNorm, pixels);
    }

    /// <summary>Grey noise, 0 to 1 per texel: what a per-strand jitter reads from.</summary>
    public static Texture Noise(GraphicsDevice device, int size = 128)
    {
        var pixels = new Color[size * size];
        var random = new Random(17);

        for (var i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Grey((float)random.NextDouble());
        }

        return Texture.New2D(device, size, size, PixelFormat.R8G8B8A8_UNorm, pixels);
    }

    private static Color Grey(float value)
    {
        var v = Math.Clamp(value, 0f, 1f);

        return new Color(v, v, v);
    }
}