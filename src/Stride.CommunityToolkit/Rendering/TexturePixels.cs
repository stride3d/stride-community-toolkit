namespace Stride.CommunityToolkit.Rendering;

/// <summary>
/// The pixel work a <see cref="TextureLoader"/> does between decoding a file and creating the texture,
/// on plain RGBA arrays: the sRGB transfer function, premultiplication, the green flip of a normal map,
/// and a mipmap chain filtered the right way for each role. Public so a texture made in code can take
/// the same steps before <c>Texture.New2D</c>.
/// </summary>
public static class TexturePixels
{
    private static readonly float[] _srgbToLinear = BuildSrgbTable();

    /// <summary>Decodes one sRGB channel value to linear light, 0 to 1.</summary>
    /// <param name="value">The stored byte.</param>
    public static float SrgbToLinear(byte value) => _srgbToLinear[value];

    /// <summary>Encodes a linear value, clamped to 0 to 1, as an sRGB channel byte.</summary>
    /// <param name="value">The linear value.</param>
    public static byte LinearToSrgb(float value)
    {
        value = Math.Clamp(value, 0f, 1f);

        var encoded = value <= 0.0031308f ? value * 12.92f : 1.055f * MathF.Pow(value, 1f / 2.4f) - 0.055f;

        return ToByte(encoded);
    }

    /// <summary>Swaps the red and blue channels in place: the engine's decoder hands back BGRA, a texture made from <see cref="Color"/> is RGBA.</summary>
    /// <param name="pixels">The pixels.</param>
    public static void SwapRedBlue(Span<Color> pixels)
    {
        foreach (ref var pixel in pixels)
        {
            (pixel.R, pixel.B) = (pixel.B, pixel.R);
        }
    }

    /// <summary>Inverts the green channel in place: a normal map's Y from one convention to the other.</summary>
    /// <param name="pixels">The pixels.</param>
    public static void InvertGreen(Span<Color> pixels)
    {
        foreach (ref var pixel in pixels)
        {
            pixel.G = (byte)(byte.MaxValue - pixel.G);
        }
    }

    /// <summary>
    /// Premultiplies sRGB-encoded colours by their alpha, in linear light, in place: decoded, scaled,
    /// encoded again. An opaque pixel is left exactly as it was; a transparent one becomes black, whatever
    /// colour the file kept under it.
    /// </summary>
    /// <param name="pixels">The pixels, sRGB, straight alpha.</param>
    /// <remarks>
    /// The content pipeline scales the stored bytes instead, which is the same for an opaque pixel and
    /// slightly darker at a half-transparent edge; scaling in linear light is what the GPU undoes when it
    /// decodes the sRGB texture, so the edge blends to exactly the colour the file meant.
    /// </remarks>
    public static void PremultiplyAlpha(Span<Color> pixels)
    {
        foreach (ref var pixel in pixels)
        {
            if (pixel.A == byte.MaxValue) continue;

            if (pixel.A == 0)
            {
                pixel = default;
                continue;
            }

            var alpha = pixel.A / 255f;

            pixel.R = LinearToSrgb(SrgbToLinear(pixel.R) * alpha);
            pixel.G = LinearToSrgb(SrgbToLinear(pixel.G) * alpha);
            pixel.B = LinearToSrgb(SrgbToLinear(pixel.B) * alpha);
        }
    }

    /// <summary>How many levels a full mipmap chain of a texture this size has, down to one texel.</summary>
    /// <param name="width">The width of the largest level.</param>
    /// <param name="height">The height of the largest level.</param>
    public static int MipCount(int width, int height) => 1 + (int)MathF.Floor(MathF.Log2(Math.Max(Math.Max(width, height), 1)));

