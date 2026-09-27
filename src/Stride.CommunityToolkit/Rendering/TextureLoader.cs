using Stride.Graphics;

namespace Stride.CommunityToolkit.Rendering;

/// <summary>
/// Loads image files into textures the way Game Studio's content pipeline would have prepared them,
/// by what the texture is for: a colour as sRGB and premultiplied, data as linear, a normal map linear
/// with unit normals in every mip, each with a full mipmap chain. Each file is loaded once per role and
/// kept until the loader is disposed.
/// </summary>
/// <remarks>
/// <para>
/// <c>Texture.Load(device, stream)</c> with its defaults does none of this: it loads every file as
/// linear, straight alpha, one mip. A colour texture comes out pale, a transparent edge gets a halo, a
/// tiled floor shimmers, and nothing warns. The pipeline does the rest at import, which is why the same
/// PNG looks right as an asset and wrong from code.
/// </para>
/// <para>
/// What the loader does not do is block compression: the texture stays eight bits per channel,
/// four to eight times the memory of the BC formats an asset would use. For that, or for mipmaps built
/// offline, ship a <c>.dds</c>: a path ending in <c>.dds</c> is loaded as it is, only its colour space
/// chosen by the role.
/// </para>
/// </remarks>
/// <param name="device">The device the textures are created on.</param>
/// <param name="root">The folder relative paths are resolved against; the executable's folder by default.</param>
public sealed class TextureLoader(GraphicsDevice device, string? root = null) : IDisposable
{
    private readonly Dictionary<(string Path, TextureLoadOptions Options), Texture> _loaded = [];

    /// <summary>The folder relative paths are resolved against.</summary>
    public string Root { get; } = root ?? AppContext.BaseDirectory;

    /// <summary>A colour texture: an albedo, an emissive, a sprite. sRGB, premultiplied, mipmapped.</summary>
    /// <param name="path">The file, absolute or relative to <see cref="Root"/>.</param>
    public Texture Color(string path) => Load(path, new TextureLoadOptions(TextureRole.Color));

    /// <summary>A data texture: glossiness, metalness, occlusion, a mask, a height map. Linear, mipmapped.</summary>
    /// <param name="path">The file, absolute or relative to <see cref="Root"/>.</param>
    public Texture Data(string path) => Load(path, new TextureLoadOptions(TextureRole.Data));

    /// <summary>A tangent-space normal map. Linear, mipmapped with unit normals.</summary>
    /// <param name="path">The file, absolute or relative to <see cref="Root"/>.</param>
    /// <param name="invertY">Whether to invert the green channel: on for a map whose green points up (Blender, Unity, glTF), off for the engine's own green-down convention.</param>
    public Texture NormalMap(string path, bool invertY = false) => Load(path, new TextureLoadOptions(TextureRole.NormalMap) { InvertY = invertY });

    /// <summary>A texture with every option spelled out. Loaded once per path and options, kept until the loader is disposed.</summary>
    /// <param name="path">The file, absolute or relative to <see cref="Root"/>.</param>
    /// <param name="options">The role and the steps to take.</param>
    public Texture Load(string path, TextureLoadOptions options)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(options);

        if (_loaded.TryGetValue((path, options), out var texture)) return texture;

        using var stream = File.OpenRead(Path.Combine(Root, path));

        texture = Path.GetExtension(path).Equals(".dds", StringComparison.OrdinalIgnoreCase)
            ? Texture.Load(device, stream, loadAsSrgb: options.Role == TextureRole.Color)
            : Load(device, stream, options);

        _loaded[(path, options)] = texture;

        return texture;
    }

    /// <summary>Loads a texture from a stream - an embedded resource, a download - prepared for its role. Not kept: the caller owns it.</summary>
    /// <param name="device">The device to create it on.</param>
    /// <param name="stream">A PNG, JPEG, BMP, GIF or TIFF.</param>
    /// <param name="options">The role and the steps to take.</param>
    public static Texture Load(GraphicsDevice device, Stream stream, TextureLoadOptions options)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var image = Image.Load(stream);

        return FromImage(device, image, options);
    }

    /// <summary>
    /// Makes a texture from a decoded image, prepared for its role: for pixels edited after decoding and
    /// before upload. Not kept: the caller owns it.
    /// </summary>
    /// <param name="device">The device to create it on.</param>
    /// <param name="image">A 2D image of eight bits per channel, RGBA or BGRA, as <c>Image.Load</c> decodes a PNG or JPEG.</param>
    /// <param name="options">The role and the steps to take.</param>
    /// <exception cref="NotSupportedException">The image is not a single 2D image of eight-bit RGBA or BGRA.</exception>
    public static Texture FromImage(GraphicsDevice device, Image image, TextureLoadOptions options)
    {
        ArgumentNullException.ThrowIfNull(device);
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(options);

        var description = image.Description;
        var format = description.Format.ToNonSRgb();

        if (description.Dimension != TextureDimension.Texture2D || description.ArraySize != 1 || format is not (PixelFormat.B8G8R8A8_UNorm or PixelFormat.R8G8B8A8_UNorm))
        {
            throw new NotSupportedException($"TextureLoader prepares single 2D images of 8-bit RGBA or BGRA; this one is {description.Dimension} {description.Format}, array size {description.ArraySize}. Load it with Texture.Load, or save it as a .dds with its mipmaps.");
        }

        var pixels = image.PixelBuffer[0].GetPixels<Color>();

        if (format == PixelFormat.B8G8R8A8_UNorm) TexturePixels.SwapRedBlue(pixels);

        if (options.Role == TextureRole.Color && options.PremultiplyAlpha) TexturePixels.PremultiplyAlpha(pixels);

        if (options.Role == TextureRole.NormalMap && options.InvertY) TexturePixels.InvertGreen(pixels);

        var levels = options.GenerateMipmaps ? TexturePixels.BuildMipChain(pixels, description.Width, description.Height, options.Role) : [pixels];
        var target = options.Role == TextureRole.Color ? PixelFormat.R8G8B8A8_UNorm_SRgb : PixelFormat.R8G8B8A8_UNorm;

        using var prepared = Image.New2D(description.Width, description.Height, levels.Count, target);

        for (var level = 0; level < levels.Count; level++)
        {
            prepared.GetPixelBuffer(0, level).SetPixels(levels[level]);
        }

        return Texture.New(device, prepared);
    }

    /// <summary>Disposes every texture the loader has loaded; they must no longer be drawn.</summary>
    public void Dispose()
    {
        foreach (var texture in _loaded.Values) texture.Dispose();

        _loaded.Clear();
    }
}