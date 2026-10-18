namespace Stride.CommunityToolkit.Rendering.Gizmos;

/// <summary>
/// The projection behind <see cref="OrientationGizmoRenderer"/>: the camera's rotation turned into
/// six screen directions, ordered so the ends behind the centre are drawn first.
/// </summary>
internal static class OrientationGizmoMath
{
    /// <summary>The number of ends: both halves of three axes.</summary>
    internal const int EndCount = 6;

    /// <summary>
    /// Fills <paramref name="ends"/> from the camera's view matrix, farthest end first.
    /// </summary>
    /// <param name="view">The camera's view matrix. Only its rotation is read.</param>
    /// <param name="ends">Room for <see cref="EndCount"/> ends.</param>
    internal static void Project(in Matrix view, Span<OrientationGizmoEnd> ends)
    {
        if (ends.Length < EndCount) throw new ArgumentException($"Room for {EndCount} ends is needed.", nameof(ends));

        for (var axis = 0; axis < 3; axis++)
        {
            var world = axis switch { 0 => Vector3.UnitX, 1 => Vector3.UnitY, _ => Vector3.UnitZ };

            // View space: X right, Y up, the camera looking down -Z; so an axis with a positive view Z
            // points at the camera. The screen has Y down, hence the flip.
            var viewSpace = Vector3.TransformNormal(world, view);
            var direction = new Vector2(viewSpace.X, -viewSpace.Y);

            ends[2 * axis] = new OrientationGizmoEnd(axis, true, direction, viewSpace.Z);
            ends[2 * axis + 1] = new OrientationGizmoEnd(axis, false, -direction, -viewSpace.Z);
        }

        // Six items: an insertion sort, with no allocation
        for (var i = 1; i < EndCount; i++)
        {
            var end = ends[i];
            var j = i - 1;

            while (j >= 0 && ends[j].Depth > end.Depth)
            {
                ends[j + 1] = ends[j];
                j--;
            }

            ends[j + 1] = end;
        }
    }
}