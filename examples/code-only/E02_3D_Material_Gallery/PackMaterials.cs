using Stride.Core.Mathematics;
using Stride.Rendering;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;

namespace E02_3D_Material_Gallery;

/// <summary>
/// Game Studio's Material Package, transcribed from its <c>.sdmat</c> files: the same features
/// with the same settings, which is the proof that a material asset is a descriptor and nothing
/// more. A few of the pack's choices are worth noticing - a glossiness map that is the metalness
/// map plus a constant, a specular map at 5 percent intensity for a dielectric - because they show
/// how the pack's authors tuned by eye, in nodes.
/// </summary>
public static class PackMaterials
{
    public static MaterialDescriptor Brick(MaterialTextures textures) => new()
    {
        Attributes =
        {
            Surface = Normal(textures, "brick/brick_nml.png"),
            Diffuse = new MaterialDiffuseMapFeature(Recipes.Colour(textures.Color("brick/brick_dif.png"))),
            DiffuseModel = new MaterialDiffuseLambertModelFeature(),
            Specular = new MaterialSpecularMapFeature { SpecularMap = new ComputeColor(Color.White), Intensity = new ComputeFloat(0.05f) },
            SpecularModel = Recipes.Microfacet(),
            Occlusion = new MaterialOcclusionMapFeature { AmbientOcclusionMap = Recipes.Scalar(textures.Data("brick/brick_AO.png")) },
        },
    };

    public static MaterialDescriptor Gold(MaterialTextures textures) => Metal(textures, "gold/gold_dif.png", "gold/gold_mtl.png", glossinessOffset: 0.4f);

    public static MaterialDescriptor Silver(MaterialTextures textures) => Metal(textures, "silver/silver_dif.png", "silver/silver_mtl.png", glossinessOffset: 0.3f);

    public static MaterialDescriptor Iron(MaterialTextures textures) => Metal(textures, "iron_blend/iron/iron_dif.png", "iron_blend/iron/iron_mtl.png", glossinessOffset: 0.5f);

    public static MaterialDescriptor IronPaint(MaterialTextures textures) => new()
    {
        Attributes =
        {
            Surface = Normal(textures, "iron_blend/paint/iron_paint_nml.png"),
            MicroSurface = new MaterialGlossinessMapFeature(Recipes.Scalar(textures.Data("iron_blend/paint/iron_paint_gls.png"))),
            Diffuse = new MaterialDiffuseMapFeature(Recipes.Colour(textures.Color("iron_blend/paint/iron_paint_dif.png"))),
            DiffuseModel = new MaterialDiffuseLambertModelFeature(),
            Specular = new MaterialMetalnessMapFeature(Recipes.Scalar(textures.Data("iron_blend/paint/iron_paint_mtl.png"))),
            SpecularModel = Recipes.Microfacet(),
        },
    };

    public static MaterialDescriptor IronRust(MaterialTextures textures) => new()
    {
        Attributes =
        {
            Surface = Normal(textures, "iron_blend/rust/iron_rust_nml.png"),
            MicroSurface = new MaterialGlossinessMapFeature(Recipes.Scalar(textures.Data("iron_blend/rust/iron_rust_gls.png"))),
            Diffuse = new MaterialDiffuseMapFeature(Recipes.Colour(textures.Color("iron_blend/rust/iron_rust_dif.png"))),
            DiffuseModel = new MaterialDiffuseLambertModelFeature(),
            // The pack's own file name has the typo
            Specular = new MaterialMetalnessMapFeature(Recipes.Scalar(textures.Data("iron_blend/rust/iton_rust_mtl.png"))),
            SpecularModel = Recipes.Microfacet(),
        },
    };

    /// <summary>The pack's painted iron as it ships: iron with the paint material layered over it through the paint mask.</summary>
    public static MaterialDescriptor IronPaintBlend(MaterialTextures textures)
    {
        var iron = Iron(textures);

        iron.Layers.Add(new MaterialBlendLayer { Material = new Material { Descriptor = IronPaint(textures) }, BlendMap = Recipes.Scalar(textures.Data("iron_blend/paint/iron_paint_msk.png")) });

        return iron;
    }

    /// <summary>The pack's rusted iron as it ships.</summary>
    public static MaterialDescriptor IronRustBlend(MaterialTextures textures)
    {
        var iron = Iron(textures);

        iron.Layers.Add(new MaterialBlendLayer { Material = new Material { Descriptor = IronRust(textures) }, BlendMap = Recipes.Scalar(textures.Data("iron_blend/rust/rust_msk.png")) });

        return iron;
    }

