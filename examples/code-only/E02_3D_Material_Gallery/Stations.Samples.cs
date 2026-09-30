using Stride.CommunityToolkit.Rendering;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;

namespace E02_3D_Material_Gallery;

/// <summary>
/// Tunings from the materials of the engine's own samples, each a small trick in nodes: a texture
/// mirrored and offset, a map reused in another slot, a normal map weakened, a colour added to a
/// texture, a layer masked by a texture's alpha. The numbers are the samples' own. The textures they
/// need are the pack's or are made in code, so no sample asset is copied.
/// </summary>
public static class SampleStations
{
    /// <summary>
    /// Five recipes, one per variation. The sample each comes from is named in its method.
    /// </summary>
    public static void SampleRecipes(MaterialStation s)
    {
        s.Clear();

        switch (s.Pick("brushed metal: a gloss map mirrored and offset", "wood table: the specular map reused as roughness", "brick: a normal map weakened, full on the left", "neon sign: a layer masked by a texture's alpha, emissive at 50", "prototyping grid: a colour added to a checker"))
        {
            case 0:
                s.PlaceTrio(s.Material(BrushedMetal(s)));
                break;
            case 1:
                s.PlaceTrio(s.Material(WoodTable(s)));
                break;
            case 2:
                s.Place(PrimitiveModelType.Cube, s.Material(Brick(s, 1f)), new Vector3(-1.6f, 0.9f, 0f), new Vector3(1.8f));
                s.Place(PrimitiveModelType.Cube, s.Material(Brick(s, 0.2f)), new Vector3(1.6f, 0.9f, 0f), new Vector3(1.8f));
                break;
            case 3:
                s.PlaceTrio(s.Material(NeonSign(s)));
                break;
            default:
                s.PlaceTrio(s.Material(PrototypingGrid(s)));
                break;
        }
    }

    /// <summary>
    /// The samples' DullSilver: a metal in one grey, whose gloss map is tiled twice, mirrored at the
    /// seams and offset, so the streaks never repeat visibly. The map here is made in code.
    /// </summary>
    private static MaterialDescriptor BrushedMetal(MaterialStation s)
    {
        var streaks = s.Textures.Generated("brushed", () => RuntimeTextures.Brushed(s.Game.GraphicsDevice));

        var gloss = new ComputeTextureScalar(streaks, TextureCoordinate.Texcoord0, new Vector2(2f), new Vector2(0.25f, 0.37f))
        {
            AddressModeU = TextureAddressMode.Mirror,
            AddressModeV = TextureAddressMode.Mirror,
        };

        return Recipes.Mapped(new ComputeColor(new Color4(0.41f, 0.408f, 0.406f, 1f)), glossiness: gloss, metalness: new ComputeFloat(1f));
    }

    /// <summary>
    /// The samples' board1: one specular map does two jobs. As the specular colour at 30 percent, and as
    /// the glossiness, scaled to a quarter and inverted, so where the wood is most reflective it is also
    /// roughest. <c>MaterialGlossinessMapFeature.Invert</c> reads a roughness map as glossiness.
    /// </summary>
    private static MaterialDescriptor WoodTable(MaterialStation s)
    {
        var t = s.Textures;
        var specular = t.Data("wood_gloss/wood_gloss_spc.png");

        var descriptor = Recipes.Mapped(
            Recipes.Colour(t.Color("wood_gloss/wood_gloss_dif.png")),
            specular: Recipes.Colour(specular),
            normal: Recipes.Colour(t.Normal("wood_gloss/wood_gloss_nml.png")));

        descriptor.Attributes.MicroSurface = new MaterialGlossinessMapFeature(new ComputeBinaryScalar(Recipes.Scalar(specular), new ComputeFloat(0.25f), BinaryOperator.Multiply)) { Invert = true };
        ((MaterialSpecularMapFeature)descriptor.Attributes.Specular).Intensity = new ComputeFloat(0.3f);

        return descriptor;
    }

    /// <summary>
    /// The samples' board1 weakens its normal map by multiplying it with (0.015, 0.015, 1, 1). That map
    /// is signed. The pack's maps are unsigned, 0.5 for flat, so the same weakening scales the map
    /// about 0.5: the texture times the strength, plus half of what is left.
    /// </summary>
    private static MaterialDescriptor Brick(MaterialStation s, float strength)
    {
        var t = s.Textures;
        var map = Recipes.Colour(t.Normal("brick/brick_nml.png"));

        IComputeColor normal = strength >= 1f
            ? map
            : new ComputeBinaryColor(
                new ComputeBinaryColor(map, new ComputeFloat4(new Vector4(strength, strength, 1f, 1f)), BinaryOperator.Multiply),
                new ComputeFloat4(new Vector4(0.5f * (1f - strength), 0.5f * (1f - strength), 0f, 0f)),
                BinaryOperator.AddMath);

        return Recipes.Mapped(Recipes.Colour(t.Color("brick/brick_dif.png")), glossiness: Recipes.Scalar(t.Data("brick/brick_gls.png")), normal: normal);
    }

