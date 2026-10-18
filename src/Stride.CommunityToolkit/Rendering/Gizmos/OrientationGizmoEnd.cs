namespace Stride.CommunityToolkit.Rendering.Gizmos;

/// <summary>
/// One end of a world axis as the orientation gizmo draws it: which axis, which half, where it points
/// on the screen and how close to the camera it is.
/// </summary>
/// <param name="Axis">0 for X, 1 for Y, 2 for Z.</param>
/// <param name="Positive">Whether this is the positive half of the axis.</param>
/// <param name="Direction">
/// Where the end lies on the screen, per unit of axis length: X to the right, Y down, the length
/// between 0 (the axis points straight at or away from the camera) and 1 (it lies in the screen).
/// </param>
/// <param name="Depth">Between -1 (pointing away from the camera) and 1 (pointing at it).</param>
internal readonly record struct OrientationGizmoEnd(int Axis, bool Positive, Vector2 Direction, float Depth)
{
    /// <summary>The axis letter.</summary>
    internal string Label => Axis switch { 0 => "X", 1 => "Y", _ => "Z" };
}