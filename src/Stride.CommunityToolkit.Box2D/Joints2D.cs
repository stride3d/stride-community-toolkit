using Box2D.NET;
using Stride.Core.Mathematics;
using static Box2D.NET.B2Joints;
using static Box2D.NET.B2Worlds;

namespace Stride.CommunityToolkit.Box2D;

/// <summary>
/// Box2D's seven joint types with world-space anchors: say where the pivot is and which way the
/// axis points, and the local frames each body needs are worked out here.
/// </summary>
/// <remarks>
/// <para>
/// A raw joint definition hides its bodies inside a <c>base</c> field and expresses each anchor
/// as a <em>local frame</em>: a point and a rotation in that body's own space, which have to be
/// derived from the world pivot with the body's current transform. Every Box2D sample repeats that
/// derivation; these factories do it once. The pose at creation is the joint's zero: a revolute
/// joint's angle, a prismatic joint's translation and a weld's rest pose are all measured from
/// where the bodies were when the joint was made.
/// </para>
/// <para>
/// Options records carry the per-type knobs by the names Box2D uses. A property left
/// <see langword="null"/> keeps Box2D's default, so <c>new RevoluteJointOptions { EnableMotor =
/// true, MotorSpeed = 2 }</c> changes exactly two things. Angles are in radians, as in Box2D.
/// </para>
/// <para>
/// The static methods take a <see cref="B2WorldId"/> and body ids, like <see cref="PhysicsQueries2D"/>;
/// <see cref="Box2DSimulation.Joints"/> offers the same with entities.
/// </para>
/// </remarks>
public static class Joints2D
{
    /// <summary>
    /// A hinge at <paramref name="worldPivot"/>: the bodies turn about it and nothing else.
    /// </summary>
    public static B2JointId CreateRevolute(B2WorldId world, B2BodyId a, B2BodyId b, Vector2 worldPivot, RevoluteJointOptions? options = null)
    {
        var def = b2DefaultRevoluteJointDef();

        Anchor(ref def.@base, a, b, worldPivot, worldPivot);
        Apply(ref def.@base, options);

        if (options is not null)
        {
            Set(ref def.targetAngle, options.TargetAngle);
            Set(ref def.enableSpring, options.EnableSpring);
            Set(ref def.hertz, options.Hertz);
            Set(ref def.dampingRatio, options.DampingRatio);
            Set(ref def.enableLimit, options.EnableLimit);
            Set(ref def.lowerAngle, options.LowerAngle);
            Set(ref def.upperAngle, options.UpperAngle);
            Set(ref def.enableMotor, options.EnableMotor);
            Set(ref def.maxMotorTorque, options.MaxMotorTorque);
            Set(ref def.motorSpeed, options.MotorSpeed);
        }

        return b2CreateRevoluteJoint(world, def);
    }

    /// <summary>
    /// A slider: <paramref name="b"/> moves along <paramref name="worldAxis"/> through
    /// <paramref name="worldPivot"/>, fixed in <paramref name="a"/>'s frame, without turning.
    /// </summary>
    public static B2JointId CreatePrismatic(B2WorldId world, B2BodyId a, B2BodyId b, Vector2 worldPivot, Vector2 worldAxis, PrismaticJointOptions? options = null)
    {
        var def = b2DefaultPrismaticJointDef();

        Anchor(ref def.@base, a, b, worldPivot, worldPivot, JointFrames2D.AxisAngle(worldAxis));
        Apply(ref def.@base, options);

        if (options is not null)
        {
            Set(ref def.enableSpring, options.EnableSpring);
            Set(ref def.hertz, options.Hertz);
            Set(ref def.dampingRatio, options.DampingRatio);
            Set(ref def.targetTranslation, options.TargetTranslation);
            Set(ref def.enableLimit, options.EnableLimit);
            Set(ref def.lowerTranslation, options.LowerTranslation);
            Set(ref def.upperTranslation, options.UpperTranslation);
            Set(ref def.enableMotor, options.EnableMotor);
            Set(ref def.maxMotorForce, options.MaxMotorForce);
            Set(ref def.motorSpeed, options.MotorSpeed);
        }

        return b2CreatePrismaticJoint(world, def);
    }

