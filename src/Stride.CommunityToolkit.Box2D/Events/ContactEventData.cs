using Box2D.NET;
using Stride.Core.Mathematics;
using Stride.Engine;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Worlds;

namespace Stride.CommunityToolkit.Box2D;

/// <summary>
/// A contact event delivered to <see cref="IContactEventHandler"/> implementations: the two shapes involved, the
/// entities that own them when they have one, plus impact data for <see cref="ContactEventType.Hit"/> events.
/// </summary>
/// <remarks>
/// A body made without an entity, such as a ground from <c>CreateStaticBody(position)</c>, has a <see langword="null"/>
/// entity; its side is still identified by <see cref="BodyIdA"/> or <see cref="BodyIdB"/> and the shape id.
/// </remarks>
/// <param name="Type">The kind of contact event.</param>
/// <param name="EntityA">The entity owning the first shape, or <see langword="null"/> when its body has none.</param>
/// <param name="EntityB">The entity owning the second shape, or <see langword="null"/> when its body has none.</param>
/// <param name="ShapeIdA">The first shape involved.</param>
/// <param name="ShapeIdB">The second shape involved.</param>
/// <param name="Point">World-space contact point; zero for begin/end touch events.</param>
/// <param name="Normal">World-space contact normal; zero for begin/end touch events.</param>
/// <param name="ApproachSpeed">Approach speed of the impact; zero for begin/end touch events.</param>
public readonly record struct ContactEventData(
    ContactEventType Type,
    Entity? EntityA,
    Entity? EntityB,
    B2ShapeId ShapeIdA,
    B2ShapeId ShapeIdB,
    Vector2 Point,
    Vector2 Normal,
    float ApproachSpeed)
{
    /// <summary>The body the first shape belongs to; the default id when an end event names a shape destroyed in the step.</summary>
    public B2BodyId BodyIdA => b2Shape_IsValid(ShapeIdA) ? b2Shape_GetBody(ShapeIdA) : default;

    /// <summary>The body the second shape belongs to; the default id when an end event names a shape destroyed in the step.</summary>
    public B2BodyId BodyIdB => b2Shape_IsValid(ShapeIdB) ? b2Shape_GetBody(ShapeIdB) : default;
}