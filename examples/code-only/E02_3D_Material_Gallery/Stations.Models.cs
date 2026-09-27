using Stride.CommunityToolkit.Rendering;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Graphics.GeometricPrimitives;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;
using Stride.Rendering.ProceduralModels;

namespace E02_3D_Material_Gallery;

// The features that reach past the pixel: light under the surface, vertices moved, triangles
// added, a material over a material, and a material drawn over a whole model as a highlight.
public static class ModelStations
{
    /// <summary>
    /// Displacement: the ripple height map from the runtime-textures station moves the vertices
    /// along their normals at the vertex stage, on a dense plane and a dense sphere - real geometry,
    /// so the silhouette changes and the shadow with it. The same map's normal map lights the
    /// bumps. V cycles the intensity.
    /// </summary>
    public static void Displacement(MaterialStation s)
    {
        s.Clear();

        var intensity = s.Pick("intensity 0.15", "intensity 0.35", "none") switch { 0 => 0.15f, 1 => 0.35f, _ => 0f };
        var device = s.Game.GraphicsDevice;
        var heights = s.Textures.Generated("ripples", () => RuntimeTextures.Ripples(device));
        var normals = s.Textures.Generated("ripple-normals", () => RuntimeTextures.NormalFromHeight(device, RuntimeTextures.RipplePixels(), 256));

        var descriptor = Recipes.Mapped(new ComputeColor(new Color(90, 140, 170)), glossiness: new ComputeFloat(0.6f), normal: Recipes.Colour(normals));

        if (intensity > 0f)
        {
            descriptor.Attributes.Displacement = new MaterialDisplacementMapFeature
            {
                DisplacementMap = Recipes.Scalar(heights),
                Intensity = new ComputeFloat(intensity),
                ScaleAndBias = true,
                Stage = DisplacementMapStage.Vertex,
            };
        }

        var material = s.Material(descriptor);

        // Dense meshes, or there are no vertices to move: 64 segments where the primitives default to far fewer
        // No shadow casting: the shadow pass draws the undisplaced mesh, and the displaced surface then
        // sits behind its own shadow-map depth and shades itself black
        s.PlaceModel(new PlaneProceduralModel { Size = new Vector2(3.2f), Tessellation = new Int2(96), Normal = NormalDirection.UpZ }.Generate(s.Game.Services), material, new Vector3(-2.2f, 1.6f, 0f), castShadows: false);
        s.PlaceModel(new SphereProceduralModel { Radius = 1.1f, Tessellation = 96 }.Generate(s.Game.Services), material, new Vector3(2f, 1.2f, 0f), castShadows: false);
    }

    /// <summary>
    /// Tessellation: the GPU splits every triangle into more and PN-triangles bend the new ones to
    /// the vertex normals, so a coarse sphere and the teapot come out round with no new mesh.
    /// Needs feature level 11, which this gallery asks for at start-up. V cycles PN, flat (more
    /// triangles, same shape) and none, on the same five-segment sphere. The models cast no shadow:
    /// the engine's shadow pass binds a hull-shader constant buffer 32 bytes short for a tessellated
    /// material and the debug layer reports it every frame (noted upstream).
    /// </summary>
    public static void Tessellation(MaterialStation s)
    {
        s.Clear();

        var descriptor = Recipes.Pbr(new Color(200, 200, 205), glossiness: 0.75f, metalness: 0f);

        descriptor.Attributes.Tessellation = s.Pick("PN triangles", "flat", "none") switch
        {
            0 => new MaterialTessellationPNFeature { TriangleSize = 3f, AdjacentEdgeAverage = true },
            1 => new MaterialTessellationFlatFeature { TriangleSize = 3f },
            _ => null,
        };

        var material = s.Material(descriptor);

        s.PlaceModel(new SphereProceduralModel { Radius = 1.4f, Tessellation = 5 }.Generate(s.Game.Services), material, new Vector3(-2.4f, 1.5f, 0f), castShadows: false);
        s.PlaceModel(new TeapotProceduralModel { Size = 2.4f, Tessellation = 3 }.Generate(s.Game.Services), material, new Vector3(2.2f, 0.4f, 0f), castShadows: false);
    }

