namespace Stride.CommunityToolkit.Rendering;

/// <summary>
/// What a texture is for, which decides how its pixels must be read: the same three choices Game
/// Studio offers as a texture asset's type. Load a texture in the wrong role and the material still
/// compiles and still draws, just wrongly, which is why the role is chosen once, at load.
/// </summary>
public enum TextureRole
{
    /// <summary>
    /// A colour: an albedo, an emissive, a sprite. Stored sRGB, so the GPU decodes it to linear when
    /// it samples, and premultiplied by its alpha, which is what the engine's blend states expect.
    /// </summary>
    Color,

    /// <summary>
    /// Numbers stored as pixels: glossiness, metalness, occlusion, a mask, a height map. Linear, never
    /// gamma-decoded, never premultiplied: a glossiness of 0.5 must sample as 0.5.
    /// </summary>
    Data,

    /// <summary>
    /// A tangent-space normal map: linear like data, its mipmaps renormalised so a distant surface keeps
    /// unit normals, its green channel inverted on request for a map whose green points up.
    /// </summary>
    NormalMap,
}