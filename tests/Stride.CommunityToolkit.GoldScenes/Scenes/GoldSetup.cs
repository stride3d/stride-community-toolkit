using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace Stride.CommunityToolkit.GoldScenes.Scenes;

/// <summary>
/// What the gold scenes share: the two bare setups, the dark slab most of them stand on, the grey
/// pillar several of them hide shapes behind, and the 2D camera's zoom. Each scene stays a picture
/// of one feature; this is only the furniture.
/// </summary>
internal static class GoldSetup
{
    private static readonly Color GroundColour = new(38, 41, 47);
    private static readonly Color PillarColour = new(96, 103, 116);

    /// <summary>A perspective camera and a light, no ground and no sky.</summary>
    public static SceneSetup Perspective => new SceneSetup3D { Camera = CameraSetup.Perspective(), Ground = null, Skybox = null };

    /// <summary>An orthographic camera over a dark clear colour, no ground.</summary>
    public static SceneSetup Orthographic => new SceneSetup2D { ClearColor = new Color(24, 26, 32), Camera = CameraSetup.Orthographic(), Ground = null };

    /// <summary>Zooms the 2D camera: the half-height of the view in world units.</summary>
    public static void SetOrthographicSize(Scene scene, float size)
    {
        var camera = scene.GetCamera() ?? throw new InvalidOperationException("The 2D scene has no camera.");

        camera.OrthographicSize = size;
    }

    /// <summary>A square slab half a unit thick, its top at y = 0, in the default dark grey.</summary>
    public static void AddGround(Game game, Scene scene, float size = 30f) => AddGround(game, scene, size, GroundColour, metalness: 0.04f, glossiness: 0.25f);

    /// <summary>A square slab half a unit thick, its top at y = 0.</summary>
    public static void AddGround(Game game, Scene scene, float size, Color colour, float metalness, float glossiness)
    {
        var ground = game.CreatePrimitive(Primitive3D.Box(new Vector3(size, 0.5f, size)), new PrimitiveEntityOptions
        {
            EntityName = "Ground",
            Material = game.CreateMaterial(colour, metalness: metalness, glossiness: glossiness),
            Position = new Vector3(0f, -0.25f, 0f),
        });

        ground.Scene = scene;
    }

    /// <summary>A grey box standing on the ground at the x and z of <paramref name="at"/>.</summary>
    public static Entity AddPillar(Game game, Scene scene, string name, Vector3 at, float height)
    {
        var pillar = game.CreatePrimitive(Primitive3D.Box(new Vector3(1.6f, height, 1.6f)), new PrimitiveEntityOptions
        {
            EntityName = name,
            Material = game.CreateMaterial(PillarColour, metalness: 0.1f, glossiness: 0.35f),
            Position = new Vector3(at.X, height * 0.5f, at.Z),
        });

        pillar.Scene = scene;

        return pillar;
    }
}