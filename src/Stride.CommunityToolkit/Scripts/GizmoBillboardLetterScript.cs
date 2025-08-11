using Stride.Engine;

namespace Stride.CommunityToolkit.Scripts;

/// <summary>
/// Keeps a gizmo letter entity oriented toward the camera with an optional additional Y-axis rotation.
/// </summary>
public class GizmoBillboardLetterScript : SyncScript
{
    /// <summary>
    /// Additional yaw rotation in degrees applied after facing the camera.
    /// </summary>
    public int DefaultRotation { get; set; } = 90;

    private CameraComponent? _camera;

    /// <summary>
    /// Per-frame update to lazily acquire camera and orient the letter.
    /// </summary>
    public override void Update()
    {
        if (_camera is null)
        {
            _camera = GetGCCamera();
        }
        else
        {
            UpdateLetterRotation(_camera.Entity.Transform.Position);
        }
    }

    /// <summary>
    /// Rotates the entity so it faces the camera (billboarding) plus <see cref="DefaultRotation"/>.
    /// </summary>
    public void UpdateLetterRotation(Vector3 cameraPosition)
    {
        var directionToCamera = cameraPosition - Entity.Transform.Position;

        directionToCamera.Y = 0;
        directionToCamera.Normalize();

        var upDirection = Vector3.UnitY;

        var lookAtRotation = Quaternion.LookRotation(directionToCamera, upDirection);

        var additionalRotation = Quaternion.RotationAxis(Vector3.UnitY, MathUtil.DegreesToRadians(DefaultRotation));

        Entity.Transform.Rotation = lookAtRotation * additionalRotation;
    }

    // This is same as in our extension but to avoid circular dependency we have to copy it here
    /// <summary>
    /// Attempts to find the primary camera named "Main" in the graphics compositor.
    /// </summary>
    public CameraComponent? GetGCCamera()
    {
        foreach (var sceneCamera in SceneSystem.GraphicsCompositor.Cameras)
        {
            if (sceneCamera.Name == "Main")
            {
                return sceneCamera.Camera;
            }
        }

        return null;
    }
}
