namespace Stride.CommunityToolkit.Rendering;

/// <summary>
/// The material streams a <see cref="MaterialStreamView"/> can draw in place of the lit colour: the
/// values the material's features wrote before lighting ran, which is what Game Studio's view modes
/// show. Every mesh in the view draws the same stream.
/// </summary>
public enum MaterialStream
{
    /// <summary>The diffuse colour, <c>matDiffuse</c>, after the metalness feature took its share: a metal shows black here.</summary>
    Diffuse,

    /// <summary>The colour as authored, <c>matColorBase</c>, before metalness split it between diffuse and specular.</summary>
    ColorBase,

    /// <summary>The specular colour, <c>matSpecular</c>: 0.02 grey for a dielectric, the colour itself for a metal.</summary>
    Specular,

    /// <summary>The glossiness, <c>matGlossiness</c>, as grey: black is rough, white is a mirror.</summary>
    Glossiness,

    /// <summary>The normal in tangent space, <c>matNormal</c>, remapped from -1..1 to a colour: a flat surface is the lilac of (0.5, 0.5, 1).</summary>
    NormalTangent,

    /// <summary>The normal in world space after the normal map, remapped to a colour: up is green, the viewer's right is red.</summary>
    NormalWorld,

    /// <summary>The ambient occlusion, <c>matAmbientOcclusion</c>, as grey: white is open, black is occluded.</summary>
    Occlusion,

    /// <summary>The cavity, <c>matCavity</c>, as grey: the crevices that darken direct light.</summary>
    Cavity,

    /// <summary>The emissive colour, <c>matEmissive</c>, without its intensity.</summary>
    Emissive,
}