    public static MaterialDescriptor Marble(MaterialTextures textures) => new()
    {
        Attributes =
        {
            MicroSurface = new MaterialGlossinessMapFeature(new ComputeFloat(0.93f)),
            Diffuse = new MaterialDiffuseMapFeature(Recipes.Colour(textures.Color("marble/marble_dif.png"))),
            DiffuseModel = new MaterialDiffuseLambertModelFeature(),
            Specular = new MaterialSpecularMapFeature { SpecularMap = Recipes.Colour(textures.Data("marble/marble_gls.png")), Intensity = new ComputeFloat(0.08f) },
            SpecularModel = Recipes.Microfacet(),
        },
    };

    public static MaterialDescriptor Rock(MaterialTextures textures) => new()
    {
        Attributes =
        {
            Surface = Normal(textures, "rock/rock_nml.png"),
            MicroSurface = new MaterialGlossinessMapFeature(new ComputeBinaryScalar(Recipes.Scalar(textures.Data("rock/rock_gls.png")), new ComputeFloat(0.1f), BinaryOperator.Add)),
            Diffuse = new MaterialDiffuseMapFeature(Recipes.Colour(textures.Color("rock/rock_dif.png"))),
            DiffuseModel = new MaterialDiffuseLambertModelFeature(),
            Specular = new MaterialSpecularMapFeature { SpecularMap = new ComputeColor(Color.White), Intensity = new ComputeFloat(0.03f) },
            SpecularModel = Recipes.Microfacet(),
        },
    };

    public static MaterialDescriptor Rooftile(MaterialTextures textures) => new()
    {
        Attributes =
        {
            Surface = Normal(textures, "rooftile/rooftile_nml.png"),
            MicroSurface = new MaterialGlossinessMapFeature(Recipes.Scalar(textures.Data("rooftile/rooftile_gls.png"))),
            Diffuse = new MaterialDiffuseMapFeature(Recipes.Colour(textures.Color("rooftile/rooftile_dif.png"))),
            DiffuseModel = new MaterialDiffuseLambertModelFeature(),
            // The pack leaves the metalness map empty; a map with no texture is its fallback, which is 1
            Specular = new MaterialMetalnessMapFeature(new ComputeFloat(1f)),
            SpecularModel = Recipes.Microfacet(),
            Occlusion = new MaterialOcclusionMapFeature { AmbientOcclusionMap = Recipes.Scalar(textures.Data("rooftile/rooftile_AO.png")) },
        },
    };

    public static MaterialDescriptor WoodGloss(MaterialTextures textures) => new()
    {
        Attributes =
        {
            Surface = Normal(textures, "wood_gloss/wood_gloss_nml.png"),
            MicroSurface = new MaterialGlossinessMapFeature(new ComputeFloat(0.93f)),
            Diffuse = new MaterialDiffuseMapFeature(Recipes.Colour(textures.Color("wood_gloss/wood_gloss_dif.png"))),
            DiffuseModel = new MaterialDiffuseLambertModelFeature(),
            Specular = new MaterialSpecularMapFeature { SpecularMap = Recipes.Colour(textures.Data("wood_gloss/wood_gloss_spc.png")) },
            SpecularModel = Recipes.Microfacet(),
        },
    };

    public static MaterialDescriptor WoodNongloss(MaterialTextures textures) => new()
    {
        Attributes =
        {
            Surface = Normal(textures, "wood_nongloss/wood_nongloss_nml.png"),
            MicroSurface = new MaterialGlossinessMapFeature(new ComputeBinaryScalar(Recipes.Scalar(textures.Data("wood_nongloss/wood_nongloss_gls.png")), new ComputeFloat(10f), BinaryOperator.Multiply)),
            Diffuse = new MaterialDiffuseMapFeature(Recipes.Colour(textures.Color("wood_nongloss/wood_nongloss_dif.png"))),
            DiffuseModel = new MaterialDiffuseLambertModelFeature(),
            Specular = new MaterialMetalnessMapFeature(Recipes.Scalar(textures.Data("wood_nongloss/wood_nongloss_spc.png"))),
            SpecularModel = Recipes.Microfacet(),
        },
    };

    /// <summary>The pack's metals: the metalness map doubles as the glossiness map with a constant added, a shortcut worth knowing.</summary>
    private static MaterialDescriptor Metal(MaterialTextures textures, string diffuse, string metalness, float glossinessOffset) => new()
    {
        Attributes =
        {
            MicroSurface = new MaterialGlossinessMapFeature(new ComputeBinaryScalar(Recipes.Scalar(textures.Data(metalness)), new ComputeFloat(glossinessOffset), BinaryOperator.Add)),
            Diffuse = new MaterialDiffuseMapFeature(Recipes.Colour(textures.Color(diffuse))),
            DiffuseModel = new MaterialDiffuseLambertModelFeature(),
            Specular = new MaterialMetalnessMapFeature(Recipes.Scalar(textures.Data(metalness))),
            SpecularModel = Recipes.Microfacet(),
        },
    };

    private static MaterialNormalMapFeature Normal(MaterialTextures textures, string path) => new(Recipes.Colour(textures.Normal(path))) { ScaleAndBias = true, IsXYNormal = true };
}