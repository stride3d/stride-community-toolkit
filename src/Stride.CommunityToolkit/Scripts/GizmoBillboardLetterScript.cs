using Stride.CommunityToolkit.Engine;
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
            _camera = this.GetCamera();

            return;
        }

        UpdateLetterRotation(_camera.Entity.Transform.Position);
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
}
