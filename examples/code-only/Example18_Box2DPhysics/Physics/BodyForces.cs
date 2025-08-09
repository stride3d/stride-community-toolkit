using Box2D.NET;
using Stride.Core.Mathematics;
using static Box2D.NET.B2Bodies;

namespace Example18_Box2DPhysics.Physics;

/// <summary>
/// Force, impulse, and velocity helpers applied to bodies.
/// </summary>
public static class BodyForces
{
    public static void ApplyImpulse(B2BodyId bodyId, Vector2 impulse)
    {
        var b2Impulse = new Box2D.NET.B2Vec2(impulse.X, impulse.Y);
        b2Body_ApplyLinearImpulseToCenter(bodyId, b2Impulse, true);
    }

    public static void ApplyImpulseAtPoint(B2BodyId bodyId, Vector2 impulse, Vector2 point)
    {
        var b2Impulse = new Box2D.NET.B2Vec2(impulse.X, impulse.Y);
        var b2Point = new Box2D.NET.B2Vec2(point.X, point.Y);
        b2Body_ApplyLinearImpulse(bodyId, b2Impulse, b2Point, true);
    }

    public static void SetVelocity(B2BodyId bodyId, Vector2 velocity)
    {
        var v = new Box2D.NET.B2Vec2(velocity.X, velocity.Y);
        b2Body_SetLinearVelocity(bodyId, v);
    }

    public static Vector2 GetVelocity(B2BodyId bodyId)
    {
        var v = b2Body_GetLinearVelocity(bodyId);
        return new Vector2(v.X, v.Y);
    }
}
