using Box2D.NET;
using Stride.Core.Mathematics;

namespace Stride.CommunityToolkit.Box2D;

/// <summary>
/// A raw hit from <see cref="PhysicsQueries2D"/>, of a ray or of a swept shape: the body and shape struck, where,
/// the surface normal there, and how far along the ray or the sweep it happened. No Stride <c>Entity</c> types, Box2D
/// identifiers and value types only; <see cref="RaycastHit"/> is the entity-resolved variant <see cref="Box2DSimulation"/> returns.
/// </summary>
/// <param name="BodyId">The body that was struck.</param>
/// <param name="ShapeId">The shape on that body that was struck.</param>
/// <param name="Point">World-space hit point.</param>
/// <param name="Normal">World-space surface normal at the hit.</param>
/// <param name="Fraction">How far along the ray or the sweep the hit occurred, 0 to 1.</param>
public readonly record struct QueryHit(
    B2BodyId BodyId,
    B2ShapeId ShapeId,
    Vector2 Point,
    Vector2 Normal,
    float Fraction);