    /// <summary>
    /// Overrides: what applies to a whole material after its features - a UV scale that tiles every
    /// map at once, and the cull mode, which decides which side of a face is drawn. Front culling
    /// on a cube shows its inside, which is what a skybox or a room interior wants.
    /// </summary>
    public static void Overrides(MaterialStation s)
    {
        s.Clear();

        var (tiling, cull) = s.Pick("UV scale 1, cull back", "UV scale 3, cull back", "UV scale 3, cull front") switch
        {
            0 => (1f, CullMode.Back),
            1 => (3f, CullMode.Back),
            _ => (3f, CullMode.Front),
        };

        var descriptor = Recipes.Mapped(
            Recipes.Colour(s.Textures.Color("rooftile/rooftile_dif.png")),
            glossiness: Recipes.Scalar(s.Textures.Data("rooftile/rooftile_gls.png")),
            normal: Recipes.Colour(s.Textures.Normal("rooftile/rooftile_nml.png")),
            occlusion: Recipes.Scalar(s.Textures.Data("rooftile/rooftile_AO.png")));

        descriptor.Attributes.Overrides.UVScale = new Vector2(tiling);
        descriptor.Attributes.CullMode = cull;

        s.PlaceTrio(s.Material(descriptor));
    }

    /// <summary>
    /// Layers: a whole material laid over another and mixed by a blend map. The base is the pack's
    /// iron; the layer is its paint or its rust, each a material of its own, and the mask says
    /// where the top one shows. This is how the pack's painted and rusted iron are made, and it
    /// composes: V cycles paint, rust and both.
    /// </summary>
    public static void Layers(MaterialStation s)
    {
        s.Clear();

        var which = s.Pick("paint over iron", "rust over iron", "rust over paint over iron");
        var descriptor = PackMaterials.Iron(s.Textures);

        if (which != 1) descriptor.Layers.Add(new MaterialBlendLayer { Material = s.Material(PackMaterials.IronPaint(s.Textures)), BlendMap = Recipes.Scalar(s.Textures.Data("iron_blend/paint/iron_paint_msk.png")) });
        if (which != 0) descriptor.Layers.Add(new MaterialBlendLayer { Material = s.Material(PackMaterials.IronRust(s.Textures)), BlendMap = Recipes.Scalar(s.Textures.Data("iron_blend/rust/rust_msk.png")) });

        s.PlaceTrio(s.Material(descriptor));
    }

    /// <summary>
    /// A highlight with no render feature: the toolkit's <c>HighlightShell</c> draws the model again a
    /// little larger, every slot filled with <c>MaterialDescriptors.Highlight</c> - a constant vertex
    /// displacement, an emissive glow, a faint blend and no culling - and moves from shape to shape the
    /// way it would follow the mouse. The cube shows the technique's seam: its faces have normals of
    /// their own, so they move apart at the corners, plainly on the thick variation. V cycles the colour
    /// and the thickness.
    /// </summary>
    public static void HighlightShellStation(MaterialStation s)
    {
        s.Clear();

        var (colour, inflate) = s.Pick("cyan", "gold", "thick, to show the seams") switch
        {
            0 => (Color.Cyan, 0.02f),
            1 => (Color.Gold, 0.02f),
            _ => (Color.Cyan, 0.08f),
        };

        var grey = s.Material(Recipes.Pbr(new Color(150, 150, 155), glossiness: 0.5f, metalness: 0f));
        var sphere = s.Place(PrimitiveModelType.Sphere, grey, MaterialStation.SphereSpot, new Vector3(1f));
        var cube = s.Place(PrimitiveModelType.Cube, grey, MaterialStation.CubeSpot, new Vector3(1.8f));
        var teapot = s.Place(PrimitiveModelType.Teapot, grey, MaterialStation.TeapotSpot, new Vector3(2.4f));
        var shell = new HighlightShell(s.Material(MaterialDescriptors.Highlight(colour, inflate: inflate)));

        s.State = new ShellState(shell, [sphere.Get<ModelComponent>()!, cube.Get<ModelComponent>()!, teapot.Get<ModelComponent>()!]);
    }

    /// <summary>The shell on each shape in turn, a second and a half each; showing the same one again costs nothing.</summary>
    public static void HighlightShellUpdate(MaterialStation s)
    {
        if (s.State is not ShellState state) return;

        state.Shell.Show(state.Targets[(int)(s.Seconds / 1.5f) % state.Targets.Count]);
    }

    private sealed record ShellState(HighlightShell Shell, IReadOnlyList<ModelComponent> Targets);
}