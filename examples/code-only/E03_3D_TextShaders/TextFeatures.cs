using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Rendering.Materials;
using Stride.Shaders;

namespace E03_3D_TextShaders;

/// <summary>
/// A material feature in the displacement slot, the one slot that runs in the vertex stage: every
/// vertex rises and falls by a wave running along the lettering (<c>Effects/TextWave.sdsl</c>).
/// The whole of a feature: a class with properties, and a <see cref="GenerateShader"/> that tells the
/// material generator which shader to add to which stage and what its parameters start as.
/// </summary>
/// <remarks>
/// The normals are left as they were, so the lighting still describes the still letters; for a wave this
/// small the eye does not notice. The lift depends on x alone, not on the normal, so a cap and its walls,
/// which do not share vertices, move together and the seams stay closed.
/// </remarks>
[DataContract]
public class WaveFeature : MaterialFeature, IMaterialDisplacementFeature
{
    /// <summary>How far a vertex rises at the crest, in object units.</summary>
    public float Amplitude { get; set; } = 0.04f;

    /// <summary>How fast the wave's phase changes along X, in radians per object unit.</summary>
    public float Frequency { get; set; } = 6f;

    /// <summary>How fast the wave runs along the lettering, in radians per second.</summary>
    public float Speed { get; set; } = 3f;

    /// <inheritdoc/>
    public override void GenerateShader(MaterialGeneratorContext context)
    {
        // The stage's final modifier, the hook the engine's own displacement feature uses: it runs after
        // every other vertex-stage shader the material has, on the object-space position
        context.SetStreamFinalModifier<WaveFeature>(MaterialShaderStage.Vertex, new ShaderClassSource("TextWave"));

        var parameters = context.MaterialPass.Parameters;

        parameters.Set(TextWaveKeys.WaveAmplitude, Amplitude);
        parameters.Set(TextWaveKeys.WaveFrequency, Frequency);
        parameters.Set(TextWaveKeys.WaveSpeed, Speed);
    }
}

/// <summary>
/// A material feature in the transparency slot, the pixel stage: noise over the texture coordinates
/// decides which pixels are gone, and a band next to the gone ones glows (<c>Effects/TextDissolve.sdsl</c>).
/// </summary>
/// <remarks>
/// The slot matters: the transparency slot is visited after the emissive one, so the shader added here
/// runs after the emissive feature's and can write the edge into the emissive stream. And a discard
/// must run in the shadow caster too, or the dissolved part still casts a shadow, which
/// <see cref="MaterialKeys.UsePixelShaderWithDepthPass"/> asks for.
/// </remarks>
[DataContract]
public class DissolveFeature : MaterialFeature, IMaterialTransparencyFeature
{
    /// <summary>How much has dissolved: 0 nothing, 1 everything. The example drives it every frame.</summary>
    public float Amount { get; set; } = 0.4f;

    /// <summary>How wide the glowing band is, in noise units.</summary>
    public float EdgeWidth { get; set; } = 0.12f;

    /// <summary>How many noise cells fit across the texture coordinates.</summary>
    public float Scale { get; set; } = 9f;

    /// <summary>The edge's colour, in linear light; above one to glow through the bloom.</summary>
    public Vector3 EdgeColor { get; set; } = new(24f, 6f, 0.8f);

    /// <inheritdoc/>
    public override void GenerateShader(MaterialGeneratorContext context)
    {
        context.AddShaderSource(MaterialShaderStage.Pixel, new ShaderClassSource("TextDissolve"));

        var parameters = context.MaterialPass.Parameters;

        parameters.Set(MaterialKeys.UsePixelShaderWithDepthPass, true);
        parameters.Set(TextDissolveKeys.DissolveAmount, Amount);
        parameters.Set(TextDissolveKeys.DissolveEdgeWidth, EdgeWidth);
        parameters.Set(TextDissolveKeys.DissolveScale, Scale);
        parameters.Set(TextDissolveKeys.DissolveEdgeColor, EdgeColor);
    }
}