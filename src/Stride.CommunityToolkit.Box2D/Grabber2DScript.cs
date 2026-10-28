using Box2D.NET;
using Stride.CommunityToolkit.Engine;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Worlds;

namespace Stride.CommunityToolkit.Box2D;

/// <summary>
/// Pick up any dynamic Box2D body with the mouse, drag it about, and let go - with whatever
/// velocity it had, so a flick throws it. Put it on the camera entity and give it the simulation.
/// </summary>
/// <remarks>
/// <para>
/// Box2D v3 has no mouse joint, so this uses the idiom of Box2D's own sample browser: a kinematic
/// anchor body is created at the grab point and a motor joint ties the picked body to it; every
/// frame the anchor is moved to the cursor with a target transform, so it carries a velocity the
/// joint feels; on release both are destroyed. The body is never moved directly, so it keeps
/// colliding and pushing while held.
/// </para>
/// <para>
/// Tuning follows the samples: a 7.5 Hz spring with critical damping, a force cap of
/// <see cref="ForceScale"/> times the body's weight so heavy and light bodies drag alike, and an
/// angular friction torque scaled by the body's lever arm so it does not spin freely on the end of
/// the cursor. The grab ends by itself if the body is destroyed under the cursor.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// game.AddGrabber2D(simulation);
/// </code>
/// </example>
[DataContract(nameof(Grabber2DScript))]
[Display("Grabber 2D (pick up Box2D bodies with the mouse)")]
[ComponentCategory("Physics")]
public class Grabber2DScript : SyncScript
{
    private CameraComponent? _camera;
    private B2BodyId _anchor;
    private B2JointId _joint;
    private bool _mouseGrab;

    /// <summary>The simulation whose bodies can be grabbed. Required.</summary>
    [DataMemberIgnore]
    public Box2DSimulation? Simulation { get; set; }

    /// <summary>The mouse button that grabs while held down. Left by default; the 2D camera controller pans with the right one.</summary>
    public MouseButton Button { get; set; } = MouseButton.Left;

    /// <summary>Half-size of the box around the cursor that a body must overlap to be picked, in metres.</summary>
    public float PickRadius { get; set; } = 0.05f;

    /// <summary>Spring stiffness of the hold, in hertz.</summary>
    public float SpringHertz { get; set; } = 7.5f;

    /// <summary>Spring damping ratio of the hold; 1 is critical.</summary>
    public float SpringDampingRatio { get; set; } = 1f;

    /// <summary>Force cap as a multiple of the held body's weight, so mass does not change the feel.</summary>
    public float ForceScale { get; set; } = 100f;

    /// <summary>The body being held, or <see langword="null"/>.</summary>
    [DataMemberIgnore]
    public B2BodyId? Held { get; private set; }

    /// <summary>Where the held body is being pulled to, in world space.</summary>
    [DataMemberIgnore]
    public Vector2 Target { get; private set; }

    /// <summary>Raised when a body is picked up.</summary>
    public event Action<B2BodyId>? Grabbed;

    /// <summary>Raised when the held body is let go, for any reason.</summary>
    public event Action<B2BodyId>? Released;

    /// <inheritdoc/>
    public override void Start()
    {
        _camera = Entity.Get<CameraComponent>()
            ?? throw new InvalidOperationException($"{nameof(Grabber2DScript)} needs a {nameof(CameraComponent)} on its entity: it picks through the camera. Add it to the camera entity.");
    }

    /// <inheritdoc/>
    public override void Update()
    {
        if (Simulation is null)
            return;

        // A grab started by the mouse ends with the button; one started through Grab() ends on Release().
        if (Held is { } held && ((_mouseGrab && !Input.IsMouseButtonDown(Button)) || !b2Joint_IsValid(_joint) || !b2Body_IsValid(held)))
        {
            Release();
            return;
        }

        if (Held is null)
        {
            if (Input.IsMouseButtonPressed(Button) && MouseWorldPoint() is { } point && Simulation.OverlapPoint(point, PickRadius) is { } body && Grab(body, point))
                _mouseGrab = true;

            return;
        }

        if (MouseWorldPoint() is { } target)
            Carry(target);
    }

    /// <inheritdoc/>
    public override void Cancel() => Release();

    /// <summary>
    /// Grabs <paramref name="body"/> at <paramref name="worldPoint"/>. The mouse path calls this
    /// from a pick; a game can call it to hand the player something.
    /// </summary>
    /// <returns><see langword="false"/> when the body cannot be held: not dynamic, or already holding one.</returns>
    public bool Grab(B2BodyId body, Vector2 worldPoint)
    {
        if (Simulation is null || Held is not null || !b2Body_IsValid(body) || b2Body_GetType(body) != B2BodyType.b2_dynamicBody)
            return false;

        var world = Simulation.WorldId;

        // The anchor: a kinematic body at the grab point that never sleeps, moved each frame.
        var anchorDef = b2DefaultBodyDef();
        anchorDef.type = B2BodyType.b2_kinematicBody;
        anchorDef.position = worldPoint.ToB2Vec2();
        anchorDef.enableSleep = false;
        _anchor = b2CreateBody(world, anchorDef);

        // The force cap is a multiple of the body's weight, as in the samples - with a floor on the
        // acceleration, so a zero-gravity world still gets a usable hold.
        var massData = b2Body_GetMassData(body);
        var gravity = MathF.Max(b2Length(b2World_GetGravity(world)), 9.81f);
        var weight = massData.mass * gravity;
        var lever = massData.mass > 0 ? MathF.Sqrt(massData.rotationalInertia / massData.mass) : 0;

        _joint = Joints2D.CreateMotor(world, _anchor, body, worldPoint, new MotorJointOptions
        {
            LinearHertz = SpringHertz,
            LinearDampingRatio = SpringDampingRatio,
            MaxSpringForce = ForceScale * weight,
            MaxVelocityTorque = 0.25f * lever * weight,      // angular friction: the body does not spin freely on the cursor
        });

        Held = body;
        Target = worldPoint;

        Grabbed?.Invoke(body);

        return true;
    }

    /// <summary>
    /// Moves the point the held body is pulled to. The mouse path calls this every frame.
    /// </summary>
    public void Carry(Vector2 worldPoint)
    {
        if (Simulation is null || Held is null)
            return;

        Target = worldPoint;

        // A target transform gives the anchor the velocity to get there in one physics step, so
        // the joint sees motion rather than a teleport.
        var transform = new B2Transform(worldPoint.ToB2Vec2(), b2Rot_identity);
        b2Body_SetTargetTransform(_anchor, transform, 1f / Simulation.TargetHz, true);
    }

    /// <summary>
    /// Lets go of the held body, if any. It keeps its current velocity.
    /// </summary>
    public void Release()
    {
        if (Held is not { } held)
            return;

        Joints2D.Destroy(_joint);

        if (b2Body_IsValid(_anchor))
            b2DestroyBody(_anchor);

        Held = null;
        _mouseGrab = false;

        Released?.Invoke(held);
    }

    private Vector2? MouseWorldPoint() => _camera?.ScreenToWorldPointOnXYPlane(Input.MousePosition);
}