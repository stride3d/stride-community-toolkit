using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;

namespace E02_3D_Material_Gallery;

// The surfaces: what happens at the boundary of the material - see-through, glass, a coat over a
// coat - and the shading models that replace Lambert and the microfacet, cel and hair.
public static class SurfaceStations
{
    /// <summary>
    /// Four ways to be see-through, on the same blue shape. Blend mixes the surface over what is
    /// behind it by an alpha; additive only brightens, a hologram; cutoff keeps or drops each pixel
    /// by a threshold on a mask, which is how leaves and fences are done with no sorting at all;
    /// dithered is a cutoff whose mask is an ordered dither from a shader class, a screen-door
    /// fade the engine itself uses for shadows of translucent things. V cycles them.
    /// </summary>
    public static void Transparency(MaterialStation s)
    {
        s.Clear();

        var colour = new Color(70, 150, 255);
        var descriptor = Recipes.Pbr(colour, glossiness: 0.7f, metalness: 0f);

        descriptor.Attributes.Transparency = s.Pick("blend", "additive", "cutoff", "dithered") switch
        {
            0 => new MaterialTransparencyBlendFeature { Alpha = new ComputeFloat(0.45f), Tint = new ComputeColor(Color.White) },
            1 => new MaterialTransparencyAdditiveFeature { Alpha = new ComputeFloat(0.7f), Tint = new ComputeColor(Color.White) },
            2 => new MaterialTransparencyCutoffFeature { Alpha = Recipes.Scalar(s.Textures.Generated("holes", () => RuntimeTextures.Holes(s.Game.GraphicsDevice)), tiling: 2f) },
            _ => new MaterialTransparencyCutoffFeature { Alpha = new ComputeShaderClassScalar { MixinReference = "GalleryDither" } },
        };

        // Something behind, so see-through has something to see
        s.Place(PrimitiveModelType.Sphere, s.Material(Recipes.Pbr(new Color(230, 120, 60), 0.5f, 0f)), new Vector3(0f, 0.8f, -2.4f), new Vector3(0.8f));
        s.PlaceTrio(s.Material(descriptor));
    }

    /// <summary>
    /// Glass: the thin-glass specular model, with a refractive index and a Fresnel term of its own.
    /// It is a multi-pass material by itself - a transmittance pass that multiplies what is behind,
    /// then an additive reflection pass, per face side - so it takes no transparency feature; adding
    /// one fights its blend states. The diffuse colour is the transmission tint - what the glass
    /// absorbs, by Beer's law - and there is no diffuse *model*, or the reflection pass would add lit
    /// colour and the glass would go opaque; that is how the engine's own sample glass is built. V
    /// cycles clear, tinted and frosted, where frosted is nothing but a lower glossiness.
    /// </summary>
    /// <remarks>
    /// The engine's transmittance pass ships with its blend state wiped - a 2025 refactor of the
    /// blend description replaced the render target's whole struct where it meant to set two alpha
    /// factors - so the pass paints its transmittance as an opaque colour and the glass is a grey
    /// wall with nothing behind it. The station puts the multiply back on the transmittance passes
    /// after the material is generated. Reported upstream; the workaround can go once the fix ships.
    /// </remarks>
    public static void ThinGlass(MaterialStation s)
    {
        s.Clear();

        var (tint, glossiness) = s.Pick("clear", "tinted", "frosted") switch
        {
            0 => (new Color(245, 250, 255), 0.95f),
            1 => (new Color(90, 215, 70), 0.85f),
            _ => (new Color(225, 230, 235), 0.45f),
        };

        var glass = s.Material(new MaterialDescriptor
        {
            Attributes =
            {
                Diffuse = new MaterialDiffuseMapFeature(new ComputeColor(tint)),
                MicroSurface = new MaterialGlossinessMapFeature(new ComputeFloat(glossiness)),
                Specular = new MaterialMetalnessMapFeature(new ComputeFloat(0.08f)),
                SpecularModel = new MaterialSpecularThinGlassModelFeature
                {
                    RefractiveIndex = 1.563f,
                    Fresnel = new MaterialSpecularMicrofacetFresnelThinGlass(),
                    Environment = new MaterialSpecularMicrofacetEnvironmentThinGlass(),
                },
                // Both faces, so the back of the glass gets its passes too
                CullMode = CullMode.None,
            },
        });

        // What is behind, times the transmittance, the alpha left alone: the blend state the engine's
        // transmittance pass means to have. Every even pass is a transmittance pass, back faces and front
        var transmit = new BlendStateDescription(Blend.Zero, Blend.SourceColor);
        transmit.RenderTargets[0].AlphaSourceBlend = Blend.One;
        transmit.RenderTargets[0].AlphaDestinationBlend = Blend.Zero;

        foreach (var pass in glass.Passes)
        {
            if (pass.PassIndex % 2 == 0) pass.BlendState = transmit;
        }

        s.Place(PrimitiveModelType.Sphere, s.Material(Recipes.Pbr(new Color(230, 120, 60), 0.5f, 0f)), new Vector3(-2.8f, 0.8f, -2.4f), new Vector3(0.8f));
        s.Place(PrimitiveModelType.Cube, s.Material(Recipes.Pbr(new Color(60, 160, 90), 0.5f, 0f)), new Vector3(2.8f, 0.8f, -2.4f), new Vector3(1.4f));
        s.PlaceTrio(glass);
    }

