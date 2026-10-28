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
        get => b2Body_IsValid(BodyId) ? b2Body_GetLinearVelocity(BodyId).ToVector2() : default;
        set
        {
            if (b2Body_IsValid(BodyId)) b2Body_SetLinearVelocity(BodyId, value.ToB2Vec2());
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
            if (b2Body_IsValid(BodyId)) b2Body_SetAngularVelocity(BodyId, value);
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

        if (point is { } at)
            b2Body_ApplyForce(BodyId, force.ToB2Vec2(), at.ToB2Vec2(), true);
        else
            b2Body_ApplyForceToCenter(BodyId, force.ToB2Vec2(), true);
    }

    /// <summary>
    /// Applies a linear impulse to the body, waking it. Ignored while <see cref="BodyId"/> is not a valid body.
    /// </summary>
    /// <param name="impulse">The impulse in newton-seconds.</param>
    /// <param name="point">World-space application point; the center of mass when null.</param>
    public void ApplyImpulse(Vector2 impulse, Vector2? point = null)
    {
        if (!b2Body_IsValid(BodyId)) return;

        if (point is { } at)
            b2Body_ApplyLinearImpulse(BodyId, impulse.ToB2Vec2(), at.ToB2Vec2(), true);
        else
            b2Body_ApplyLinearImpulseToCenter(BodyId, impulse.ToB2Vec2(), true);
    }

    /// <summary>
    /// Applies a torque to the body, waking it. Ignored while <see cref="BodyId"/> is not a valid body.
    /// </summary>
    /// <param name="torque">The torque in newton-meters.</param>
    public void ApplyTorque(float torque)
    {
        if (b2Body_IsValid(BodyId)) b2Body_ApplyTorque(BodyId, torque, true);
    }
}