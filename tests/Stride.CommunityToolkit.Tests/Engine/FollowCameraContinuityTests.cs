using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Scripts;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Engine;

/// <summary>
/// <see cref="Basic3DFollowCameraController"/> on a turning up: a target carried half way round a planet, with
/// <see cref="Basic3DFollowCameraController.Up"/> following it, keeps a camera whose ground direction turns
/// smoothly with the surface and stays in it. A flip of the view would show as a jump between one frame's
/// <see cref="Basic3DFollowCameraController.Forward"/> and the next.
/// </summary>
[Collection(GameExtensionsRunTests.Name)]
public class FollowCameraContinuityTests
{
    [Fact]
    public void Forward_FollowsATurningUpWithoutAJump()
    {
        using var game = new Game();
        const float radius = 10f;
        const int steps = 180;
        var smallestTurn = 1f;
        var largestTilt = 0f;
        var frames = 0;
        Entity? target = null;
        Basic3DFollowCameraController? follow = null;
        var lastForward = Vector3.Zero;

        game.Run(SceneSetup.None, start: scene =>
        {
            target = new Entity("target") { Transform = { Position = new Vector3(0f, radius, 0f) } };
            target.Scene = scene;

            var camera = new Entity("camera") { new CameraComponent() };

            camera.Transform.Position = new Vector3(0f, radius + 3f, 6f);
            follow = new Basic3DFollowCameraController { Target = target, TargetOffset = Vector3.Zero, FollowSmoothing = 0f };
            camera.Add(follow);
            camera.Scene = scene;
        }, update: (_, _) =>
        {
            frames++;

            if (frames < 2) return;

            // The target walks over the top of the planet and down the far side, through the point where up meets world +Z
            var angle = MathUtil.Pi * (frames - 2) / steps;
            var up = new Vector3(0f, MathF.Cos(angle), MathF.Sin(angle));

            target!.Transform.Position = up * radius;
            follow!.Up = up;

            if (frames > 3)
            {
                smallestTurn = MathF.Min(smallestTurn, Vector3.Dot(follow.Forward, lastForward));
                largestTilt = MathF.Max(largestTilt, MathF.Abs(Vector3.Dot(follow.Forward, up)));
            }

            lastForward = follow.Forward;

            if (frames == steps + 2) game.Exit();
        }, context: new GameContextHeadless());

        Assert.True(smallestTurn > 0.99f, $"the camera's ground direction jumped between two frames: the cosine of the turn fell to {smallestTurn}");
        Assert.True(largestTilt < 0.05f, $"the camera's ground direction left the ground: its component along up reached {largestTilt}");
    }
}