    /// <summary>
    /// Hair: the Kajiya-Kay family of shading models, where the highlight runs along the strand
    /// direction - the mesh tangent - rather than around the normal, with two shifted specular
    /// lobes and a diffuse term wrapped round the strand. Written for hair cards, so the station is
    /// three heads of them, faces towards the visitor: a quiff, a bob and long hair, the last on a
    /// raised head so it can hang. Each is a haircut from a runtime mesh - roots above a hairline,
    /// longer on the crown than at the sides, swept and lifted the way the cut says - wearing a
    /// runtime strand texture whose alpha the material's three passes turn into solid cores and
    /// soft edges (see <see cref="HairCards"/>). The hair moves: the heads turn slowly, and a
    /// feature of the gallery's own in the material's displacement slot - the vertex-stage hook -
    /// sways the cards by a wind set every frame (<see cref="HairSwayFeature"/>), so the anisotropic
    /// highlight slides along the strands as they move. V cycles the three shading models.
    /// </summary>
    /// <remarks>
    /// Requires a Stride build newer than 4.4.0-beta8. The light attenuation is set to none on both
    /// models: the default directional attenuation renders these shapes black at its defaults.
    /// </remarks>
    public static void Hair(MaterialStation s)
    {
        s.Clear();

        // Scheuermann first: on solid shapes its sheen reads at once, where Kajiya-Kay's is subtle
        var model = s.Pick("Scheuermann approximation", "Scheuermann improved", "Kajiya-Kay shifted") switch
        {
            0 => HairShared.HairShadingModel.ScheuermannApproximation,
            1 => HairShared.HairShadingModel.ScheuermannImproved,
            _ => HairShared.HairShadingModel.KajiyaKayShifted,
        };

        var colour = new Color(120, 75, 35);
        var device = s.Game.GraphicsDevice;
        var strands = s.Textures.Generated("strands", () => HairCards.Strands(device, colour));

        // The strand texture is the diffuse; its alpha is what the passes cut by. Both faces of a card are hair
        // Clamped along the strand: wrapped, the tip row would blend with the root row and paint a line across every card end
        var strandInput = Recipes.Colour(strands);
        strandInput.AddressModeV = TextureAddressMode.Clamp;
        var cards = s.Material(HairMaterial(model, strandInput, twoSided: true));

        // A skin-coloured head, so the clear forehead reads as a face; the face is towards the visitor
        const float headRadius = 0.55f;
        var skin = s.Material(Recipes.Pbr(new Color(225, 180, 150), 0.35f, 0f));

        var heads = new List<(Entity Entity, Quaternion Placed)>();

        Head(new Vector3(-2.8f, 1.1f, 0f), HairStyle.Quiff);
        Head(new Vector3(0f, 1.1f, 0f), HairStyle.Bob);
        Head(new Vector3(2.8f, 2.6f, 0f), HairStyle.Long);

        s.State = new HairState(cards, heads);

        void Head(Vector3 at, HairStyle style)
        {
            var head = s.Place(PrimitiveModelType.Sphere, skin, at, new Vector3(headRadius));
            var hair = s.PlaceModel(HairCards.Haircut(device, headRadius, style), cards, at);

            heads.Add((head, head.Transform.Rotation));
            heads.Add((hair, hair.Transform.Rotation));
        }
    }

    /// <summary>
    /// The hair station's frame: the heads turn to and fro, and the sway shader gets the time, a
    /// wind and how far a tip may travel. The material is shared by the three heads, so the wind is
    /// one vector in the hair's object space, which turns with the heads - close enough for a
    /// breeze, and one parameter set rather than three.
    /// </summary>
    public static void HairSway(MaterialStation s)
    {
        if (s.State is not HairState state) return;

        var turn = Quaternion.RotationY(MathF.Sin(s.Seconds * 0.35f) * 0.7f);

        foreach (var (entity, placed) in state.Heads)
        {
            entity.Transform.Rotation = placed * turn;
        }

        foreach (var pass in state.Cards.Passes)
        {
            pass.Parameters.Set(HairSwayKeys.SwayTime, s.Seconds);
            pass.Parameters.Set(HairSwayKeys.SwayStrength, 0.18f);
            pass.Parameters.Set(HairSwayKeys.SwayWind, new Vector3(1f, 0.1f, 0.4f));
        }
    }