    /// <summary>
    /// The samples' LogoA over MaskC: a dark material with a layer on top, the layer masked by the alpha
    /// channel of a texture and glowing where it shows. The glow is the mask times (5, 9, 50, 5), a
    /// <c>ComputeFloat4</c>, which is passed to the shader as it is: the blue at 50 is what blooms.
    /// </summary>
    private static MaterialDescriptor NeonSign(MaterialStation s)
    {
        var sign = s.Textures.Generated("sign", () => Sign(s.Game.GraphicsDevice));

        var glow = new MaterialDescriptor
        {
            Attributes =
            {
                Diffuse = new MaterialDiffuseMapFeature(new ComputeColor(Color.Black)),
                DiffuseModel = new MaterialDiffuseLambertModelFeature(),
                MicroSurface = new MaterialGlossinessMapFeature(new ComputeFloat(0.5f)),
                Specular = new MaterialSpecularMapFeature { SpecularMap = new ComputeColor(Color.White), Intensity = new ComputeFloat(0.15f) },
                SpecularModel = Recipes.Microfacet(),
                Emissive = new MaterialEmissiveMapFeature(new ComputeBinaryColor(new ComputeTextureColor(sign), new ComputeFloat4(new Vector4(5f, 9f, 50f, 5f)), BinaryOperator.Multiply)),
            },
        };

        var descriptor = new MaterialDescriptor
        {
            Attributes =
            {
                Diffuse = new MaterialDiffuseMapFeature(new ComputeColor(new Color(12, 12, 14))),
                DiffuseModel = new MaterialDiffuseLambertModelFeature(),
                MicroSurface = new MaterialGlossinessMapFeature(new ComputeFloat(0.5f)),
                Specular = new MaterialSpecularMapFeature { SpecularMap = new ComputeColor(Color.White), Intensity = new ComputeFloat(0.15f) },
                SpecularModel = Recipes.Microfacet(),
            },
        };

        descriptor.Layers.Add(new MaterialBlendLayer
        {
            Material = s.Material(glow),
            BlendMap = new ComputeTextureScalar(sign, TextureCoordinate.Texcoord0, Vector2.One, Vector2.Zero) { Channel = ColorChannel.A },
        });

        return descriptor;
    }

    /// <summary>
    /// The Prototyping Blocks' GridMTArc: one grey checker texture serves every colour of block, tinted
    /// by adding a colour to it in the diffuse slot. Metalness and glossiness at a half.
    /// </summary>
    private static MaterialDescriptor PrototypingGrid(MaterialStation s)
    {
        var checker = s.Textures.Generated("checker", () => Checker(s.Game.GraphicsDevice));
        var tinted = new ComputeBinaryColor(Recipes.Colour(checker, tiling: 2f), new ComputeColor(new Color4(0.031f, 0.435f, 0f, 1f)), BinaryOperator.Add);

        return Recipes.Mapped(tinted, glossiness: new ComputeFloat(0.5f), metalness: new ComputeFloat(0.5f));
    }

    /// <summary>Three chevrons, white where the sign is and transparent elsewhere: a colour texture with an alpha.</summary>
    private static Texture Sign(GraphicsDevice device, int size = 256)
    {
        var pixels = new Color[size * size];

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var u = (x + 0.5f) / size;
                var v = (y + 0.5f) / size;
                var alpha = 0f;

                for (var chevron = 0; chevron < 3; chevron++)
                {
                    // A chevron pointing right: a band of constant thickness folded at its middle
                    var along = u - (0.22f + chevron * 0.26f) + MathF.Abs(v - 0.5f) * 0.6f;
                    var inside = MathF.Min(0.05f - MathF.Abs(along), 0.32f - MathF.Abs(v - 0.5f));

                    alpha = MathF.Max(alpha, MathUtil.Clamp(inside * size / 2f + 0.5f, 0f, 1f));
                }

                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        return TextureLoader.FromPixels(device, pixels, size, size, new TextureLoadOptions(TextureRole.Color));
    }

    /// <summary>The Prototyping Blocks' grid: light and dark cells with a thin line between them, as a colour texture.</summary>
    private static Texture Checker(GraphicsDevice device, int size = 256, int cells = 4)
    {
        var pixels = new Color[size * size];
        var cell = size / cells;

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var onLine = x % cell < 2 || y % cell < 2;
                var dark = (x / cell + y / cell) % 2 == 0;
                var grey = onLine ? 0.25f : dark ? 0.55f : 0.75f;

                pixels[y * size + x] = new Color(grey, grey, grey);
            }
        }

        return TextureLoader.FromPixels(device, pixels, size, size, new TextureLoadOptions(TextureRole.Color));
    }
}