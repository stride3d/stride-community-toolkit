using Stride.Core.Mathematics;

namespace Stride.CommunityToolkit.Shapes;

/// <summary>
/// The ray through a point of the screen, which picking and the reference grid both cast.
/// </summary>
internal static class ScreenRay
{
    /// <summary>
    /// The ray through a screen position, normalised (0,0) top left to (1,1) bottom right, from the near plane
    /// into the scene.
    /// </summary>
    internal static Ray Through(Vector2 normalised, in Matrix inverseViewProjection)
    {
        var clip = new Vector3(normalised.X * 2f - 1f, 1f - normalised.Y * 2f, 0f);
        var near = Vector3.TransformCoordinate(clip, inverseViewProjection);

        clip.Z = 1f;

        var far = Vector3.TransformCoordinate(clip, inverseViewProjection);

        return new Ray(near, Vector3.Normalize(far - near));
    }
}