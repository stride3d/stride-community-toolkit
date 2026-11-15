using Stride.CommunityToolkit.DebugShapes;
using Stride.CommunityToolkit.Engine;
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
    private ImmediateDebugRenderSystem _debug = null!;

    public SceneSetup Setup => GoldSetup.Perspective;

    public void Start(Game game, Scene scene)
    {
        game.SetCameraPosition(new Vector3(0f, 5f, 14f));
        game.SetCameraRotation(new Vector3(0f, -14f, 0f));

        GoldSetup.AddGround(game, scene);

        // The extension shows the system in a Debug build only
        _debug = game.AddDebugShapes();
        _debug.Visible = true;
    }

    public void Update(Game game, Scene scene, GameTime time)
    {
        var tilt = Quaternion.RotationYawPitchRoll(0.6f, 0.3f, 0f);

        // Wireframe row at the back, solid row in front, one shape per primitive
        _debug.DrawSphere(new Vector3(-6f, 1f, -3f), 0.8f, Color.Red);
        _debug.DrawCube(new Vector3(-3f, 1f, -3f), new Vector3(1.4f, 1.4f, 1.4f), tilt, Color.Lime);
        _debug.DrawCapsule(new Vector3(0f, 1.2f, -3f), 2f, 0.5f, tilt, Color.Cyan);
        _debug.DrawCylinder(new Vector3(3f, 1f, -3f), 1.8f, 0.6f, default, Color.Yellow);
        _debug.DrawCone(new Vector3(6f, 1f, -3f), 1.8f, 0.7f, default, Color.Magenta);

        _debug.DrawSphere(new Vector3(-6f, 1f, 1f), 0.8f, Color.Red, solid: true);
        _debug.DrawCube(new Vector3(-3f, 1f, 1f), new Vector3(1.4f, 1.4f, 1.4f), tilt, Color.Lime, solid: true);
        _debug.DrawCapsule(new Vector3(0f, 1.2f, 1f), 2f, 0.5f, tilt, Color.Cyan, solid: true);
        _debug.DrawCylinder(new Vector3(3f, 1f, 1f), 1.8f, 0.6f, default, Color.Yellow, solid: true);
        _debug.DrawCone(new Vector3(6f, 1f, 1f), 1.8f, 0.7f, default, Color.Magenta, solid: true);

        // Flat and linear primitives, plus two drawn over everything
        _debug.DrawCircle(new Vector3(-4f, 0.05f, 4f), 1.2f, Quaternion.RotationX(-MathF.PI / 2f), Color.Orange);
        _debug.DrawQuad(new Vector3(0f, 0.05f, 4f), new Vector2(2f, 1.4f), Quaternion.RotationX(-MathF.PI / 2f), Color.White, solid: true);
        _debug.DrawLine(new Vector3(3f, 0.1f, 3f), new Vector3(7f, 2.5f, 5f), Color.LightBlue);
        _debug.DrawArrow(new Vector3(3f, 2f, 5f), new Vector3(6f, 0.5f, 3f), color: Color.HotPink);
        _debug.DrawBounds(new Vector3(-7f, 0f, 3f), new Vector3(-5f, 1.5f, 5f), color: Color.LightGreen);

        _debug.DrawSphere(new Vector3(-6f, 0.4f, 1f), 0.5f, Color.White, depthTest: false);
        _debug.DrawLine(new Vector3(-8f, 0.5f, -3f), new Vector3(8f, 0.5f, -3f), Color.White, depthTest: false);
    }
}