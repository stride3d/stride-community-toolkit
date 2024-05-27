using Stride.Engine;

namespace Stride.CommunityToolkit.Scripts;

public class GizmoBillboardLetterScript : SyncScript
{
    public int DefaultRotation { get; set; } = 90;

    private CameraComponent? _camera;

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
