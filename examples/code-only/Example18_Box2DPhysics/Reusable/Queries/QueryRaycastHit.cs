using Box2D.NET;
using Stride.Core.Mathematics;

namespace Example18_Box2DPhysics.Reusable.Queries;

public static partial class PhysicsQueries2D
{
    /// <summary>
    /// Represents a raw raycast hit without any engine-specific references.
    /// </summary>
    public readonly record struct QueryRaycastHit(
        B2BodyId BodyId,
        B2ShapeId ShapeId,
        Vector2 Point,
        Vector2 Normal,
        float Fraction);
}