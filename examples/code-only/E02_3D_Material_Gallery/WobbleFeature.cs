using Stride.Core;
using Stride.Rendering.Materials;
using Stride.Shaders;

namespace E02_3D_Material_Gallery;

/// <summary>
/// A material feature of the gallery's own in the displacement slot, the one slot that runs in the
/// vertex stage: every vertex moves along its normal by a wave running up the shape
/// (<c>Effects/GalleryWobble.sdsl</c>). The whole of a feature is here: a class with properties, and a
/// <see cref="GenerateShader"/> that tells the material generator which shader to add to which stage
/// and what its parameters start as.
/// </summary>
/// <remarks>
/// The vertex normals are left as they were, so the lighting still describes the undeformed shape; for
/// a wobble this small the eye does not notice, for a large one the shader would bend the normal too.
/// On a shape with split normals - a cube's corners - the faces move apart and the seams open.
/// </remarks>
[DataContract]
public class WobbleFeature : MaterialFeature, IMaterialDisplacementFeature
{
    /// <summary>How far a vertex moves along its normal at the crest of the wave, in object units.</summary>
    public float Amplitude { get; set; } = 0.08f;

    /// <summary>How fast the wave's phase changes with height, in radians per object unit.</summary>
    public float Frequency { get; set; } = 7f;

    /// <summary>How fast the wave runs up the shape, in radians per second.</summary>
    public float Speed { get; set; } = 4f;

    /// <inheritdoc/>
    public override void GenerateShader(MaterialGeneratorContext context)
    {
        // The stage's final modifier, the hook the engine's own displacement feature uses: it runs after
        // every other vertex-stage shader the material has, on the object-space position
        context.SetStreamFinalModifier<WobbleFeature>(MaterialShaderStage.Vertex, new ShaderClassSource("GalleryWobble"));

        // The properties become the pass's parameters, through the keys the shader source generator made
        // from the shader's cbuffer. A multi-pass material generates once per pass, so each pass gets them
        var parameters = context.MaterialPass.Parameters;

        parameters.Set(GalleryWobbleKeys.WobbleAmplitude, Amplitude);
        parameters.Set(GalleryWobbleKeys.WobbleFrequency, Frequency);
        parameters.Set(GalleryWobbleKeys.WobbleSpeed, Speed);
    }
}