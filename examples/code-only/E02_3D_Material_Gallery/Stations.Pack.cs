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
    public static MaterialDescriptor Brick(MaterialTextures t) => new()
    {
        Attributes =
        {
            Surface = Normal(t, "brick/brick_nml.png"),
            Diffuse = new MaterialDiffuseMapFeature(Recipes.Colour(t.Color("brick/brick_dif.png"))),
            DiffuseModel = new MaterialDiffuseLambertModelFeature(),
            Specular = new MaterialSpecularMapFeature { SpecularMap = new ComputeColor(Color.White), Intensity = new ComputeFloat(0.05f) },
            SpecularModel = Recipes.Microfacet(),
            Occlusion = new MaterialOcclusionMapFeature { AmbientOcclusionMap = Recipes.Scalar(t.Data("brick/brick_AO.png")) },
        },
    };

    public static MaterialDescriptor Gold(MaterialTextures t) => Metal(t, "gold/gold_dif.png", "gold/gold_mtl.png", glossinessOffset: 0.4f);

    public static MaterialDescriptor Silver(MaterialTextures t) => Metal(t, "silver/silver_dif.png", "silver/silver_mtl.png", glossinessOffset: 0.3f);

    public static MaterialDescriptor Iron(MaterialTextures t) => Metal(t, "iron_blend/iron/iron_dif.png", "iron_blend/iron/iron_mtl.png", glossinessOffset: 0.5f);

    public static MaterialDescriptor IronPaint(MaterialTextures t) => new()
    {
        Attributes =
        {
            Surface = Normal(t, "iron_blend/paint/iron_paint_nml.png"),
            MicroSurface = new MaterialGlossinessMapFeature(Recipes.Scalar(t.Data("iron_blend/paint/iron_paint_gls.png"))),
            Diffuse = new MaterialDiffuseMapFeature(Recipes.Colour(t.Color("iron_blend/paint/iron_paint_dif.png"))),
            DiffuseModel = new MaterialDiffuseLambertModelFeature(),
            Specular = new MaterialMetalnessMapFeature(Recipes.Scalar(t.Data("iron_blend/paint/iron_paint_mtl.png"))),
            SpecularModel = Recipes.Microfacet(),
        },
    };

    public static MaterialDescriptor IronRust(MaterialTextures t) => new()
    {
        Attributes =
        {
            Surface = Normal(t, "iron_blend/rust/iron_rust_nml.png"),
            MicroSurface = new MaterialGlossinessMapFeature(Recipes.Scalar(t.Data("iron_blend/rust/iron_rust_gls.png"))),
            Diffuse = new MaterialDiffuseMapFeature(Recipes.Colour(t.Color("iron_blend/rust/iron_rust_dif.png"))),
            DiffuseModel = new MaterialDiffuseLambertModelFeature(),
            // The pack's own file name has the typo
            Specular = new MaterialMetalnessMapFeature(Recipes.Scalar(t.Data("iron_blend/rust/iton_rust_mtl.png"))),
            SpecularModel = Recipes.Microfacet(),
        },
    };

    /// <summary>The pack's painted iron as it ships: iron with the paint material layered over it through the paint mask.</summary>
    public static MaterialDescriptor IronPaintBlend(MaterialStation s)
    {
        var iron = Iron(s.Textures);

        iron.Layers.Add(new MaterialBlendLayer { Material = s.Material(IronPaint(s.Textures)), BlendMap = Recipes.Scalar(s.Textures.Data("iron_blend/paint/iron_paint_msk.png")) });

        return iron;
    }

    /// <summary>The pack's rusted iron as it ships.</summary>
    public static MaterialDescriptor IronRustBlend(MaterialStation s)
    {
        var iron = Iron(s.Textures);

        iron.Layers.Add(new MaterialBlendLayer { Material = s.Material(IronRust(s.Textures)), BlendMap = Recipes.Scalar(s.Textures.Data("iron_blend/rust/rust_msk.png")) });

        return iron;
    }

    public static MaterialDescriptor Marble(MaterialTextures t) => new()
    {
        Attributes =
        {
            MicroSurface = new MaterialGlossinessMapFeature(new ComputeFloat(0.93f)),
            Diffuse = new MaterialDiffuseMapFeature(Recipes.Colour(t.Color("marble/marble_dif.png"))),
            DiffuseModel = new MaterialDiffuseLambertModelFeature(),
            Specular = new MaterialSpecularMapFeature { SpecularMap = Recipes.Colour(t.Data("marble/marble_gls.png")), Intensity = new ComputeFloat(0.08f) },
            SpecularModel = Recipes.Microfacet(),
        },
    };

    public static MaterialDescriptor Rock(MaterialTextures t) => new()
    {
        Attributes =
        {
            Surface = Normal(t, "rock/rock_nml.png"),
            MicroSurface = new MaterialGlossinessMapFeature(new ComputeBinaryScalar(Recipes.Scalar(t.Data("rock/rock_gls.png")), new ComputeFloat(0.1f), BinaryOperator.Add)),
            Diffuse = new MaterialDiffuseMapFeature(Recipes.Colour(t.Color("rock/rock_dif.png"))),
            DiffuseModel = new MaterialDiffuseLambertModelFeature(),
            Specular = new MaterialSpecularMapFeature { SpecularMap = new ComputeColor(Color.White), Intensity = new ComputeFloat(0.03f) },
            SpecularModel = Recipes.Microfacet(),
        },
    };

    public static MaterialDescriptor Rooftile(MaterialTextures t) => new()
    {
        Attributes =
        {
            Surface = Normal(t, "rooftile/rooftile_nml.png"),
            MicroSurface = new MaterialGlossinessMapFeature(Recipes.Scalar(t.Data("rooftile/rooftile_gls.png"))),
            Diffuse = new MaterialDiffuseMapFeature(Recipes.Colour(t.Color("rooftile/rooftile_dif.png"))),
            DiffuseModel = new MaterialDiffuseLambertModelFeature(),
            // The pack leaves the metalness map empty; a map with no texture is its fallback, which is 1
            Specular = new MaterialMetalnessMapFeature(new ComputeFloat(1f)),
            SpecularModel = Recipes.Microfacet(),
            Occlusion = new MaterialOcclusionMapFeature { AmbientOcclusionMap = Recipes.Scalar(t.Data("rooftile/rooftile_AO.png")) },
        },
    };

    public static MaterialDescriptor WoodGloss(MaterialTextures t) => new()
    {
        Attributes =
        {
            Surface = Normal(t, "wood_gloss/wood_gloss_nml.png"),
            MicroSurface = new MaterialGlossinessMapFeature(new ComputeFloat(0.93f)),
            Diffuse = new MaterialDiffuseMapFeature(Recipes.Colour(t.Color("wood_gloss/wood_gloss_dif.png"))),
            DiffuseModel = new MaterialDiffuseLambertModelFeature(),
            Specular = new MaterialSpecularMapFeature { SpecularMap = Recipes.Colour(t.Data("wood_gloss/wood_gloss_spc.png")) },
            SpecularModel = Recipes.Microfacet(),
        },
    };

    public static MaterialDescriptor WoodNongloss(MaterialTextures t) => new()
    {
        Attributes =
        {
            Surface = Normal(t, "wood_nongloss/wood_nongloss_nml.png"),
            MicroSurface = new MaterialGlossinessMapFeature(new ComputeBinaryScalar(Recipes.Scalar(t.Data("wood_nongloss/wood_nongloss_gls.png")), new ComputeFloat(10f), BinaryOperator.Multiply)),
            Diffuse = new MaterialDiffuseMapFeature(Recipes.Colour(t.Color("wood_nongloss/wood_nongloss_dif.png"))),
            DiffuseModel = new MaterialDiffuseLambertModelFeature(),
            Specular = new MaterialMetalnessMapFeature(Recipes.Scalar(t.Data("wood_nongloss/wood_nongloss_spc.png"))),
            SpecularModel = Recipes.Microfacet(),
        },
    };

    /// <summary>The pack's metals: the metalness map doubles as the glossiness map with a constant added, a shortcut worth knowing.</summary>
    private static MaterialDescriptor Metal(MaterialTextures t, string diffuse, string metalness, float glossinessOffset) => new()
    {
        Attributes =
        {
            MicroSurface = new MaterialGlossinessMapFeature(new ComputeBinaryScalar(Recipes.Scalar(t.Data(metalness)), new ComputeFloat(glossinessOffset), BinaryOperator.Add)),
            Diffuse = new MaterialDiffuseMapFeature(Recipes.Colour(t.Color(diffuse))),
            DiffuseModel = new MaterialDiffuseLambertModelFeature(),
            Specular = new MaterialMetalnessMapFeature(Recipes.Scalar(t.Data(metalness))),
            SpecularModel = Recipes.Microfacet(),
        },
    };

    private static MaterialNormalMapFeature Normal(MaterialTextures t, string path) => new(Recipes.Colour(t.Normal(path))) { ScaleAndBias = true, IsXYNormal = true };
}

