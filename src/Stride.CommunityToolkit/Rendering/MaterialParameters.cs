using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Materials;

namespace Stride.CommunityToolkit.Rendering;

/// <summary>
/// Changes a compiled material at runtime through its parameters, the way the engine's own gizmo
/// materials do: no rebuild, the value is picked up by the next draw.
/// </summary>
/// <remarks>
/// <para>
/// Once <c>Material.New</c> has run, the features are gone; what remains is a shader and the constants
/// the features registered. A number in a slot registers under the slot's key (<see cref="MaterialKeys.GlossinessValue"/>,
/// <see cref="MaterialKeys.EmissiveIntensity"/>); a colour or texture node can carry a key of your own
/// (<c>new ComputeColor(Color.White) { Key = MaterialParameters.ColorKey("MyGame.Tint") }</c>), which is
/// how a value is animated without touching the shader. Every texture node also registers its scale and
/// offset, so a texture scrolls through <see cref="SetTextureOffset"/> with no recompile.
/// </para>
/// <para>
/// Two things the generator did at compile time have to be repeated for a colour set at runtime, or the
/// two disagree: it converted the colour from sRGB to linear and premultiplied it by its alpha.
/// <see cref="SetColor(Material, ValueParameterKey{Color4}, Color4, bool)"/> does both. And a material may
/// have several passes (clear coat two, hair three, thin glass two or four), each with its own
/// parameters, so every setter here writes to all of them.
/// </para>
/// </remarks>
public static class MaterialParameters
{
    /// <summary>A colour key of your own for a <c>ComputeColor</c> node, named so it reads in a debugger.</summary>
    /// <param name="name">A unique name, such as <c>"MyGame.Tint"</c>.</param>
    public static ValueParameterKey<Color4> ColorKey(string name) => ParameterKeys.NewValue<Color4>(default, name);

    /// <summary>A number key of your own for a <c>ComputeFloat</c> node.</summary>
    /// <param name="name">A unique name, such as <c>"MyGame.Wear"</c>.</param>
    public static ValueParameterKey<float> FloatKey(string name) => ParameterKeys.NewValue<float>(default, name);

    /// <summary>A texture key of your own for a <c>ComputeTextureColor</c> or <c>ComputeTextureScalar</c> node.</summary>
    /// <param name="name">A unique name, such as <c>"MyGame.Screen"</c>.</param>
    public static ObjectParameterKey<Texture> TextureKey(string name) => ParameterKeys.NewObject<Texture>(name: name);

    /// <summary>Sets a value parameter on every pass of the material.</summary>
    public static void Set<T>(this Material material, ValueParameterKey<T> key, T value) where T : struct
    {
        foreach (var pass in material.Passes)
        {
            pass.Parameters.Set(key, value);
        }
    }

    /// <summary>Sets an object parameter - a texture, a sampler - on every pass of the material.</summary>
    public static void Set<T>(this Material material, ObjectParameterKey<T> key, T value) where T : class
    {
        foreach (var pass in material.Passes)
        {
            pass.Parameters.Set(key, value);
        }
    }

    /// <summary>
    /// Sets a colour parameter on every pass, converted the way the generator converted the node's
    /// initial value: to linear, and premultiplied by its alpha unless <paramref name="premultiply"/> is off.
    /// </summary>
    /// <param name="material">The material.</param>
    /// <param name="key">The colour's key: one of <see cref="MaterialKeys"/>, or the key the node was given.</param>
    /// <param name="colour">The colour, in sRGB, as you would write it.</param>
    /// <param name="premultiply">Whether to premultiply by alpha; the node's default is to.</param>
    public static void SetColor(this Material material, ValueParameterKey<Color4> key, Color4 colour, bool premultiply = true)
        => material.Set(key, ToMaterialValue(colour, premultiply));

    /// <summary>Sets a colour parameter on every pass, from a byte colour, converted like the generator would.</summary>
    public static void SetColor(this Material material, ValueParameterKey<Color4> key, Color colour, bool premultiply = true)
        => material.SetColor(key, new Color4(colour), premultiply);

    /// <summary>
    /// What a <c>ComputeColor</c> node's value becomes in the material's constants: linear, and
    /// premultiplied by its alpha. Setting the raw sRGB colour instead makes the runtime value brighter
    /// than the one compiled in.
    /// </summary>
    /// <param name="colour">The colour, in sRGB.</param>
    /// <param name="premultiply">Whether to premultiply by alpha.</param>
    public static Color4 ToMaterialValue(Color4 colour, bool premultiply = true)
    {
        var linear = colour.ToColorSpace(ColorSpace.Linear);

        return premultiply ? Color4.PremultiplyAlpha(linear) : linear;
    }

    /// <summary>
    /// Sets the tiling of a texture node on every pass: how many times it repeats across the UV range.
    /// The diffuse texture is the material's first, the others follow in the order the generator
    /// visited the slots.
    /// </summary>
    /// <param name="material">The material.</param>
    /// <param name="scale">The tiling; the node's default is one.</param>
    /// <param name="textureIndex">Which texture node, 0 for the first.</param>
    public static void SetTextureScale(this Material material, Vector2 scale, int textureIndex = 0)
        => material.Set(TextureKeyAt(MaterialKeys.TextureScale, textureIndex), scale);

    /// <summary>
    /// Sets the offset of a texture node on every pass, in UV units: raise it every frame and the
    /// texture scrolls, with no recompile.
    /// </summary>
    /// <param name="material">The material.</param>
    /// <param name="offset">The offset, in UV units.</param>
    /// <param name="textureIndex">Which texture node, 0 for the first.</param>
    public static void SetTextureOffset(this Material material, Vector2 offset, int textureIndex = 0)
        => material.Set(TextureKeyAt(MaterialKeys.TextureOffset, textureIndex), offset);

    /// <summary>
    /// The key a texture node's scale or offset registered under: the base key for the first texture,
    /// then the base key composed with <c>i1</c>, <c>i2</c> and so on, which is how the generator
    /// names a key's later uses.
    /// </summary>
    /// <param name="key"><see cref="MaterialKeys.TextureScale"/> or <see cref="MaterialKeys.TextureOffset"/>.</param>
    /// <param name="textureIndex">Which texture node, 0 for the first.</param>
    public static ValueParameterKey<Vector2> TextureKeyAt(ValueParameterKey<Vector2> key, int textureIndex)
        => textureIndex == 0 ? key : key.ComposeWith($"i{textureIndex}");
}