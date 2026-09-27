using Stride.CommunityToolkit.Rendering;
using Stride.Graphics;

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