using Stride.CommunityToolkit.Engine;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace CubeCollapse.Components;

/// <summary>
/// Turns its entity about the Y axis to face the camera every frame.
/// </summary>
/// <remarks>
/// For static presentation objects such as the game-over menu, which must stay readable while the
/// player orbits. Physics bodies never get this - fighting the solver over orientation every frame
/// is exactly what the falling letters avoid by facing the camera once, at spawn, instead.
/// </remarks>
public class FaceCameraScript : SyncScript
{
    private CameraComponent? _camera;

    /// <inheritdoc />
    public override void Update()
    {
        _camera ??= SceneSystem.SceneInstance.RootScene.GetCamera();

        if (_camera is null) return;

        var direction = _camera.Entity.Transform.Position - Entity.Transform.Position;

        direction.Y = 0;

        // The lettering's front is +Z, so face away from the camera to show it; Y is zeroed above, so
        // this is a yaw only and the letters stay upright
        Entity.Transform.FaceDirection(-direction);
    }
}