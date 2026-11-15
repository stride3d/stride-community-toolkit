using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Rendering;

namespace Stride.CommunityToolkit.GoldScenes.Scenes;

/// <summary>
/// What the gold scenes share: the two bare setups, the dark slab most of them stand on and the grey
/// pillar several of them hide shapes behind. Each scene stays a picture of one feature; this is only
/// the furniture.
/// </summary>
internal static class GoldSetup
{
    private static readonly Color GroundColour = new(38, 41, 47);
    private static readonly Color PillarColour = new(96, 103, 116);

    /// <summary>A perspective camera and a light, no ground and no sky.</summary>
    internal static SceneSetup Perspective => new SceneSetup3D { Camera = CameraSetup.Perspective(), Ground = null, Skybox = null };

    /// <summary>An orthographic camera over a dark clear colour, no ground; <paramref name="size"/> is the height of the view in world units.</summary>
    internal static SceneSetup Orthographic(float size)
        => new SceneSetup2D { ClearColor = new Color(24, 26, 32), Camera = CameraSetup.Orthographic(orthographicSize: size), Ground = null };

    /// <summary>A square slab half a unit thick, its top at y = 0, dark grey unless a colour is given.</summary>
    internal static void AddGround(Game game, Scene scene, float size = 30f, Color? colour = null, float metalness = 0.04f, float glossiness = 0.25f)
    {
        var ground = game.CreatePrimitive(Primitive3D.Box(new Vector3(size, 0.5f, size)), new PrimitiveEntityOptions
        {
            EntityName = "Ground",
            Material = game.CreateMaterial(colour ?? GroundColour, metalness: metalness, glossiness: glossiness),
            Position = new Vector3(0f, -0.25f, 0f),
        });

        ground.Scene = scene;
    }

    /// <summary>A grey box standing on the ground at the x and z of <paramref name="at"/>.</summary>
    internal static Entity AddPillar(Game game, Scene scene, string name, Vector3 at, float height)
    {
        var pillar = game.CreatePrimitive(Primitive3D.Box(new Vector3(1.6f, height, 1.6f)), new PrimitiveEntityOptions
        {
            EntityName = name,
            Material = PillarMaterial(game),
            Position = new Vector3(at.X, height * 0.5f, at.Z),
        });

        pillar.Scene = scene;

        return pillar;
    }

    /// <summary>The grey of the pillars, for a scene that stands something else in their place.</summary>
    internal static Material PillarMaterial(Game game) => game.CreateMaterial(PillarColour, metalness: 0.1f, glossiness: 0.35f);
}