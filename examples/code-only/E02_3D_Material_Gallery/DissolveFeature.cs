using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Rendering.Materials;
using Stride.Shaders;

namespace E02_3D_Material_Gallery;

/// <summary>
/// A material feature of the gallery's own in the transparency slot: the surface burns away through
/// fractal noise, with a hot edge where it is about to go (<c>Effects/GalleryDissolve.sdsl</c>). The
/// amount is a parameter, so the station drives it every frame through <c>MaterialParameters</c> with
/// no rebuild; the rest are set once from the properties.
/// </summary>
/// <remarks>
/// Two things make it more than a shader. The slot: the transparency slot is visited after the emissive
/// one, so the shader added here runs after the emissive feature's and can write the edge into the
/// emissive stream; in the surface slot it would run first and be overwritten. And the depth pass:
/// a discard must run in the shadow caster too, or the dissolved part still casts a shadow, and
/// <see cref="MaterialKeys.UsePixelShaderWithDepthPass"/> is what the engine's cutoff feature sets for it.
/// </remarks>
[DataContract]
public class DissolveFeature : MaterialFeature, IMaterialTransparencyFeature
{
    /// <summary>How much has dissolved: 0 nothing, 1 everything.</summary>
    public float Amount { get; set; } = 0.4f;

    /// <summary>How wide the glowing band is, in noise units.</summary>
    public float EdgeWidth { get; set; } = 0.1f;

    /// <summary>How many noise cells fit across the texture coordinates.</summary>
    public float Scale { get; set; } = 7f;

    /// <summary>The edge's colour, in linear light; above one to glow through the bloom.</summary>
    public Vector3 EdgeColor { get; set; } = new(24f, 6f, 0.8f);

    /// <inheritdoc/>
    public override void GenerateShader(MaterialGeneratorContext context)
    {
        context.AddShaderSource(MaterialShaderStage.Pixel, new ShaderClassSource("GalleryDissolve"));

        var parameters = context.MaterialPass.Parameters;

        parameters.Set(MaterialKeys.UsePixelShaderWithDepthPass, true);
        parameters.Set(GalleryDissolveKeys.DissolveAmount, Amount);
        parameters.Set(GalleryDissolveKeys.DissolveEdgeWidth, EdgeWidth);
        parameters.Set(GalleryDissolveKeys.DissolveScale, Scale);
        parameters.Set(GalleryDissolveKeys.DissolveEdgeColor, EdgeColor);
    }
}