    /// <summary>
    /// A wheel on a suspension: <paramref name="wheel"/> turns about <paramref name="worldPivot"/>
    /// and slides along <paramref name="worldAxis"/> - the suspension direction, usually straight
    /// up - relative to <paramref name="chassis"/>.
    /// </summary>
    public static B2JointId CreateWheel(B2WorldId world, B2BodyId chassis, B2BodyId wheel, Vector2 worldPivot, Vector2 worldAxis, WheelJointOptions? options = null)
    {
        var def = b2DefaultWheelJointDef();

        Anchor(ref def.@base, chassis, wheel, worldPivot, worldPivot, JointFrames2D.AxisAngle(worldAxis));
        Apply(ref def.@base, options);

        if (options is not null)
        {
            Set(ref def.enableSpring, options.EnableSpring);
            Set(ref def.hertz, options.Hertz);
            Set(ref def.dampingRatio, options.DampingRatio);
            Set(ref def.enableLimit, options.EnableLimit);
            Set(ref def.lowerTranslation, options.LowerTranslation);
            Set(ref def.upperTranslation, options.UpperTranslation);
            Set(ref def.enableMotor, options.EnableMotor);
            Set(ref def.maxMotorTorque, options.MaxMotorTorque);
            Set(ref def.motorSpeed, options.MotorSpeed);
        }

        return b2CreateWheelJoint(world, def);
    }

    /// <summary>
    /// A rod between two anchors, one on each body. The rest length defaults to their distance at
    /// creation; a spring makes it soft, a limit with a spring makes it a rope.
    /// </summary>
    public static B2JointId CreateDistance(B2WorldId world, B2BodyId a, B2BodyId b, Vector2 worldAnchorA, Vector2 worldAnchorB, DistanceJointOptions? options = null)
    {
        var def = b2DefaultDistanceJointDef();

        Anchor(ref def.@base, a, b, worldAnchorA, worldAnchorB);
        Apply(ref def.@base, options);

        def.length = options?.Length ?? Vector2.Distance(worldAnchorA, worldAnchorB);

        if (options is not null)
        {
            Set(ref def.enableSpring, options.EnableSpring);
            Set(ref def.hertz, options.Hertz);
            Set(ref def.dampingRatio, options.DampingRatio);
            Set(ref def.lowerSpringForce, options.LowerSpringForce);
            Set(ref def.upperSpringForce, options.UpperSpringForce);
            Set(ref def.enableLimit, options.EnableLimit);
            Set(ref def.minLength, options.MinLength);
            Set(ref def.maxLength, options.MaxLength);
            Set(ref def.enableMotor, options.EnableMotor);
            Set(ref def.maxMotorForce, options.MaxMotorForce);
            Set(ref def.motorSpeed, options.MotorSpeed);
        }

        return b2CreateDistanceJoint(world, def);
    }

    /// <summary>
    /// Glues the bodies together at <paramref name="worldPivot"/> in their current relative pose.
    /// </summary>
    public static B2JointId CreateWeld(B2WorldId world, B2BodyId a, B2BodyId b, Vector2 worldPivot, WeldJointOptions? options = null)
    {
        var def = b2DefaultWeldJointDef();

        Anchor(ref def.@base, a, b, worldPivot, worldPivot);
        Apply(ref def.@base, options);

        if (options is not null)
        {
            Set(ref def.linearHertz, options.LinearHertz);
            Set(ref def.angularHertz, options.AngularHertz);
            Set(ref def.linearDampingRatio, options.LinearDampingRatio);
            Set(ref def.angularDampingRatio, options.AngularDampingRatio);
        }

        return b2CreateWeldJoint(world, def);
    }