    /// <summary>What the hair station keeps between frames: the cards' material and every entity that turns, with the rotation it was placed at.</summary>
    private sealed record HairState(Material Cards, List<(Entity Entity, Quaternion Placed)> Heads);

    /// <summary>
    /// The other parts of a hair material, one at a time against the station before: the three
    /// passes painted red, green and blue; the shadowing function that thins a shadow by the
    /// strand's thickness from the shadow map; per-strand noise on the highlight shift and the
    /// glints, from a runtime texture instead of flat values; and the engine's default direction
    /// function, which reads the bitangent, on cards laid out for it. V walks them; the first
    /// variation is the plain material for reference.
    /// </summary>
    public static void HairParts(MaterialStation s)
    {
        s.Clear();

        var device = s.Game.GraphicsDevice;
        var colour = new Color(120, 75, 35);
        var strands = s.Textures.Generated("strands", () => HairCards.Strands(device, colour));
        var strandInput = Recipes.Colour(strands);
        strandInput.AddressModeV = TextureAddressMode.Clamp;

        var part = s.Pick("plain", "the three passes in colour", "scattering shadowing", "per-strand noise", "direction from the bitangent");

        IComputeScalar? noise = part == 3 ? Recipes.Scalar(s.Textures.Generated("noise", () => RuntimeTextures.Noise(device)), 6f) : null;
        IMaterialHairShadowingFunction? shadowing = part == 2 ? new MaterialHairShadowingFunctionScattering { ExtinctionStrength = 15f } : null;
        IMaterialHairDirectionFunction? direction = part == 4 ? new MaterialHairDirectionFunctionBitangent() : null;

        var cards = s.Material(HairMaterial(HairShared.HairShadingModel.ScheuermannApproximation, strandInput, twoSided: true,
            sway: false, debugPasses: part == 1, shadowing: shadowing, noise: noise, direction: direction));

        const float headRadius = 0.55f;
        var skin = s.Material(Recipes.Pbr(new Color(225, 180, 150), 0.35f, 0f));
        var at = new Vector3(0f, 1.1f, 0f);

        s.Place(PrimitiveModelType.Sphere, skin, at, new Vector3(headRadius));
        s.PlaceModel(HairCards.Haircut(device, headRadius, HairStyle.Bob, directionInBitangent: part == 4), cards, at);
    }

    /// <summary>
    /// The hair material: both hair models on one shading model, attenuation off, and by default
    /// flat noise, shadow-map shadowing, the strand in the tangent.
    /// </summary>
    /// <param name="sway">The vertex-stage sway, on two-sided cards.</param>
    /// <param name="debugPasses">Paint the opaque, back and front passes red, green and blue.</param>
    /// <param name="shadowing">A shadowing function other than the shadow map's.</param>
    /// <param name="noise">A per-strand noise for the highlight shift and the glints, instead of flat values.</param>
    /// <param name="direction">A direction function other than the tangent's.</param>
    private static MaterialDescriptor HairMaterial(HairShared.HairShadingModel model, IComputeColor diffuse, bool twoSided = false,
        bool sway = true, bool debugPasses = false, IMaterialHairShadowingFunction? shadowing = null, IComputeScalar? noise = null, IMaterialHairDirectionFunction? direction = null) => new()
    {
        Attributes =
        {
            Diffuse = new MaterialDiffuseMapFeature(diffuse),
            DiffuseModel = new MaterialDiffuseHairModelFeature
            {
                ShadingModel = model,
                AlphaThreshold = 0.5f,
                DebugRenderPasses = debugPasses,
                HairDirectionFunction = direction ?? new MaterialHairDirectionFunctionTangent(),
                HairShadowingFunction = shadowing ?? new MaterialHairShadowingFunctionShadowing(),
                LightAttenuationFunction = new MaterialHairLightAttenuationFunctionNone(),
            },
            MicroSurface = new MaterialGlossinessMapFeature(new ComputeFloat(0.6f)),
            Specular = new MaterialMetalnessMapFeature(new ComputeFloat(0f)),
            SpecularModel = new MaterialSpecularHairModelFeature
            {
                ShadingModel = model,
                AlphaThreshold = 0.5f,
                DebugRenderPasses = debugPasses,
                // Two lobes in the hair's own colour, well under the defaults, or the sheen bleaches the strands white
                SpecularColor1 = new Color3(0.9f, 0.75f, 0.5f),
                SpecularScale1 = 0.05f,
                SpecularColor2 = new Color3(0.7f, 0.45f, 0.2f),
                SpecularScale2 = 0.12f,
                HairDirectionFunction = direction ?? new MaterialHairDirectionFunctionTangent(),
                HairShadowingFunction = shadowing ?? new MaterialHairShadowingFunctionShadowing(),
                LightAttenuationFunction = new MaterialHairLightAttenuationFunctionNone(),
                // Flat noise unless given some: no shift jitter, full glints. The defaults are texture lookups with no texture
                HairSpecularHighlightsShiftNoise = noise ?? new ComputeFloat(0.5f),
                HairSecondarySpecularGlintsNoise = noise ?? new ComputeFloat(1f),
            },
            // The vertex-stage hook: the sway, on the cards only
            Displacement = twoSided && sway ? new HairSwayFeature() : null,
            CullMode = twoSided ? CullMode.None : CullMode.Back,
        },
    };

