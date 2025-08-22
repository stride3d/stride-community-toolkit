using Box2D.NET;
using Stride.Core.Mathematics;

namespace Example18_Box2DPhysics.Box2DPhysics.Core.Results;

/// <summary>
/// Represents a raw 2D raycast hit returned by <see cref="PhysicsQueries2D"/>.
/// Engine-agnostic: contains only Box2D identifiers and value types so it can be lifted
/// into a future reusable Box2D toolkit library without changes.
/// </summary>
/// <param name="BodyId">The body hit by the ray.</param>
/// <param name="ShapeId">The specific shape on the body that was hit.</param>
/// <param name="Point">World-space intersection point.</param>
/// <param name="Normal">World-space surface normal at the hit.</param>
/// <param name="Fraction">Normalized fraction along the ray in range [0,1].</param>
public readonly record struct QueryRaycastHit(
    B2BodyId BodyId,
    B2ShapeId ShapeId,
    Vector2 Point,
    Vector2 Normal,
    float Fraction);