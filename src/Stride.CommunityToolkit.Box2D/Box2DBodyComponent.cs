using Box2D.NET;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Worlds;

namespace Stride.CommunityToolkit.Box2D;

/// <summary>
/// Component that ties a Stride entity to a Box2D body, with live velocity, force and impulse
/// access once <see cref="BodyId"/> refers to a created body. The body itself is created from code
/// through <see cref="Box2DSimulation"/>; its type, density, friction and the rest are given there.
/// </summary>
// DataContract is what makes the component usable from Game Studio at all; the body itself is
// created by the simulation from code.
[DataContract("Box2DBodyComponent")]
[Display("Box2D Body")]
[ComponentCategory("Physics")]
public class Box2DBodyComponent : EntityComponent
{
    /// <summary>The Box2D body backing this component. Default (invalid) until the body is created.</summary>
    [DataMemberIgnore]
    public B2BodyId BodyId { get; set; }

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