using Stride.CommunityToolkit.Effects.Picking;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.Instancing;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Shapes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Rendering;

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
    private GpuPicker? _picker;
    private ShapeBatch? _shapes;
    private int _frame;

    public void Start(Game game, Scene scene)
    {
        game.SetupBase3D();
        game.SetCameraPosition(new Vector3(0f, 5f, 12f));
        game.SetCameraRotation(new Vector3(0f, -18f, 0f));
        game.AddInstancingSupport();

        var ground = game.Create3DPrimitive(PrimitiveModelType.Cube, new Primitive3DEntityOptions
        {
            EntityName = "Ground",
            Material = game.CreateMaterial(new Color(38, 41, 47), metalness: 0.04f, glossiness: 0.25f),
            Size = new Vector3(30f, 0.5f, 30f),
            Position = new Vector3(0f, -0.25f, 0f),
        });

        ground.Scene = scene;

        var pillar = game.Create3DPrimitive(PrimitiveModelType.Cube, new Primitive3DEntityOptions
        {
            EntityName = "Pillar",
            Material = game.CreateMaterial(new Color(96, 103, 116), metalness: 0.1f, glossiness: 0.35f),
            Size = new Vector3(1.6f, 4f, 1.6f),
            Position = new Vector3(0f, 2f, 0f),
        });

        pillar.Scene = scene;

        var sphere = game.Create3DPrimitive(PrimitiveModelType.Sphere, new Primitive3DEntityOptions
        {
            EntityName = "Sphere",
            Material = game.CreateMaterial(Color.MediumPurple, metalness: 0.3f, glossiness: 0.6f),
            Size = new Vector3(1.6f),
            Position = new Vector3(-4.5f, 0.8f, 1f),
        });

        sphere.Scene = scene;

        // A row of five cubes drawn as one instanced model, so a pick can name the instance
        var prototype = game.Create3DPrimitive(PrimitiveModelType.Cube, new Primitive3DEntityOptions
        {
            EntityName = "Row",
            Material = game.CreateMaterial(Color.SteelBlue, metalness: 0.2f, glossiness: 0.5f),
            Size = new Vector3(0.9f),
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
        if (_picker is not { } picker || _shapes is not { } shapes) return;

        // One point per frame, round and round; every answer is kept under the point it was for
        picker.Request(Points[_frame++ % Points.Length]);

        if (picker.Result is { } result)
        {
            _results[Array.IndexOf(Points, result.ScreenPosition)] = result;
        }

        shapes.BorderWidth = 2f;
        shapes.Fill.Alpha = 0f;

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
                    shapes.DrawPixelRing(at, 10f + ring * 8f, colour);
                }
            }
            else
            {
                shapes.Screen = true;

                var centre = Points[i] * shapes.ScreenSize;

                shapes.DrawPixelLine(new Vector3(centre.X - 14f, centre.Y - 14f, 0f), new Vector3(centre.X + 14f, centre.Y + 14f, 0f), 3f, Color.Red);
                shapes.DrawPixelLine(new Vector3(centre.X - 14f, centre.Y + 14f, 0f), new Vector3(centre.X + 14f, centre.Y - 14f, 0f), 3f, Color.Red);
                shapes.Screen = false;
            }
        }

        // Where the questions were asked, so the answers can be read against them
        shapes.Screen = true;
        shapes.BorderWidth = 1.5f;

        foreach (var point in Points)
        {
            shapes.DrawRing(new Vector3(point * shapes.ScreenSize, 0f), Vector3.UnitZ, 6f, Color.White);
        }

        shapes.Screen = false;
        shapes.BorderWidth = 3f;
        shapes.Fill.Alpha = 0.45f;
    }
}