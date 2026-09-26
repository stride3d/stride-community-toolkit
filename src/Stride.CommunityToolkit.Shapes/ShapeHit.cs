using Stride.Core.Mathematics;

namespace Stride.CommunityToolkit.Shapes;

/// <summary>
/// A shape found under a screen position by <see cref="ShapeBatch.TryPick(Vector2, out ShapeHit, float)"/>
/// or <see cref="ShapeBatch.PickAll(Vector2, float)"/>.
/// </summary>
/// <param name="Tag">The <see cref="ShapeBatch.Tag"/> the shape was drawn with.</param>
/// <param name="Point">Where the pick lands on the shape: in world units, or in scaled pixels from the top left for a screen shape.</param>
/// <param name="Local">
/// The same point in the shape's own plane coordinates - relative to the position it was drawn at,
/// along the axes it was drawn with, in the units its vertices were given in - which is what a
/// board or a chart converts into its content. Zero for a space stroke, which has no plane.
/// </param>
/// <param name="Distance">
/// Signed distance from the point to the shape's outline, negative inside: world units in the
/// shape's plane, or pixels for a screen shape or a space stroke. The border counts as part of the
/// shape, so a pick on the outer half of the border has a small positive distance.
/// </param>
/// <param name="Depth">The point's depth in the view, 0 at the near plane and 1 at the far one, for ordering; -1 for a screen shape, which is over everything.</param>
public readonly record struct ShapeHit(object Tag, Vector3 Point, Vector2 Local, float Distance, float Depth);