// The last two stations: the pack as a whole, and one material with every slot filled.
public static class PackStations
{
    /// <summary>
    /// Every material of Game Studio's Material Package, transcribed from its <c>.sdmat</c> into
    /// a <c>MaterialDescriptor</c>, on two rows of spheres: brick, rock, rooftile, marble, gold,
    /// silver, iron, painted iron, rusted iron and the two woods. What the editor hands you is here
    /// in C#, and the transcription is <see cref="PackMaterials"/>.
    /// </summary>
    public static void ThePack(MaterialStation s)
    {
        s.Clear();

        var t = s.Textures;

        s.PlaceRow(
        [
            s.Material(PackMaterials.Brick(t)),
            s.Material(PackMaterials.Rock(t)),
            s.Material(PackMaterials.Rooftile(t)),
            s.Material(PackMaterials.Marble(t)),
            s.Material(PackMaterials.WoodGloss(t)),
            s.Material(PackMaterials.WoodNongloss(t)),
        ], spacing: 1.7f, depth: 1.2f);

        s.PlaceRow(
        [
            s.Material(PackMaterials.Gold(t)),
            s.Material(PackMaterials.Silver(t)),
            s.Material(PackMaterials.Iron(t)),
            s.Material(PackMaterials.IronPaintBlend(s)),
            s.Material(PackMaterials.IronRustBlend(s)),
        ], spacing: 1.7f, depth: -1f);
    }

    /// <summary>
    /// One material with every slot filled - normal, glossiness, metalness, occlusion, an emissive
    /// glow in the mortar, a UV scale - on the trio: what a full PBR material looks like in code,
    /// which is a page of features and nothing else.
    /// </summary>
    public static void TheLot(MaterialStation s)
    {
        s.Clear();

        var t = s.Textures;

        var descriptor = Recipes.Mapped(
            Recipes.Colour(t.Color("brick/brick_dif.png")),
            glossiness: Recipes.Scalar(t.Data("brick/brick_gls.png")),
            metalness: new ComputeFloat(0f),
            normal: Recipes.Colour(t.Normal("brick/brick_nml.png")),
            occlusion: Recipes.Scalar(t.Data("brick/brick_AO.png")),
            emissive: new ComputeBinaryColor(Recipes.Colour(t.Data("brick/brick_AO.png")), new ComputeColor(new Color(255, 120, 30)), BinaryOperator.Multiply),
            emissiveIntensity: 0.6f);

        descriptor.Attributes.Overrides.UVScale = new Vector2(2f);

        s.PlaceTrio(s.Material(descriptor));
    }
}