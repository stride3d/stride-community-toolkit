using Box2D.NET;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Worlds;

namespace Stride.CommunityToolkit.Box2D;

/// <summary>
/// Component that ties a Stride entity to a Box2D body: creation-time configuration plus live
/// velocity, force and impulse access once <see cref="BodyId"/> refers to a created body.
/// </summary>
// DataContract is what makes the component usable from Game Studio at all; the body itself is
// created by the simulation from code, so in the editor this is the authoring data only.
[DataContract("Box2DBodyComponent")]
[Display("Box2D Body")]
[ComponentCategory("Physics")]
public class Box2DBodyComponent : EntityComponent
{
    /// <summary>The Box2D body backing this component. Default (invalid) until the body is created.</summary>
    [DataMemberIgnore]
    public B2BodyId BodyId { get; set; }

    /// <summary>The body type used when the body is created.</summary>
    public B2BodyType BodyType { get; set; } = B2BodyType.b2_dynamicBody;

    /// <summary>Collision group used with <see cref="Box2DCollisionMatrix"/>-style filtering.</summary>
    public int CollisionGroup { get; set; }

    /// <summary>Whether shapes created for this body should be sensors (detect contacts, no collision response).</summary>
    public bool IsSensor { get; set; }

    // Creation-time material configuration: applied when the body's shapes are created;
    // changing these later does not affect an existing body.

    /// <summary>Mass density used when shapes are created.</summary>
    public float Mass { get; set; } = 1.0f;

    /// <summary>Friction coefficient used when shapes are created.</summary>
    public float Friction { get; set; } = 0.3f;

    /// <summary>Restitution (bounciness) used when shapes are created.</summary>
    public float Restitution { get; set; }

    /// <summary>Linear damping used when the body is created.</summary>
    public float LinearDamping { get; set; }

    /// <summary>Angular damping used when the body is created.</summary>
    public float AngularDamping { get; set; }

    /// <summary>
    /// The body's linear velocity. Reads return zero and writes are ignored while <see cref="BodyId"/>
    /// is not a valid body.
    /// </summary>
    [DataMemberIgnore]
    public Vector2 LinearVelocity
    {
        get
        {
            if (!b2Body_IsValid(BodyId)) return default;

            var velocity = b2Body_GetLinearVelocity(BodyId);

            return new Vector2(velocity.X, velocity.Y);
        }
        set
        {
            if (b2Body_IsValid(BodyId))
                b2Body_SetLinearVelocity(BodyId, new B2Vec2(value.X, value.Y));
        }
    }

    /// <summary>
    /// The body's angular velocity in radians per second. Reads return zero and writes are ignored
    /// while <see cref="BodyId"/> is not a valid body.
    /// </summary>
    [DataMemberIgnore]
    public float AngularVelocity
    {
        get => b2Body_IsValid(BodyId) ? b2Body_GetAngularVelocity(BodyId) : 0f;
        set
        {
            if (b2Body_IsValid(BodyId))
                b2Body_SetAngularVelocity(BodyId, value);
        }
    }

    /// <summary>
    /// Applies a force to the body, waking it. Ignored while <see cref="BodyId"/> is not a valid body.
    /// </summary>
    /// <param name="force">The force in newtons.</param>
    /// <param name="point">World-space application point; the center of mass when null.</param>
    public void ApplyForce(Vector2 force, Vector2? point = null)
    {
        if (!b2Body_IsValid(BodyId)) return;

        var b2Force = new B2Vec2(force.X, force.Y);

        if (point.HasValue)
            b2Body_ApplyForce(BodyId, b2Force, new B2Vec2(point.Value.X, point.Value.Y), true);
        else
            b2Body_ApplyForceToCenter(BodyId, b2Force, true);
    }

    /// <summary>
    /// Applies a linear impulse to the body, waking it. Ignored while <see cref="BodyId"/> is not a valid body.
    /// </summary>
    /// <param name="impulse">The impulse in newton-seconds.</param>
    /// <param name="point">World-space application point; the center of mass when null.</param>
    public void ApplyImpulse(Vector2 impulse, Vector2? point = null)
    {
        if (!b2Body_IsValid(BodyId)) return;

        var b2Impulse = new B2Vec2(impulse.X, impulse.Y);

        if (point.HasValue)
            b2Body_ApplyLinearImpulse(BodyId, b2Impulse, new B2Vec2(point.Value.X, point.Value.Y), true);
        else
            b2Body_ApplyLinearImpulseToCenter(BodyId, b2Impulse, true);
    }

    /// <summary>
    /// Applies a torque to the body, waking it. Ignored while <see cref="BodyId"/> is not a valid body.
    /// </summary>
    /// <param name="torque">The torque in newton-meters.</param>
    public void ApplyTorque(float torque)
    {
        if (b2Body_IsValid(BodyId))
            b2Body_ApplyTorque(BodyId, torque, true);
    }
}