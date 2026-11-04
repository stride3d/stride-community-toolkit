using Stride.CommunityToolkit.Rendering;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Rendering;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;

namespace E02_3D_Material_Gallery;

// The maps: every slot of a material can be a texture instead of a number, and then every slot can
// be an expression - a vertex stream, two nodes combined, a constant changed while the game runs.
public static class MapStations
{
    /// <summary>
    /// A texture where the colour was: the brick albedo on all three shapes, through a
    /// <c>ComputeTextureColor</c> whose scale tiles it. The teapot shows what a texture does on
    /// UVs that were never unwrapped for it. V cycles the tiling, then three options of the node.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><c>UseRandomTextureCoordinates</c> breaks up the repetition of a tiled texture.</item>
    /// <item><c>Swizzle</c> reorders the channels the node reads: "bgra" swaps red and blue.</item>
    /// <item><c>FallbackValue</c> is what the node gives when it has no texture.</item>
    /// </list>
    /// </remarks>
    public static void AlbedoTexture(MaterialStation station)
    {
        station.Clear();

        var v = station.Pick("tiled once", "tiled twice", "tiled four times", "four times, random coordinates", "swizzle bgra: red and blue swapped", "no texture: the fallback colour");
        var tiling = v switch { 1 => 2f, 2 or 3 => 4f, _ => 1f };
        var brick = Recipes.Colour(station.Textures.Color("brick/brick_dif.png"), tiling);

        switch (v)
        {
            case 3:
                brick.UseRandomTextureCoordinates = true;
                break;
            case 4:
                brick.Swizzle = "bgra";
                break;
            case 5:
                brick.Texture = null;
                brick.FallbackValue = new ComputeColor(new Color(200, 110, 60));
                break;
        }

        station.PlaceTrio(station.Compile(Recipes.Mapped(brick, glossiness: new ComputeFloat(0.3f))));
    }

    /// <summary>
    /// The same albedo twice: the left cube has only the colour map, the right one the normal map
    /// as well, and the light now falls into the mortar lines that were only painted before. A
    /// normal map needs a tangent frame per vertex. V cycles the texture set.
    /// </summary>
    public static void NormalMap(MaterialStation station)
    {
        station.Clear();

        var set = station.Pick("brick", "rock", "rooftile") switch { 0 => "brick", 1 => "rock", _ => "rooftile" };
        var albedo = station.Textures.Color($"{set}/{set}_dif.png");
        var normal = station.Textures.Normal($"{set}/{set}_nml.png");
        var gloss = station.Textures.Data($"{set}/{set}_gls.png");

        var flat = station.Compile(Recipes.Mapped(Recipes.Colour(albedo), glossiness: Recipes.Scalar(gloss)));
        var bumped = station.Compile(Recipes.Mapped(Recipes.Colour(albedo), glossiness: Recipes.Scalar(gloss), normal: Recipes.Colour(normal)));

        station.Place(Primitive3D.Cube(1.8f), flat, new Vector3(-1.6f, 0.9f, 0f));
        station.Place(Primitive3D.Cube(1.8f), bumped, new Vector3(1.6f, 0.9f, 0f));
    }

