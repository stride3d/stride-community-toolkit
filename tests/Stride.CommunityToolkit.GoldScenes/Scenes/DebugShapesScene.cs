using Stride.CommunityToolkit.DebugShapes.Code;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;

namespace Stride.CommunityToolkit.GoldScenes.Scenes;

/// <summary>
/// Every DebugShapes primitive, solid and wireframe, in a fixed row, with two of them drawn without
/// the depth test - the colour path through the primitive shader has broken once already.
/// </summary>
internal sealed class DebugShapesScene : IGoldScene
{
    private ImmediateDebugRenderSystem? _debug;

    public void Start(Game game, Scene scene)
    {
        game.SetupBase3D();
        game.SetCameraPosition(new Vector3(0f, 5f, 14f));
        game.SetCameraRotation(new Vector3(0f, -14f, 0f));

        var ground = game.Create3DPrimitive(PrimitiveModelType.Cube, new Primitive3DEntityOptions
        {
            EntityName = "Ground",
            Material = game.CreateMaterial(new Color(38, 41, 47), metalness: 0.04f, glossiness: 0.25f),
            Size = new Vector3(30f, 0.5f, 30f),
            Position = new Vector3(0f, -0.25f, 0f),
        });

        ground.Scene = scene;

        game.AddDebugShapes();

        _debug = game.Services.GetService<ImmediateDebugRenderSystem>();

        if (_debug is not null)
        {
            _debug.Visible = true;
        }
    }

    public void Update(Game game, Scene scene, GameTime time)
    {
        if (_debug is not { } debug) return;

        var tilt = Quaternion.RotationYawPitchRoll(0.6f, 0.3f, 0f);

        // Wireframe row at the back, solid row in front, one shape per primitive
        debug.DrawSphere(new Vector3(-6f, 1f, -3f), 0.8f, Color.Red);
        debug.DrawCube(new Vector3(-3f, 1f, -3f), new Vector3(1.4f, 1.4f, 1.4f), tilt, Color.Lime);
        debug.DrawCapsule(new Vector3(0f, 1.2f, -3f), 2f, 0.5f, tilt, Color.Cyan);
        debug.DrawCylinder(new Vector3(3f, 1f, -3f), 1.8f, 0.6f, default, Color.Yellow);
        debug.DrawCone(new Vector3(6f, 1f, -3f), 1.8f, 0.7f, default, Color.Magenta);

        debug.DrawSphere(new Vector3(-6f, 1f, 1f), 0.8f, Color.Red, solid: true);
        debug.DrawCube(new Vector3(-3f, 1f, 1f), new Vector3(1.4f, 1.4f, 1.4f), tilt, Color.Lime, solid: true);
        debug.DrawCapsule(new Vector3(0f, 1.2f, 1f), 2f, 0.5f, tilt, Color.Cyan, solid: true);
        debug.DrawCylinder(new Vector3(3f, 1f, 1f), 1.8f, 0.6f, default, Color.Yellow, solid: true);
        debug.DrawCone(new Vector3(6f, 1f, 1f), 1.8f, 0.7f, default, Color.Magenta, solid: true);

        // Flat and linear primitives, plus two drawn over everything
        debug.DrawCircle(new Vector3(-4f, 0.05f, 4f), 1.2f, Quaternion.RotationX(-MathF.PI / 2f), Color.Orange);
        debug.DrawQuad(new Vector3(0f, 0.05f, 4f), new Vector2(2f, 1.4f), Quaternion.RotationX(-MathF.PI / 2f), Color.White, solid: true);
        debug.DrawLine(new Vector3(3f, 0.1f, 3f), new Vector3(7f, 2.5f, 5f), Color.LightBlue);
        debug.DrawArrow(new Vector3(3f, 2f, 5f), new Vector3(6f, 0.5f, 3f), color: Color.HotPink);
        debug.DrawBounds(new Vector3(-7f, 0f, 3f), new Vector3(-5f, 1.5f, 5f), color: Color.LightGreen);

        debug.DrawSphere(new Vector3(-6f, 0.4f, 1f), 0.5f, Color.White, depthTest: false);
        debug.DrawLine(new Vector3(-8f, 0.5f, -3f), new Vector3(8f, 0.5f, -3f), Color.White, depthTest: false);
    }
}