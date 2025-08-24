using Box2D.NET;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace Example18_Box2DPhysics.Box2DPhysics.Events;

public struct ContactEventData
{
    public ContactEventType Type;
    public Entity EntityA;
    public Entity EntityB;
    public B2ShapeId ShapeIdA;
    public B2ShapeId ShapeIdB;
    public int ContactId; // Using int instead of B2ContactId for now
    public Vector2 Point;
    public Vector2 Normal;
    public float ApproachSpeed;
}