    /// <summary>
    /// Maps for the numbers: wood with a glossiness map and a specular map (the specular workflow),
    /// iron with a glossiness map and a metalness map (the metalness workflow), gold with both
    /// maps and a normal-less shine. Where a number was one value for the whole surface, a map
    /// makes it vary per texel - the varnish on the wood, the dull patches on the iron.
    /// </summary>
    /// <remarks>
    /// The second variation reads the iron's glossiness and metalness from one texture: glossiness in
    /// its red channel, metalness in its green. <c>ComputeTextureScalar.Channel</c> picks the channel,
    /// so one packed texture can feed several slots. The iron looks the same in both variations.
    /// </remarks>
    public static void GlossAndMetalMaps(MaterialStation station)
    {
        station.Clear();

        var textures = station.Textures;
        var packed = station.Pick("one texture per map", "iron: both maps packed into one texture") == 1;

        var wood = station.Compile(Recipes.Mapped(
            Recipes.Colour(textures.Color("wood_nongloss/wood_nongloss_dif.png")),
            glossiness: Recipes.Scalar(textures.Data("wood_nongloss/wood_nongloss_gls.png")),
            specular: Recipes.Colour(textures.Data("wood_nongloss/wood_nongloss_spc.png")),
            normal: Recipes.Colour(textures.Normal("wood_nongloss/wood_nongloss_nml.png"))));

        var ironMaps = packed ? textures.Packed("iron-gloss-metal", "iron_blend/iron/iron_gls.png", "iron_blend/iron/iron_mtl.png") : null;

        var iron = station.Compile(Recipes.Mapped(
            Recipes.Colour(textures.Color("iron_blend/iron/iron_dif.png")),
            glossiness: ironMaps is null ? Recipes.Scalar(textures.Data("iron_blend/iron/iron_gls.png")) : Recipes.Scalar(ironMaps, channel: ColorChannel.R),
            metalness: ironMaps is null ? Recipes.Scalar(textures.Data("iron_blend/iron/iron_mtl.png")) : Recipes.Scalar(ironMaps, channel: ColorChannel.G)));

        var gold = station.Compile(Recipes.Mapped(
            Recipes.Colour(textures.Color("gold/gold_dif.png")),
            glossiness: Recipes.Scalar(textures.Data("gold/gold_gls.png")),
            metalness: Recipes.Scalar(textures.Data("gold/gold_mtl.png"))));

        station.PlaceTrio(wood, iron, gold);
    }

    /// <summary>
    /// Ambient occlusion: a map that says how much of the sky each point can see, so the crevices
    /// of the brick go dark under the skybox's light where a flat surface would light them evenly.
    /// It scales the ambient term only; the direct light is left alone. Left without, right with.
    /// V cycles the texture set, then the two ways to darken the direct light too.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><c>DirectLightingFactor</c> is how much of the occlusion applies to direct light. The default is 0.</item>
    /// <item><c>CavityMap</c> darkens direct light by a map of its own, with <c>DiffuseCavity</c> and <c>SpecularCavity</c> as its strength on each term.</item>
    /// </list>
    /// The pack's occlusion maps are nearly white, so the last three variations use the brick's map
    /// with more contrast, made in code. That makes the three comparable with each other.
    /// </remarks>
    public static void Occlusion(MaterialStation station)
    {
        station.Clear();

        var v = station.Pick("brick: ambient light only", "rooftile: ambient light only", "brick, deeper map: ambient light only", "brick, deeper map: direct light too", "brick, deeper map: as a cavity map");
        var set = v == 1 ? "rooftile" : "brick";
        var albedo = Recipes.Colour(station.Textures.Color($"{set}/{set}_dif.png"));
        var normal = Recipes.Colour(station.Textures.Normal($"{set}/{set}_nml.png"));
        var gloss = Recipes.Scalar(station.Textures.Data($"{set}/{set}_gls.png"));

        var map = v >= 2
            ? station.Textures.Deepened("brick-occlusion-deep", "brick/brick_AO.png", power: 8f)
            : station.Textures.Data($"{set}/{set}_AO.png");

        var descriptor = Recipes.Mapped(albedo, glossiness: gloss, normal: normal, occlusion: Recipes.Scalar(map));
        var occlusion = (MaterialOcclusionMapFeature)descriptor.Attributes.Occlusion;

        if (v == 3) occlusion.DirectLightingFactor = new ComputeFloat(1f);

        if (v == 4)
        {
            occlusion.CavityMap = Recipes.Scalar(map);
            occlusion.DiffuseCavity = new ComputeFloat(1f);
            occlusion.SpecularCavity = new ComputeFloat(1f);
        }

        var open = station.Compile(Recipes.Mapped(albedo, glossiness: gloss, normal: normal));
        var occluded = station.Compile(descriptor);

        station.Place(Primitive3D.Cube(1.8f), open, new Vector3(-1.6f, 0.9f, 0f));
        station.Place(Primitive3D.Cube(1.8f), occluded, new Vector3(1.6f, 0.9f, 0f));
    }

