namespace Stride.CommunityToolkit.Rendering;

/// <summary>
/// How a <see cref="TextureLoader"/> prepares a texture: its role, and the steps the content pipeline
/// takes at import that a runtime load has to take by hand. The defaults are the pipeline's, except
/// the green flip of a normal map, which the engine's own convention does not need.
/// </summary>
/// <param name="Role">What the texture is for.</param>
public sealed record TextureLoadOptions(TextureRole Role)
{
    /// <summary>
    /// For a <see cref="TextureRole.Color"/> texture, whether to premultiply its colour by its alpha, in
    /// linear light. On by default, as in a colour texture asset. Turn it off only for a texture whose
    /// pixels are premultiplied already. Ignored for the other roles.
    /// </summary>
    public bool PremultiplyAlpha { get; init; } = true;

    /// <summary>
    /// For a <see cref="TextureRole.NormalMap"/>, whether to invert the green channel. Off by default: the
    /// engine's tangent space has green pointing down the texture, the DirectX convention, which is how
    /// the Material Package's maps are stored. Turn it on for a map whose green points up, the OpenGL
    /// convention of Blender, Unity and glTF; the sign is wrong when bumps light as dents. Ignored for the
    /// other roles.
    /// </summary>
    /// <remarks>
    /// Game Studio's normal-map import inverts green by default, which suits a green-up map and turns a
    /// green-down one, such as the Material Package's own, upside down.
    /// </remarks>
    public bool InvertY { get; init; }

    /// <summary>
    /// Whether to build the full mipmap chain on the CPU. On by default. Without mipmaps a texture seen
    /// small or at a grazing angle shimmers, since each screen pixel samples one texel of many; the engine
    /// builds no mipmaps at runtime, and a PNG carries none.
    /// </summary>
    public bool GenerateMipmaps { get; init; } = true;
}