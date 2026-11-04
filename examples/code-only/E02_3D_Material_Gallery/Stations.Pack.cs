using Stride.Core.Mathematics;
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
    public static MaterialDescriptor IronPaintBlend(MaterialStation station)
    {
        var iron = Iron(station.Textures);

        iron.Layers.Add(new MaterialBlendLayer { Material = station.Compile(IronPaint(station.Textures)), BlendMap = Recipes.Scalar(station.Textures.Data("iron_blend/paint/iron_paint_msk.png")) });

        return iron;
    }

    /// <summary>The pack's rusted iron as it ships.</summary>
    public static MaterialDescriptor IronRustBlend(MaterialStation station)
    {
        var iron = Iron(station.Textures);

        iron.Layers.Add(new MaterialBlendLayer { Material = station.Compile(IronRust(station.Textures)), BlendMap = Recipes.Scalar(station.Textures.Data("iron_blend/rust/rust_msk.png")) });

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

// Two of the closing stations: the pack as a whole, and one material with every slot filled.
public static class PackStations
{
    /// <summary>
    /// Every material of Game Studio's Material Package, transcribed from its <c>.sdmat</c> into
    /// a <c>MaterialDescriptor</c>, on two rows of spheres: brick, rock, rooftile, marble, gold,
    /// silver, iron, painted iron, rusted iron and the two woods. What the editor hands you is here
    /// in C#, and the transcription is <see cref="PackMaterials"/>.
    /// </summary>
    public static void ThePack(MaterialStation station)
    {
        station.Clear();

        var textures = station.Textures;

        station.PlaceRow(
        [
            station.Compile(PackMaterials.Brick(textures)),
            station.Compile(PackMaterials.Rock(textures)),
            station.Compile(PackMaterials.Rooftile(textures)),
            station.Compile(PackMaterials.Marble(textures)),
            station.Compile(PackMaterials.WoodGloss(textures)),
            station.Compile(PackMaterials.WoodNongloss(textures)),
        ], spacing: 1.7f, depth: 1.2f);

        station.PlaceRow(
        [
            station.Compile(PackMaterials.Gold(textures)),
            station.Compile(PackMaterials.Silver(textures)),
            station.Compile(PackMaterials.Iron(textures)),
            station.Compile(PackMaterials.IronPaintBlend(station)),
            station.Compile(PackMaterials.IronRustBlend(station)),
        ], spacing: 1.7f, depth: -1f);
    }

    /// <summary>
    /// One material with every slot filled - normal, glossiness, metalness, occlusion, an emissive
    /// glow in the mortar, a UV scale - on the trio: what a full PBR material looks like in code,
    /// which is a page of features and nothing else.
    /// </summary>
    public static void TheLot(MaterialStation station)
    {
        station.Clear();

        var textures = station.Textures;

        var descriptor = Recipes.Mapped(
            Recipes.Colour(textures.Color("brick/brick_dif.png")),
            glossiness: Recipes.Scalar(textures.Data("brick/brick_gls.png")),
            metalness: new ComputeFloat(0f),
            normal: Recipes.Colour(textures.Normal("brick/brick_nml.png")),
            occlusion: Recipes.Scalar(textures.Data("brick/brick_AO.png")),
            emissive: new ComputeBinaryColor(Recipes.Colour(textures.Data("brick/brick_AO.png")), new ComputeColor(new Color(255, 120, 30)), BinaryOperator.Multiply),
            emissiveIntensity: 0.6f);

        descriptor.Attributes.Overrides.UVScale = new Vector2(2f);

        station.PlaceTrio(station.Compile(descriptor));
    }
}