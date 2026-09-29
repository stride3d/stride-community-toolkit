using Stride.CommunityToolkit.Rendering;
using Stride.Graphics;
using Pixel = Stride.Core.Mathematics.Color;

namespace E02_3D_Material_Gallery;

/// <summary>
/// The Material Package's textures, loaded from the resources folder next to the executable on
/// first use and kept for the run, through the toolkit's <see cref="TextureLoader"/>: a colour map
/// sRGB and premultiplied, a data map linear, a normal map linear with unit normals in every mip, each
/// with its mipmaps. The one thing that goes wrong when every texture is
/// loaded the same way is the role, so each accessor names one.
/// </summary>
public sealed class MaterialTextures(GraphicsDevice device) : IDisposable
{
    private readonly Dictionary<string, Texture> _generated = [];

    /// <summary>The loader behind the accessors, rooted at <c>Resources/materials</c>; the texture-loading station asks it for the wrong roles on purpose.</summary>
    public TextureLoader Loader { get; } = new(device, Path.Combine(AppContext.BaseDirectory, "Resources", "materials"));

    /// <summary>A colour map: sRGB, premultiplied.</summary>
    /// <param name="path">The file under <c>Resources/materials</c>, such as <c>brick/brick_dif.png</c>.</param>
    public Texture Color(string path) => Loader.Color(path);

    /// <summary>A data map - gloss, metalness, occlusion, specular, mask - kept linear.</summary>
    /// <param name="path">The file under <c>Resources/materials</c>, such as <c>brick/brick_gls.png</c>.</param>
    public Texture Data(string path) => Loader.Data(path);

    /// <summary>A normal map: linear, as stored - the pack's maps are green-down, the engine's own convention.</summary>
    /// <param name="path">The file under <c>Resources/materials</c>, such as <c>brick/brick_nml.png</c>.</param>
    public Texture Normal(string path) => Loader.NormalMap(path);

    /// <summary>
    /// Several greyscale maps packed into one data texture: the first in its red channel, the second
    /// in green, the third in blue. Made on first use and kept for the run.
    /// </summary>
    /// <param name="name">Its name, the key it is kept under.</param>
    /// <param name="maps">The files under <c>Resources/materials</c>, up to three, all of one size.</param>
    public Texture Packed(string name, params string[] maps) => Generated(name, () =>
    {
        Pixel[]? packed = null;
        var (width, height) = (0, 0);

        for (var channel = 0; channel < maps.Length; channel++)
        {
            var values = ReadGreyscale(maps[channel], out width, out height);

            packed ??= [.. Enumerable.Repeat(Pixel.Black, values.Length)];

            for (var i = 0; i < values.Length; i++)
            {
                packed[i][channel] = values[i];
            }
        }

        return TextureLoader.FromPixels(device, packed!, width, height, new TextureLoadOptions(TextureRole.Data));
    });

    /// <summary>
    /// A greyscale map with more contrast: every value raised to a power, so the light greys stay
    /// light and the darker ones go dark. Made on first use and kept for the run.
    /// </summary>
    /// <param name="name">Its name, the key it is kept under.</param>
    /// <param name="map">The file under <c>Resources/materials</c>.</param>
    /// <param name="power">The power. 1 leaves the map as it is.</param>
    public Texture Deepened(string name, string map, float power) => Generated(name, () =>
    {
        var values = ReadGreyscale(map, out var width, out var height);
        var pixels = new Pixel[values.Length];

        for (var i = 0; i < values.Length; i++)
        {
            var value = (byte)MathF.Round(MathF.Pow(values[i] / 255f, power) * 255f);

            pixels[i] = new Pixel(value, value, value, byte.MaxValue);
        }

        return TextureLoader.FromPixels(device, pixels, width, height, new TextureLoadOptions(TextureRole.Data));
    });

    /// <summary>Reads a greyscale map as one byte per pixel.</summary>
    private static byte[] ReadGreyscale(string map, out int width, out int height)
    {
        using var stream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "Resources", "materials", map));
        using var image = Image.Load(stream);

        (width, height) = (image.Description.Width, image.Description.Height);

        // A greyscale map has the same value in every channel; green is the same in RGBA and BGRA
        return [.. image.PixelBuffer[0].GetPixels<Pixel>().Select(pixel => pixel.G)];
    }

    /// <summary>A texture computed in code, made on first use and kept for the run.</summary>
    /// <param name="name">Its name, the key it is kept under.</param>
    /// <param name="make">How to make it.</param>
    public Texture Generated(string name, Func<Texture> make)
    {
        if (!_generated.TryGetValue(name, out var texture))
        {
            texture = make();
            _generated[name] = texture;
        }

        return texture;
    }

    public void Dispose()
    {
        foreach (var texture in _generated.Values) texture.Dispose();

        _generated.Clear();
        Loader.Dispose();
    }
}