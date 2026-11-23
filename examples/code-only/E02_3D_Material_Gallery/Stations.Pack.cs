using Stride.Core.Mathematics;
using Stride.Rendering.Materials.ComputeColors;

namespace E02_3D_Material_Gallery;

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
            station.Compile(PackMaterials.IronPaintBlend(station.Textures)),
            station.Compile(PackMaterials.IronRustBlend(station.Textures)),
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