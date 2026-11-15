using Stride.CommunityToolkit.Effects.Picking;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.Instancing;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Shapes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;

namespace Stride.CommunityToolkit.GoldScenes.Scenes;

/// <summary>
/// GPU picking with no physics in the scene: five screen points are asked about in turn - a pillar,
/// a sphere, one cube of an instanced row, the ground and the sky - and each answer is drawn where
/// it landed: a billboard ring at the hit point in a colour per entity, with one extra ring per
/// instance index for the instanced cube, and a red cross on the screen where nothing was hit. A
/// white pixel ring marks every point asked. Pins the id pass through the forward effect, the
/// instance index, the two-frame readback and the depth-to-world reconstruction.
/// </summary>
internal sealed class PickingScene : IGoldScene
{
    private static readonly Vector2[] Points =
    [
        new(0.5f, 0.42f),   // the pillar
        new(0.22f, 0.62f),  // the sphere
        new(0.78f, 0.6f),   // the third cube of the instanced row
        new(0.5f, 0.88f),   // the ground
        new(0.5f, 0.08f),   // the sky
    ];

    private readonly PickResult?[] _results = new PickResult?[Points.Length];
    private GpuPicker _picker = null!;
    private ShapeBatch _shapes = null!;
    private int _frame;

    public SceneSetup Setup => GoldSetup.Perspective;

    public void Start(Game game, Scene scene)
    {
        game.SetCameraPosition(new Vector3(0f, 5f, 12f));
        game.SetCameraRotation(new Vector3(0f, -18f, 0f));
        game.AddInstancingSupport();

        GoldSetup.AddGround(game, scene);
        GoldSetup.AddPillar(game, scene, "Pillar", Vector3.Zero, 4f);

        var sphere = game.CreatePrimitive(Primitive3D.Sphere(1.6f), new PrimitiveEntityOptions
        {
            EntityName = "Sphere",
            Material = game.CreateMaterial(Color.MediumPurple, metalness: 0.3f, glossiness: 0.6f),
            Position = new Vector3(-4.5f, 0.8f, 1f),
        });

        sphere.Scene = scene;

        // A row of five cubes drawn as one instanced model, so a pick can name the instance
        var prototype = game.CreatePrimitive(Primitive3D.Cube(0.9f), new PrimitiveEntityOptions
        {
            EntityName = "Row",
            Material = game.CreateMaterial(Color.SteelBlue, metalness: 0.2f, glossiness: 0.5f),
        });

        var matrices = new Matrix[5];

        for (var i = 0; i < matrices.Length; i++)
        {
            matrices[i] = Matrix.Translation(3f + i * 1.1f, 0.45f, 2.5f - i * 0.6f);
        }

        var instancing = new InstancingUserArray();

        instancing.UpdateWorldMatrices(matrices);
        prototype.Add(new InstancingComponent { Type = instancing });
        prototype.Scene = scene;

        _shapes = game.AddShapeBatch(depthTest: false);
        _picker = game.AddGpuPicker();
    }

    public void Update(Game game, Scene scene, GameTime time)
    {
        // One point per frame, round and round; every answer is kept under the point it was for
        _picker.Request(Points[_frame++ % Points.Length]);

        if (_picker.Result is { } result)
        {
            _results[Array.IndexOf(Points, result.ScreenPosition)] = result;
        }

        _shapes.StrokeWidth = 2f;
        _shapes.Fill.Alpha = 0f;

        for (var i = 0; i < Points.Length; i++)
        {
            if (_results[i] is not { } answer) continue;

            if (answer is { Hit: true, WorldPosition: { } at, Entity: { } entity })
            {
                var colour = entity.Name switch
                {
                    "Pillar" => Color.LimeGreen,
                    "Sphere" => Color.Magenta,
                    "Row" => Color.Cyan,
                    _ => Color.Orange,
                };

                // One ring per instance index plus one, so the third cube shows three rings
                for (var ring = 0; ring <= answer.InstanceIndex; ring++)
                {
                    _shapes.DrawPixelRing(at, 10f + ring * 8f, colour);
                }
            }
            else
            {
                _shapes.Screen = true;

                var centre = Points[i] * _shapes.ScreenSize;

                _shapes.DrawPixelLine(new Vector3(centre.X - 14f, centre.Y - 14f, 0f), new Vector3(centre.X + 14f, centre.Y + 14f, 0f), 3f, Color.Red);
                _shapes.DrawPixelLine(new Vector3(centre.X - 14f, centre.Y + 14f, 0f), new Vector3(centre.X + 14f, centre.Y - 14f, 0f), 3f, Color.Red);
                _shapes.Screen = false;
            }
        }

        // Where the questions were asked, so the answers can be read against them
        _shapes.Screen = true;
        _shapes.StrokeWidth = 1.5f;

        foreach (var point in Points)
        {
            _shapes.DrawRing(new Vector3(point * _shapes.ScreenSize, 0f), Vector3.UnitZ, 6f, Color.White);
        }

        _shapes.Screen = false;
        _shapes.StrokeWidth = 1f;
        _shapes.Fill.Alpha = 0.45f;
    }
}