    /// <summary>
    /// Light that comes from the surface itself: an emissive colour on a near-black base, so the
    /// shapes glow in their own colour and bloom in the post effects. The intensity is a material
    /// parameter, changed every frame by <see cref="EmissivePulse"/> without rebuilding anything -
    /// which is how a material animates. V cycles the colour.
    /// </summary>
    public static void Emissive(MaterialStation station)
    {
        station.Clear();

        var colour = station.Pick("cyan", "amber", "magenta") switch
        {
            0 => new Color(40, 220, 255),
            1 => new Color(255, 170, 40),
            _ => new Color(255, 60, 200),
        };

        var material = station.Compile(Recipes.Mapped(new ComputeColor(new Color(12, 12, 16)), glossiness: new ComputeFloat(0.5f), emissive: new ComputeColor(colour), emissiveIntensity: 1f));

        station.State = material;
        station.PlaceTrio(material);
    }

    /// <summary>The per-frame half of the emissive station: the intensity breathes between dim and bright.</summary>
    public static void EmissivePulse(MaterialStation station)
    {
        if (station.State is not Material material) return;

        var pulse = 0.5f + 0.5f * MathF.Sin(station.Seconds * 2f);

        // A material's parameters are the shader's constants; a keyed value set here is picked up
        // by the next draw. EmissiveIntensity is the key the emissive feature registers its scalar under,
        // and the toolkit's setter writes it to every pass the material has.
        material.Set(MaterialKeys.EmissiveIntensity, 0.2f + 3f * pulse);
    }

    /// <summary>
    /// Three ways a material changes every frame without being rebuilt, all through the parameters
    /// the generator registered: the sphere's diffuse is a colour node carrying a key of the
    /// station's own, so its tint walks round the hue; the cube's brick texture scrolls through the
    /// offset key every texture node registers; the teapot's glossiness breathes through the key the
    /// number in its glossiness slot registered under. Every write goes through the toolkit's
    /// <c>MaterialParameters</c>, which converts a colour the way the generator did - to linear,
    /// premultiplied - and reaches every pass of the material.
    /// </summary>
    public static void AnimatedParameters(MaterialStation station)
    {
        station.Clear();

        // A colour node with a key of our own: what the generator would have named MaterialKeys.DiffuseValue
        // is instead this key, and this key is what the update sets
        var tint = station.Compile(Recipes.Mapped(new ComputeColor(Color.White) { Key = TintKey }, glossiness: new ComputeFloat(0.6f)));
        var scroll = station.Compile(Recipes.Mapped(Recipes.Colour(station.Textures.Color("brick/brick_dif.png"), 2f), glossiness: new ComputeFloat(0.4f)));
        var gloss = station.Compile(Recipes.Pbr(new Color(60, 110, 200), glossiness: 0.5f, metalness: 0f));

        station.State = new AnimatedState(tint, scroll, gloss);
        station.Place(Primitive3D.Sphere(), tint, MaterialStation.SphereSpot);
        station.Place(Primitive3D.Cube(), scroll, MaterialStation.CubeSpot);
        station.Place(Primitive3D.Teapot(), gloss, MaterialStation.TeapotSpot);
    }

    /// <summary>The per-frame half: the tint round the hue, the bricks scrolling, the glossiness breathing.</summary>
    public static void AnimatedParametersUpdate(MaterialStation station)
    {
        if (station.State is not AnimatedState state) return;

        var hue = station.Seconds * 40f % 360f;

        state.Tint.SetColor(TintKey, new ColorHSV(hue, 0.7f, 1f, 1f).ToColor());
        state.Scroll.SetTextureOffset(new Vector2(station.Seconds * 0.15f, 0f));
        state.Gloss.Set(MaterialKeys.GlossinessValue, 0.5f + 0.5f * MathF.Sin(station.Seconds));
    }

    /// <summary>The key the tint sphere's colour node carries; named so it reads in a debugger.</summary>
    private static readonly ValueParameterKey<Color4> TintKey = MaterialParameters.ColorKey("MaterialGallery.Tint");

    private sealed record AnimatedState(Material Tint, Material Scroll, Material Gloss);