    /// <summary>
    /// Subsurface scattering: light that enters the surface and leaves elsewhere, the softness of
    /// skin and wax. The feature marks the material for the compositor's scattering blur and adds a
    /// translucency term from the shadow map's thickness, with a scattering profile - skin's here -
    /// and a kernel. The blur is off here: on 4.4 its shader needs an engine fix and then draws the
    /// frame wrong (reported upstream), so what shows is the translucency, from the sun's shadow map
    /// with transmittance on. V cycles it.
    /// </summary>
    public static void Subsurface(MaterialStation s)
    {
        s.Clear();

        var translucency = s.Pick("translucency 0.83", "translucency 0.3", "translucency 1") switch { 0 => 0.83f, 1 => 0.3f, _ => 1f };

        var descriptor = Recipes.Pbr(new Color(230, 180, 160), 0.45f, 0f);
        descriptor.Attributes.SubsurfaceScattering = new MaterialSubsurfaceScatteringFeature
        {
            Translucency = translucency,
            ScatteringWidth = 0.015f,
            TranslucencyMap = new ComputeFloat(1f),
        };

        s.PlaceTrio(s.Material(descriptor));
    }

    /// <summary>
    /// Car paint: a base colour, metal flakes under it that catch the light from their own random
    /// normals, and a clear coat over both with its own glossiness. One feature holds the three
    /// layers; the flake normal map is a runtime texture of small random tilts. V cycles the paint.
    /// </summary>
    public static void ClearCoat(MaterialStation s)
    {
        s.Clear();

        var paint = s.Pick("candy red", "midnight blue", "british green") switch
        {
            0 => new Color(180, 20, 30),
            1 => new Color(20, 30, 90),
            _ => new Color(10, 70, 40),
        };

        var flakes = s.Textures.Generated("flakes", () => RuntimeTextures.Flakes(s.Game.GraphicsDevice));

        var descriptor = Recipes.Pbr(paint, glossiness: 0.6f, metalness: 0f);

        descriptor.Attributes.ClearCoat = new MaterialClearCoatFeature
        {
            BasePaintDiffuseMap = new ComputeColor(paint),
            BasePaintGlossinessMap = new ComputeFloat(0.6f),
            MetalFlakesDiffuseMap = new ComputeColor(new Color(200, 200, 210)),
            MetalFlakesGlossinessMap = new ComputeFloat(0.85f),
            MetalFlakesMetalnessMap = new ComputeFloat(1f),
            MetalFlakesNormalMap = Recipes.Colour(flakes, tiling: 12f),
            MetalFlakesScaleAndBias = true,
            MetalFlakeslIsXYNormal = true,
            ClearCoatGlossinessMap = new ComputeFloat(0.97f),
            LODDistance = new ComputeFloat(20f),
        };

        s.PlaceTrio(s.Material(descriptor));
    }

    /// <summary>
    /// Cel shading: the diffuse and specular models replaced by ones that quantise the light. The
    /// default function makes a few hard bands from the angle to the light; the ramp function
    /// looks the band up in a texture, here a three-step ramp made at runtime, so the bands are
    /// yours to draw. V switches between them.
    /// </summary>
    public static void CelShading(MaterialStation s)
    {
        s.Clear();

        IMaterialCelShadingLightFunction ramp = s.Pick("default bands", "three-step ramp") == 0
            ? new MaterialCelShadingLightDefault()
            : new MaterialCelShadingLightRamp { RampTexture = s.Textures.Generated("cel-ramp", () => RuntimeTextures.Ramp(s.Game.GraphicsDevice, 0.25f, 0.6f, 1f)) };

        var descriptor = Recipes.Pbr(new Color(240, 120, 90), glossiness: 0.7f, metalness: 0f);

        descriptor.Attributes.DiffuseModel = new MaterialDiffuseCelShadingModelFeature { RampFunction = ramp };
        descriptor.Attributes.SpecularModel = new MaterialSpecularCelShadingModelFeature { RampFunction = ramp };

        s.PlaceTrio(s.Material(descriptor));
    }

}