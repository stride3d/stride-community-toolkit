using Box2D.NET;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace Stride.CommunityToolkit.Box2D;

/// <summary>
/// A 2D raycast hit with the owning Stride entity resolved, as returned by
/// <see cref="Box2DSimulation.Raycast"/> and <see cref="Box2DSimulation.RaycastAll"/>.
/// See <see cref="QueryHit"/> for the raw variant, which carries no entity.
/// </summary>
/// <param name="Entity">The entity associated with the hit body, or null when the body has none.</param>
/// <param name="BodyId">The body hit by the ray.</param>
/// <param name="ShapeId">The specific shape on the body that was hit.</param>
/// <param name="Point">World-space intersection point.</param>
/// <param name="Normal">World-space surface normal at the hit.</param>
/// <param name="Distance">Distance from the ray origin to the hit.</param>
/// <param name="Fraction">Normalized fraction along the ray in range [0,1].</param>
public readonly record struct RaycastHit(
    Entity? Entity,
    B2BodyId BodyId,
    B2ShapeId ShapeId,
    Vector2 Point,
    Vector2 Normal,
    float Distance,
    float Fraction);