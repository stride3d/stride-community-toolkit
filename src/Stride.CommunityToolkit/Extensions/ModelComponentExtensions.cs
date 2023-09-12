namespace Stride.Engine;

public static class ModelComponentExtensions
{
    /// <summary>
    /// Gets the ModelComponents BoundingBox and calculates the Y height
    /// </summary>
    /// <param name="modelComponent"></param>
    /// <returns></returns>
    public static float GetMeshHeight(this ModelComponent modelComponent)
    {
        var boundingBox = modelComponent.Model.BoundingBox;
        var height = boundingBox.Maximum.Y - boundingBox.Minimum.Y;

        return height;
    }

    /// <summary>
    /// Gets the ModelComponents BoundingBox and calculates the Height, Width and Length
    /// </summary>
    /// <param name="modelComponent"></param>
    /// <returns></returns>
    public static Vector3 GetMeshHWL(this ModelComponent modelComponent)
    {
        var boundingBox = modelComponent.Model.BoundingBox;
        var height = boundingBox.Maximum.Y - boundingBox.Minimum.Y;
        var width = boundingBox.Maximum.X - boundingBox.Minimum.X;
        var length = boundingBox.Maximum.Z - boundingBox.Minimum.Z;

        return new Vector3(height, width, length);
    }
}