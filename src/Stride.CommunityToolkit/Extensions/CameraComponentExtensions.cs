using Stride.Core.Mathematics;
using Stride.Engine;

namespace Stride.CommunityToolkit.Extensions;

public static class CameraComponentExtensions
{
    /// <summary>
    /// Returns near and far vector based on a ray going from camera through a screen point. The ray is in world space, starting on the near plane of the camera and going through position's (x,y) pixel coordinates on the screen.
    /// </summary>
    /// <param name="cameraComponent"></param>
    /// <param name="mousePosition"></param>
    /// <returns></returns>
    public static (Vector4 VectorNear, Vector4 VectorFar) ScreenPointToRay(this CameraComponent cameraComponent, Vector2 mousePosition)
    {
        var validMousePosition = mousePosition;

        var invertedMatrix = Matrix.Invert(cameraComponent.ViewProjectionMatrix);

        Vector3 position;
        position.X = validMousePosition.X * 2f - 1f;
        position.Y = 1f - validMousePosition.Y * 2f;
        position.Z = 0f;

        Vector4 vectorNear = Vector3.Transform(position, invertedMatrix);
        vectorNear /= vectorNear.W;

        position.Z = 1f;

        Vector4 vectorFar = Vector3.Transform(position, invertedMatrix);
        vectorFar /= vectorFar.W;

        return (vectorNear, vectorFar);
	}
	public static Vector3 LogicDirectionToWorldDirection(this CameraComponent camera, Vector2 logicDirection, Vector3 upVector)
	{
		camera.Update();
		var inverseView = Matrix.Invert(camera.ViewMatrix);

		var forward = Vector3.Cross(upVector, inverseView.Right);
		forward.Normalize();

		var right = Vector3.Cross(forward, upVector);
		var worldDirection = forward * logicDirection.Y + right * logicDirection.X;
		worldDirection.Normalize();
		return worldDirection;
	}

	public static Vector3 LogicDirectionToWorldDirection(this CameraComponent camera, Vector2 logicDirection)
	{
		camera.Update();
		var upVector = Vector3.UnitY;
		var inverseView = Matrix.Invert(camera.ViewMatrix);

		var forward = Vector3.Cross(upVector, inverseView.Right);
		forward.Normalize();

		var right = Vector3.Cross(forward, upVector);
		var worldDirection = forward * logicDirection.Y + right * logicDirection.X;
		worldDirection.Normalize();
		return worldDirection;
	}
}