    /// <summary>
    /// Drives <paramref name="b"/> relative to <paramref name="a"/>. With a pivot, both frames sit
    /// there and the spring pulls that point on B toward that point on A - the mouse-drag shape;
    /// without one, the frames are the body origins.
    /// </summary>
    public static B2JointId CreateMotor(B2WorldId world, B2BodyId a, B2BodyId b, Vector2? worldPivot = null, MotorJointOptions? options = null)
    {
        var def = b2DefaultMotorJointDef();

        if (worldPivot is { } pivot)
            Anchor(ref def.@base, a, b, pivot, pivot);
        else
        {
            def.@base.bodyIdA = a;
            def.@base.bodyIdB = b;
        }

        Apply(ref def.@base, options);

        if (options is not null)
        {
            if (options.LinearVelocity is { } velocity)
                def.linearVelocity = new B2Vec2(velocity.X, velocity.Y);

            Set(ref def.maxVelocityForce, options.MaxVelocityForce);
            Set(ref def.angularVelocity, options.AngularVelocity);
            Set(ref def.maxVelocityTorque, options.MaxVelocityTorque);
            Set(ref def.linearHertz, options.LinearHertz);
            Set(ref def.linearDampingRatio, options.LinearDampingRatio);
            Set(ref def.maxSpringForce, options.MaxSpringForce);
            Set(ref def.angularHertz, options.AngularHertz);
            Set(ref def.angularDampingRatio, options.AngularDampingRatio);
            Set(ref def.maxSpringTorque, options.MaxSpringTorque);
        }

        return b2CreateMotorJoint(world, def);
    }

    /// <summary>
    /// No constraint at all: the two bodies simply never collide with each other. For the links of
    /// a chain or the parts of a ragdoll.
    /// </summary>
    public static B2JointId CreateFilter(B2WorldId world, B2BodyId a, B2BodyId b)
    {
        var def = b2DefaultFilterJointDef();

        def.@base.bodyIdA = a;
        def.@base.bodyIdB = b;

        return b2CreateFilterJoint(world, def);
    }

    /// <summary>Removes a joint. Safe on a joint that is already gone.</summary>
    /// <param name="joint">The joint.</param>
    /// <param name="wakeBodies">Wake the bodies it connected, so they react to being freed.</param>
    public static void Destroy(B2JointId joint, bool wakeBodies = true)
    {
        if (b2Joint_IsValid(joint))
            b2DestroyJoint(joint, wakeBodies);
    }

    /// <summary>Whether the joint still exists - it goes when either body is destroyed.</summary>
    public static bool IsValid(B2JointId joint) => b2Joint_IsValid(joint);

    /// <summary>
    /// The joint's two anchors in world space right now, for drawing it. They coincide for a
    /// hinge or a weld and sit apart for a rod.
    /// </summary>
    public static (Vector2 A, Vector2 B) GetAnchors(B2JointId joint)
    {
        var a = JointFrames2D.WorldPoint(B2Bodies.b2Body_GetTransform(b2Joint_GetBodyA(joint)), b2Joint_GetLocalFrameA(joint));
        var b = JointFrames2D.WorldPoint(B2Bodies.b2Body_GetTransform(b2Joint_GetBodyB(joint)), b2Joint_GetLocalFrameB(joint));

        return (a, b);
    }

    private static void Anchor(ref B2JointDef def, B2BodyId a, B2BodyId b, Vector2 worldAnchorA, Vector2 worldAnchorB, float worldAngle = 0f)
    {
        def.bodyIdA = a;
        def.bodyIdB = b;
        def.localFrameA = JointFrames2D.LocalFrame(a, worldAnchorA, worldAngle);
        def.localFrameB = JointFrames2D.LocalFrame(b, worldAnchorB, worldAngle);
    }

    private static void Apply(ref B2JointDef def, JointOptionsBase? options)
    {
        if (options is null)
            return;

        Set(ref def.collideConnected, options.CollideConnected);
        Set(ref def.forceThreshold, options.ForceThreshold);
        Set(ref def.torqueThreshold, options.TorqueThreshold);
    }

    private static void Set<T>(ref T field, T? value) where T : struct
    {
        if (value is { } set)
            field = set;
    }
}