    /// <summary>
    /// Builds the mipmap levels below a prepared base level, each half the size of the one above, by
    /// averaging two by two texels in the space that is right for the role: linear light for a colour,
    /// the stored values for data, unit vectors for a normal map.
    /// </summary>
    /// <param name="baseLevel">The largest level, prepared for its role: premultiplied, flipped.</param>
    /// <param name="width">Its width.</param>
    /// <param name="height">Its height.</param>
    /// <param name="role">What the texture is for.</param>
    /// <returns>Every level including the base, largest first.</returns>
    /// <remarks>
    /// Averaging sRGB bytes directly, the easy way, darkens every mipmap of a contrasted colour texture: black
    /// and white average to a byte of 128, which the GPU decodes as 0.22, not 0.5. A normal map averaged
    /// without renormalising grows shorter and flatter in the distance. An odd size repeats its last row
    /// or column, so the chain works for any size, power of two or not.
    /// </remarks>
    public static IReadOnlyList<Color[]> BuildMipChain(Color[] baseLevel, int width, int height, TextureRole role)
    {
        ArgumentNullException.ThrowIfNull(baseLevel);

        if (baseLevel.Length != width * height) throw new ArgumentException($"Expected {width * height} pixels for {width} by {height}, got {baseLevel.Length}", nameof(baseLevel));

        var levels = new List<Color[]>(MipCount(width, height)) { baseLevel };
        var current = new Vector4[baseLevel.Length];

        for (var i = 0; i < baseLevel.Length; i++) current[i] = Decode(baseLevel[i], role);

        while (width > 1 || height > 1)
        {
            var nextWidth = Math.Max(1, width / 2);
            var nextHeight = Math.Max(1, height / 2);
            var next = new Vector4[nextWidth * nextHeight];

            for (var y = 0; y < nextHeight; y++)
            {
                var y0 = Math.Min(2 * y, height - 1);
                var y1 = Math.Min(2 * y + 1, height - 1);

                for (var x = 0; x < nextWidth; x++)
                {
                    var x0 = Math.Min(2 * x, width - 1);
                    var x1 = Math.Min(2 * x + 1, width - 1);

                    next[y * nextWidth + x] = 0.25f * (current[y0 * width + x0] + current[y0 * width + x1] + current[y1 * width + x0] + current[y1 * width + x1]);
                }
            }

            var pixels = new Color[next.Length];

            for (var i = 0; i < next.Length; i++)
            {
                if (role == TextureRole.NormalMap) next[i] = Renormalise(next[i]);

                pixels[i] = Encode(next[i], role);
            }

            levels.Add(pixels);
            current = next;
            width = nextWidth;
            height = nextHeight;
        }

        return levels;
    }

    private static Vector4 Decode(Color pixel, TextureRole role) => role switch
    {
        TextureRole.Color => new(SrgbToLinear(pixel.R), SrgbToLinear(pixel.G), SrgbToLinear(pixel.B), pixel.A / 255f),
        TextureRole.NormalMap => new(pixel.R / 127.5f - 1f, pixel.G / 127.5f - 1f, pixel.B / 127.5f - 1f, pixel.A / 255f),
        _ => new(pixel.R / 255f, pixel.G / 255f, pixel.B / 255f, pixel.A / 255f),
    };

    private static Color Encode(Vector4 value, TextureRole role) => role switch
    {
        TextureRole.Color => new Color(LinearToSrgb(value.X), LinearToSrgb(value.Y), LinearToSrgb(value.Z), ToByte(value.W)),
        TextureRole.NormalMap => new Color(ToByte(value.X * 0.5f + 0.5f), ToByte(value.Y * 0.5f + 0.5f), ToByte(value.Z * 0.5f + 0.5f), ToByte(value.W)),
        _ => new Color(ToByte(value.X), ToByte(value.Y), ToByte(value.Z), ToByte(value.W)),
    };

    // The average of opposite normals is zero, with no direction to keep; that texel stays as it is
    private static Vector4 Renormalise(Vector4 value)
    {
        var normal = new Vector3(value.X, value.Y, value.Z);
        var length = normal.Length();

        return length > 1e-6f ? new Vector4(normal / length, value.W) : value;
    }

    private static byte ToByte(float value) => (byte)MathF.Round(Math.Clamp(value, 0f, 1f) * 255f);

    private static float[] BuildSrgbTable()
    {
        var table = new float[256];

        for (var i = 0; i < table.Length; i++)
        {
            var encoded = i / 255f;

            table[i] = encoded <= 0.04045f ? encoded / 12.92f : MathF.Pow((encoded + 0.055f) / 1.055f, 2.4f);
        }

        return table;
    }
}