    /// <summary>
    /// The colour from the vertices: a cube built with <c>MeshBuilder</c> whose corners each carry
    /// a colour, read by the material through <c>ComputeVertexStreamColor</c> - no texture, no
    /// UVs, one colour per vertex interpolated across each face. What a painter's tool exports,
    /// and what a procedural mesh can carry for free. The stream is saturated to 0..1 on the way
    /// in, so a vertex colour cannot carry HDR.
    /// </summary>
    public static void VertexColours(MaterialStation station)
    {
        station.Clear();

        var material = station.Compile(Recipes.Mapped(
            new ComputeVertexStreamColor { Stream = new ColorVertexStreamDefinition() },
            glossiness: new ComputeFloat(0.4f)));

        station.PlaceModel(ColouredCube(station), material, new Vector3(-1.8f, 0.9f, 0f), 1.8f);
        station.PlaceModel(ColouredCube(station), material, new Vector3(1.8f, 0.9f, 0f), 1.8f, Quaternion.RotationY(MathF.PI * 0.25f) * Quaternion.RotationX(MathF.PI * 0.2f));
    }

    /// <summary>
    /// Two textures combined by an operator before the material sees them: a
    /// <c>ComputeBinaryColor</c> is a node with two children, and any node can be a child, so
    /// a tint, a mask or a whole tree goes in the diffuse slot. V cycles the operator over the
    /// same two inputs - brick and marble. The last two variations add a tint to the brick.
    /// </summary>
    /// <remarks>
    /// <c>BinaryOperator.Add</c> composites the second colour by its alpha, as a paint program's Add
    /// layer does: a tint at half alpha adds half of itself. <c>BinaryOperator.AddMath</c> is the plain
    /// sum of the two colours.
    /// </remarks>
    public static void NodeArithmetic(MaterialStation station)
    {
        station.Clear();

        var v = station.Pick("Multiply", "Average", "Overlay", "Difference", "Screen", "Desaturate", "Add: a tint at half alpha adds half", "AddMath: the same tint adds all");

        var op = v switch
        {
            0 => BinaryOperator.Multiply,
            1 => BinaryOperator.Average,
            2 => BinaryOperator.Overlay,
            3 => BinaryOperator.Difference,
            4 => BinaryOperator.Screen,
            5 => BinaryOperator.Desaturate,
            6 => BinaryOperator.Add,
            _ => BinaryOperator.AddMath,
        };

        var brick = Recipes.Colour(station.Textures.Color("brick/brick_dif.png"));

        IComputeColor second = v >= 6
            ? new ComputeColor(new Color4(0.1f, 0.25f, 0.6f, 0.5f))
            : Recipes.Colour(station.Textures.Color("marble/marble_dif.png"));

        station.PlaceTrio(station.Compile(Recipes.Mapped(new ComputeBinaryColor(brick, second, op), glossiness: new ComputeFloat(0.5f))));
    }

    /// <summary>A unit cube with per-face normals and a colour at every corner, from the toolkit's MeshBuilder.</summary>
    private static Model ColouredCube(MaterialStation station)
    {
        using var builder = new MeshBuilder();

        builder.WithIndexType(IndexingType.Int16);

        var position = builder.WithPosition<Vector3>();
        var normal = builder.WithNormal<Vector3>();
        var colour = builder.WithColor<Color>();

        ReadOnlySpan<Vector3> normals = [Vector3.UnitX, -Vector3.UnitX, Vector3.UnitY, -Vector3.UnitY, Vector3.UnitZ, -Vector3.UnitZ];

        foreach (var n in normals)
        {
            // The face's own axes; both triangles wind clockwise seen from outside, which is the front
            // face in Stride as in Direct3D - the other way round and culling shows the insides
            var up = MathF.Abs(n.Y) > 0.5f ? Vector3.UnitZ : Vector3.UnitY;
            var right = Vector3.Cross(up, n);
            var first = builder.VertexCount;

            foreach (var (u, v) in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f) })
            {
                var corner = (n + right * u + up * v) * 0.5f;

                builder.AddVertex();
                builder.SetElement(position, corner);
                builder.SetElement(normal, n);
                // A colour from the corner's position: the eight corners of the cube get eight colours
                builder.SetElement(colour, new Color(corner.X + 0.5f, corner.Y + 0.5f, corner.Z + 0.5f));
            }

            builder.AddIndex(first);
            builder.AddIndex(first + 2);
            builder.AddIndex(first + 1);
            builder.AddIndex(first);
            builder.AddIndex(first + 3);
            builder.AddIndex(first + 2);
        }

        return new Model { new Mesh { Draw = builder.ToMeshDraw(station.Game.GraphicsDevice), MaterialIndex = 0 } };
    }
}