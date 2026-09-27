using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Rendering.Utilities;
using Stride.CommunityToolkit.Rendering;
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
    /// UVs that were never unwrapped for it. V cycles the tiling.
    /// </summary>
    public static void AlbedoTexture(MaterialStation s)
    {
        s.Clear();

        var tiling = s.Pick("tiled once", "tiled twice", "tiled four times") switch { 0 => 1f, 1 => 2f, _ => 4f };

        s.PlaceTrio(s.Material(Recipes.Mapped(Recipes.Colour(s.Textures.Color("brick/brick_dif.png"), tiling), glossiness: new ComputeFloat(0.3f))));
    }

    /// <summary>
    /// The same albedo twice: the left cube has only the colour map, the right one the normal map
    /// as well, and the light now falls into the mortar lines that were only painted before. A
    /// normal map costs a tangent frame per vertex and nothing else. V cycles the texture set.
    /// </summary>
    public static void NormalMap(MaterialStation s)
    {
        s.Clear();

        var set = s.Pick("brick", "rock", "rooftile") switch { 0 => "brick", 1 => "rock", _ => "rooftile" };
        var albedo = s.Textures.Color($"{set}/{set}_dif.png");
        var normal = s.Textures.Data($"{set}/{set}_nml.png");
        var gloss = s.Textures.Data($"{set}/{set}_gls.png");

        var flat = s.Material(Recipes.Mapped(Recipes.Colour(albedo), glossiness: Recipes.Scalar(gloss)));
        var bumped = s.Material(Recipes.Mapped(Recipes.Colour(albedo), glossiness: Recipes.Scalar(gloss), normal: Recipes.Colour(normal)));

        s.Place(PrimitiveModelType.Cube, flat, new Vector3(-1.6f, 0.9f, 0f), new Vector3(1.8f));
        s.Place(PrimitiveModelType.Cube, bumped, new Vector3(1.6f, 0.9f, 0f), new Vector3(1.8f));
    }

    /// <summary>
    /// Maps for the numbers: wood with a glossiness map and a specular map (the specular workflow),
    /// iron with a glossiness map and a metalness map (the metalness workflow), gold with both
    /// maps and a normal-less shine. Where a number was one value for the whole surface, a map
    /// makes it vary per texel - the varnish on the wood, the dull patches on the iron.
    /// </summary>
    public static void GlossAndMetalMaps(MaterialStation s)
    {
        s.Clear();

        var t = s.Textures;

        var wood = s.Material(Recipes.Mapped(
            Recipes.Colour(t.Color("wood_nongloss/wood_nongloss_dif.png")),
            glossiness: Recipes.Scalar(t.Data("wood_nongloss/wood_nongloss_gls.png")),
            specular: Recipes.Colour(t.Data("wood_nongloss/wood_nongloss_spc.png")),
            normal: Recipes.Colour(t.Data("wood_nongloss/wood_nongloss_nml.png"))));

        var iron = s.Material(Recipes.Mapped(
            Recipes.Colour(t.Color("iron_blend/iron/iron_dif.png")),
            glossiness: Recipes.Scalar(t.Data("iron_blend/iron/iron_gls.png")),
            metalness: Recipes.Scalar(t.Data("iron_blend/iron/iron_mtl.png"))));

        var gold = s.Material(Recipes.Mapped(
            Recipes.Colour(t.Color("gold/gold_dif.png")),
            glossiness: Recipes.Scalar(t.Data("gold/gold_gls.png")),
            metalness: Recipes.Scalar(t.Data("gold/gold_mtl.png"))));

        s.PlaceTrio(wood, iron, gold);
    }

    /// <summary>
    /// Ambient occlusion: a map that says how much of the sky each point can see, so the crevices
    /// of the brick go dark under the skybox's light where a flat surface would light them evenly.
    /// It scales the ambient term only; the direct light is left alone. Left without, right with.
    /// V cycles the texture set.
    /// </summary>
    public static void Occlusion(MaterialStation s)
    {
        s.Clear();

        var set = s.Pick("brick", "rooftile") == 0 ? "brick" : "rooftile";
        var albedo = Recipes.Colour(s.Textures.Color($"{set}/{set}_dif.png"));
        var normal = Recipes.Colour(s.Textures.Data($"{set}/{set}_nml.png"));
        var gloss = Recipes.Scalar(s.Textures.Data($"{set}/{set}_gls.png"));

        var open = s.Material(Recipes.Mapped(albedo, glossiness: gloss, normal: normal));
        var occluded = s.Material(Recipes.Mapped(albedo, glossiness: gloss, normal: normal, occlusion: Recipes.Scalar(s.Textures.Data($"{set}/{set}_AO.png"))));

        s.Place(PrimitiveModelType.Cube, open, new Vector3(-1.6f, 0.9f, 0f), new Vector3(1.8f));
        s.Place(PrimitiveModelType.Cube, occluded, new Vector3(1.6f, 0.9f, 0f), new Vector3(1.8f));
    }

    /// <summary>
    /// Light that comes from the surface itself: an emissive colour on a near-black base, so the
    /// shapes glow in their own colour and bloom in the post effects. The intensity is a material
    /// parameter, changed every frame by <see cref="EmissivePulse"/> without rebuilding anything -
    /// which is how a material animates. V cycles the colour.
    /// </summary>
    public static void Emissive(MaterialStation s)
    {
        s.Clear();

        var colour = s.Pick("cyan", "amber", "magenta") switch
        {
            0 => new Color(40, 220, 255),
            1 => new Color(255, 170, 40),
            _ => new Color(255, 60, 200),
        };

        var material = s.Material(Recipes.Mapped(new ComputeColor(new Color(12, 12, 16)), glossiness: new ComputeFloat(0.5f), emissive: new ComputeColor(colour), emissiveIntensity: 1f));

        s.State = material;
        s.PlaceTrio(material);
    }

    /// <summary>The per-frame half of the emissive station: the intensity breathes between dim and bright.</summary>
    public static void EmissivePulse(MaterialStation s)
    {
        if (s.State is not Material material) return;

        var pulse = 0.5f + 0.5f * MathF.Sin(s.Seconds * 2f);

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
    public static void AnimatedParameters(MaterialStation s)
    {
        s.Clear();

        // A colour node with a key of our own: what the generator would have named MaterialKeys.DiffuseValue
        // is instead this key, and this key is what the update sets
        var tint = s.Material(Recipes.Mapped(new ComputeColor(Color.White) { Key = TintKey }, glossiness: new ComputeFloat(0.6f)));
        var scroll = s.Material(Recipes.Mapped(Recipes.Colour(s.Textures.Color("brick/brick_dif.png"), 2f), glossiness: new ComputeFloat(0.4f)));
        var gloss = s.Material(Recipes.Pbr(new Color(60, 110, 200), glossiness: 0.5f, metalness: 0f));

        s.State = new AnimatedState(tint, scroll, gloss);
        s.Place(PrimitiveModelType.Sphere, tint, MaterialStation.SphereSpot);
        s.Place(PrimitiveModelType.Cube, scroll, MaterialStation.CubeSpot);
        s.Place(PrimitiveModelType.Teapot, gloss, MaterialStation.TeapotSpot);
    }

    /// <summary>The per-frame half: the tint round the hue, the bricks scrolling, the glossiness breathing.</summary>
    public static void AnimatedParametersUpdate(MaterialStation s)
    {
        if (s.State is not AnimatedState state) return;

        var hue = s.Seconds * 40f % 360f;

        state.Tint.SetColor(TintKey, new ColorHSV(hue, 0.7f, 1f, 1f).ToColor());
        state.Scroll.SetTextureOffset(new Vector2(s.Seconds * 0.15f, 0f));
        state.Gloss.Set(MaterialKeys.GlossinessValue, 0.5f + 0.5f * MathF.Sin(s.Seconds));
    }

    /// <summary>The key the tint sphere's colour node carries; named so it reads in a debugger.</summary>
    private static readonly ValueParameterKey<Color4> TintKey = MaterialParameters.ColorKey("MaterialGallery.Tint");

    private sealed record AnimatedState(Material Tint, Material Scroll, Material Gloss);

    /// <summary>
    /// The colour from the vertices: a cube built with <c>MeshBuilder</c> whose corners each carry
    /// a colour, read by the material through <c>ComputeVertexStreamColor</c> - no texture, no
    /// UVs, one colour per vertex interpolated across each face. What a painter's tool exports,
    /// and what a procedural mesh can carry for free.
    /// </summary>
    public static void VertexColours(MaterialStation s)
    {
        s.Clear();

        var material = s.Material(Recipes.Mapped(
            new ComputeVertexStreamColor { Stream = new ColorVertexStreamDefinition() },
            glossiness: new ComputeFloat(0.4f)));

        s.PlaceModel(ColouredCube(s), material, new Vector3(-1.8f, 0.9f, 0f), 1.8f);
        s.PlaceModel(ColouredCube(s), material, new Vector3(1.8f, 0.9f, 0f), 1.8f, Quaternion.RotationY(MathF.PI * 0.25f) * Quaternion.RotationX(MathF.PI * 0.2f));
    }

    /// <summary>
    /// Two textures combined by an operator before the material sees them: a
    /// <c>ComputeBinaryColor</c> is a node with two children, and any node can be a child, so
    /// a tint, a mask or a whole tree goes in the diffuse slot. V cycles the operator over the
    /// same two inputs - brick and marble.
    /// </summary>
    public static void NodeArithmetic(MaterialStation s)
    {
        s.Clear();

        var op = s.Pick("Multiply", "Average", "Overlay", "Difference", "Screen", "Desaturate") switch
        {
            0 => BinaryOperator.Multiply,
            1 => BinaryOperator.Average,
            2 => BinaryOperator.Overlay,
            3 => BinaryOperator.Difference,
            4 => BinaryOperator.Screen,
            _ => BinaryOperator.Desaturate,
        };

        var brick = Recipes.Colour(s.Textures.Color("brick/brick_dif.png"));
        var marble = Recipes.Colour(s.Textures.Color("marble/marble_dif.png"));

        s.PlaceTrio(s.Material(Recipes.Mapped(new ComputeBinaryColor(brick, marble, op), glossiness: new ComputeFloat(0.5f))));
    }

    /// <summary>A unit cube with per-face normals and a colour at every corner, from the toolkit's MeshBuilder.</summary>
    private static Model ColouredCube(MaterialStation s)
    {
        using var builder = new MeshBuilder();

        builder.WithIndexType(IndexingType.Int16);

        var position = builder.WithPosition<Vector3>();
        var normal = builder.WithNormal<Vector3>();
        var colour = builder.WithColor<Color>();

        ReadOnlySpan<Vector3> normals = [Vector3.UnitX, -Vector3.UnitX, Vector3.UnitY, -Vector3.UnitY, Vector3.UnitZ, -Vector3.UnitZ];

        foreach (var n in normals)
        {
            // The face's own axes; its four corners go round clockwise seen from outside, which is the
            // front face in Stride as in Direct3D - the other way round and culling shows the insides
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

        return new Model { new Mesh { Draw = builder.ToMeshDraw(s.Game.GraphicsDevice), MaterialIndex = 0 } };
    }
}