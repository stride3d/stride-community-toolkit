using Box2D.NET;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace Stride.CommunityToolkit.Box2D;

/// <summary>
/// A contact event delivered to <see cref="IContactEventHandler"/> implementations: the two entities
/// and shapes involved, plus impact data for <see cref="ContactEventType.Hit"/> events.
/// </summary>
/// <param name="Type">The kind of contact event.</param>
/// <param name="EntityA">The entity owning the first shape.</param>
/// <param name="EntityB">The entity owning the second shape.</param>
/// <param name="ShapeIdA">The first shape involved.</param>
/// <param name="ShapeIdB">The second shape involved.</param>
/// <param name="Point">World-space contact point; zero for begin/end touch events.</param>
/// <param name="Normal">World-space contact normal; zero for begin/end touch events.</param>
/// <param name="ApproachSpeed">Approach speed of the impact; zero for begin/end touch events.</param>
public readonly record struct ContactEventData(
    ContactEventType Type,
    Entity EntityA,
    Entity EntityB,
    B2ShapeId ShapeIdA,
    B2ShapeId ShapeIdB,
    Vector2 Point,
    Vector2 Normal,
    float ApproachSpeed);