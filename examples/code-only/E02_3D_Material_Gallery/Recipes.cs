using Stride.CommunityToolkit.Rendering;
using Stride.Core.Mathematics;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;

namespace E02_3D_Material_Gallery;

/// <summary>
/// The descriptors the stations are built from. A material is a bag of features; these are the
/// three bags that come up again and again - the four numbers, the specular workflow, and the
/// mapped PBR material where every slot may be a texture - so a station reads as what it changes.
/// </summary>
public static class Recipes
{
    /// <summary>
    /// The environment term is what a metal is made of - the sky in its reflection. The default
    /// GGX LUT variant reads a lookup texture from the content database, which a code-only game
    /// does not have, so metals come out black; the polynomial fit needs nothing.
    /// </summary>
    public static MaterialSpecularMicrofacetModelFeature Microfacet(IMaterialSpecularMicrofacetNormalDistributionFunction? distribution = null) => new()
    {
        NormalDistribution = distribution ?? new MaterialSpecularMicrofacetNormalDistributionGGX(),
        Environment = new MaterialSpecularMicrofacetEnvironmentGGXPolynomial(),
    };

    /// <summary>
    /// The metalness workflow in one descriptor: a colour, a glossiness, a metalness and the
    /// microfacet specular model - the four numbers of a PBR material, and what
    /// <c>game.CreateMaterial</c> builds.
    /// </summary>
    public static MaterialDescriptor Pbr(Color colour, float glossiness, float metalness, IMaterialSpecularMicrofacetNormalDistributionFunction? distribution = null)
    {
        // The toolkit's own bag, with the distribution swapped when a station asks for one
        var descriptor = MaterialDescriptors.Pbr(colour, metalness, glossiness);

        if (distribution is not null) descriptor.Attributes.SpecularModel = Microfacet(distribution);

        return descriptor;
    }

    /// <summary>The specular workflow: a diffuse colour and a specular colour, no metalness.</summary>
    public static MaterialDescriptor SpecularWorkflow(Color diffuse, Color specular, float glossiness) => new()
    {
        Attributes =
        {
            Diffuse = new MaterialDiffuseMapFeature(new ComputeColor(diffuse)),
            DiffuseModel = new MaterialDiffuseLambertModelFeature(),
            MicroSurface = new MaterialGlossinessMapFeature(new ComputeFloat(glossiness)),
            Specular = new MaterialSpecularMapFeature { SpecularMap = new ComputeColor(specular) },
            SpecularModel = Microfacet(),
        },
    };

    /// <summary>
    /// A mapped PBR material: every slot a node, so a slot can be a colour, a texture, a vertex
    /// stream or an expression. What is left out is left out of the shader too - no normal map
    /// means no tangent-space work at all.
    /// </summary>
    /// <param name="diffuse">The colour, or the albedo texture.</param>
    /// <param name="glossiness">The glossiness, a number or a map; null for the engine's default.</param>
    /// <param name="metalness">The metalness, a number or a map; leave null when <paramref name="specular"/> is given.</param>
    /// <param name="specular">A specular colour or map instead of a metalness.</param>
    /// <param name="normal">A normal map, in the pack's XY convention.</param>
    /// <param name="occlusion">An ambient occlusion map.</param>
    /// <param name="emissive">An emissive colour or map, at <paramref name="emissiveIntensity"/>.</param>
    public static MaterialDescriptor Mapped(
        IComputeColor diffuse,
        IComputeScalar? glossiness = null,
        IComputeScalar? metalness = null,
        IComputeColor? specular = null,
        IComputeColor? normal = null,
        IComputeScalar? occlusion = null,
        IComputeColor? emissive = null,
        float emissiveIntensity = 1f)
    {
        var descriptor = new MaterialDescriptor
        {
            Attributes =
            {
                Diffuse = new MaterialDiffuseMapFeature(diffuse),
                DiffuseModel = new MaterialDiffuseLambertModelFeature(),
                MicroSurface = new MaterialGlossinessMapFeature(glossiness ?? new ComputeFloat(0.65f)),
                SpecularModel = Microfacet(),
            },
        };

        descriptor.Attributes.Specular = specular is not null
            ? new MaterialSpecularMapFeature { SpecularMap = specular }
            : new MaterialMetalnessMapFeature(metalness ?? new ComputeFloat(0f));

        // The pack's normal maps store X and Y and leave Z to be rebuilt, in the 0..1 range a
        // texture holds, so both switches are on
        if (normal is not null) descriptor.Attributes.Surface = new MaterialNormalMapFeature(normal) { ScaleAndBias = true, IsXYNormal = true };

        if (occlusion is not null) descriptor.Attributes.Occlusion = new MaterialOcclusionMapFeature { AmbientOcclusionMap = occlusion, DirectLightingFactor = new ComputeFloat(0f) };

        if (emissive is not null) descriptor.Attributes.Emissive = new MaterialEmissiveMapFeature(emissive) { Intensity = new ComputeFloat(emissiveIntensity) };

        return descriptor;
    }

    /// <summary>A colour texture as a diffuse or emissive input, tiled <paramref name="tiling"/> times across the shape.</summary>
    public static ComputeTextureColor Colour(Stride.Graphics.Texture texture, float tiling = 1f)
        => new(texture, TextureCoordinate.Texcoord0, new Vector2(tiling), Vector2.Zero);

    /// <summary>A data texture as a scalar input - gloss, metalness, occlusion, a mask - tiled <paramref name="tiling"/> times.</summary>
    public static ComputeTextureScalar Scalar(Stride.Graphics.Texture texture, float tiling = 1f)
        => new(texture, TextureCoordinate.Texcoord0, new Vector2(tiling), Vector2.Zero);
}