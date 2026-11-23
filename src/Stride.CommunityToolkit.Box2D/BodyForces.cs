using Box2D.NET;
using Stride.Core.Mathematics;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2MathFunction;

namespace Stride.CommunityToolkit.Box2D;

/// <summary>
/// Force, impulse, velocity and teleport helpers applied to bodies by id, for a body without an entity;
/// <see cref="Box2DBodyComponent"/> has the same for an entity's body.
/// </summary>
public static class BodyForces
{
    /// <summary>
    /// Moves a body to a pose and stops it there, awake, as a reset does: no velocity carries over into the new position.
    /// </summary>
    /// <param name="bodyId">The target body.</param>
    /// <param name="position">The world position.</param>
    /// <param name="rotation">The rotation in radians about the Z axis.</param>
    public static void TeleportAtRest(B2BodyId bodyId, Vector2 position, float rotation = 0f)
    {
        b2Body_SetTransform(bodyId, position.ToB2Vec2(), b2MakeRot(rotation));
        b2Body_SetLinearVelocity(bodyId, b2Vec2_zero);
        b2Body_SetAngularVelocity(bodyId, 0f);
        b2Body_SetAwake(bodyId, true);
    }

    /// <summary>
    /// Applies a linear impulse to the body's center of mass, waking it.
    /// </summary>
    /// <param name="bodyId">The target body.</param>
    /// <param name="impulse">The impulse in newton-seconds.</param>
    public static void ApplyImpulse(B2BodyId bodyId, Vector2 impulse)
        => b2Body_ApplyLinearImpulseToCenter(bodyId, impulse.ToB2Vec2(), true);

    /// <summary>
    /// Applies a linear impulse at a world-space point, waking the body.
    /// </summary>
    /// <param name="bodyId">The target body.</param>
    /// <param name="impulse">The impulse in newton-seconds.</param>
    /// <param name="point">World-space application point.</param>
    public static void ApplyImpulseAtPoint(B2BodyId bodyId, Vector2 impulse, Vector2 point)
        => b2Body_ApplyLinearImpulse(bodyId, impulse.ToB2Vec2(), point.ToB2Vec2(), true);

    /// <summary>
    /// Sets the body's linear velocity.
    /// </summary>
    /// <param name="bodyId">The target body.</param>
    /// <param name="velocity">The new velocity.</param>
    public static void SetVelocity(B2BodyId bodyId, Vector2 velocity)
        => b2Body_SetLinearVelocity(bodyId, velocity.ToB2Vec2());

    /// <summary>
    /// Gets the body's linear velocity.
    /// </summary>
    /// <param name="bodyId">The target body.</param>
    /// <returns>The current velocity.</returns>
    public static Vector2 GetVelocity(B2BodyId bodyId) => b2Body_GetLinearVelocity(bodyId).ToVector2();
}