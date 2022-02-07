namespace Stride.GameDefaults.Extensions;

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
}