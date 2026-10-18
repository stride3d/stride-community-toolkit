using BepuPhysics;
using Stride.BepuPhysics;
using Stride.BepuPhysics.Constraints;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Mathematics;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Processors;
using Stride.Input;

namespace Stride.CommunityToolkit.Bepu;

/// <summary>
/// Pick up any dynamic body with the mouse, carry it on the end of the camera ray, and let go -
/// with whatever velocity it had, so a flick throws it. Put it on the camera entity.
/// </summary>
/// <remarks>
/// <para>
/// The body is never moved directly. A <see cref="OneBodyLinearServoConstraintComponent"/> pulls
/// the grabbed point toward the cursor and a <see cref="OneBodyAngularServoConstraintComponent"/>
/// holds the orientation, so the solver stays in charge: the held body still collides, still
/// pushes other bodies, and cannot be forced through a wall. Teleporting a kinematic body each
/// frame, the other common way to drag, does none of that: the body stops colliding while held
/// and has no velocity when let go.
/// </para>
/// <para>
/// Tuning follows bepuphysics2's demo grabber: a soft spring (5 Hz, damping ratio 2), a force cap
/// scaled by the body's mass so heavy and light bodies drag alike, no angular servo on a body whose
/// rotation is locked, and the held body kept awake. The grab ends by itself if the body turns
/// kinematic or leaves the scene.
/// </para>
/// <para>
/// Keys: <see cref="Button"/> to grab and hold; the mouse wheel moves the carry point along the
/// ray; <see cref="RotateKey"/> plus mouse movement turns the held body. All configurable, and none
/// of them clash with the toolkit's camera controllers, which look with the right button.
/// </para>
/// <para>
/// A 2D scene needs nothing more: under an orthographic camera the pick ray runs along the view, so
/// a <see cref="Body2DComponent"/> is carried across its plane. The wheel does nothing for a 2D body,
/// or under an orthographic camera, since depth there is the plane itself.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// game.GetCameraEntity().Add(new GrabberScript());
/// </code>
/// </example>
[DataContract(nameof(GrabberScript))]
[Display("Grabber (pick up bodies with the mouse)")]
[ComponentCategory("Physics")]
public class GrabberScript : SyncScript
{
    private const float MinimumHoldDistance = 0.5f;

    private CameraComponent? _camera;
    private OneBodyLinearServoConstraintComponent? _linear;
    private OneBodyAngularServoConstraintComponent? _angular;
    private Vector3 _localGrabPoint;
    private Quaternion _targetOrientation;
    private bool _mouseGrab;

    /// <summary>The mouse button that grabs while held down. Left by default; the camera controllers look with the right one.</summary>
    public MouseButton Button { get; set; } = MouseButton.Left;

    /// <summary>Hold this key while grabbing to turn the held body with the mouse instead of moving it.</summary>
    public Keys RotateKey { get; set; } = Keys.T;

    /// <summary>How far from the camera a body can be picked up. Defaults to 100, which reaches a 2D scene's bodies from its camera at 50 and a pile viewed from 80.</summary>
    public float MaxDistance { get; set; } = 100f;

    /// <summary>Spring stiffness of the hold, in hertz. Higher snaps harder and starts to fight the solver.</summary>
    public float SpringFrequency { get; set; } = 5f;

    /// <summary>Spring damping ratio of the hold. Above 1 there is no overshoot.</summary>
    public float SpringDampingRatio { get; set; } = 2f;

    /// <summary>Force cap per kilogram of held body, so mass does not change how the drag feels.</summary>
    public float ForcePerKilogram { get; set; } = 360f;

    /// <summary>How far one mouse-wheel notch moves the carry point along the ray.</summary>
    public float WheelDistanceStep { get; set; } = 0.5f;

    /// <summary>Radians of body rotation per screen width of mouse travel while <see cref="RotateKey"/> is held.</summary>
    public float RotationSensitivity { get; set; } = 4f;

    /// <summary>The body being held, or <see langword="null"/>.</summary>
    [DataMemberIgnore]
    public BodyComponent? Held { get; private set; }

    /// <summary>Distance from the camera to the carry point while holding.</summary>
    [DataMemberIgnore]
    public float HoldDistance { get; private set; }

    /// <summary>Raised when a body is picked up.</summary>
    public event Action<BodyComponent>? Grabbed;

    /// <summary>Raised when the held body is let go, for any reason.</summary>
    public event Action<BodyComponent>? Released;

    /// <inheritdoc/>
    public override void Start()
    {
        _camera = Entity.Get<CameraComponent>()
            ?? throw new InvalidOperationException($"{nameof(GrabberScript)} needs a {nameof(CameraComponent)} on its entity: it picks along the camera's ray. Add it to the camera entity.");
    }

    /// <inheritdoc/>
    public override void Update()
    {
        // A grab started by the mouse ends with the button; one started through Grab() ends on Release().
        if (Held is { } held && ((_mouseGrab && !Input.IsMouseButtonDown(Button)) || !StillHoldable(held)))
        {
            Release();
            return;
        }

        if (Held is null)
        {
            if (Input.IsMouseButtonPressed(Button))
                GrabUnderMouse();

            return;
        }

        Carry();
    }

    /// <inheritdoc/>
    public override void Cancel() => Release();

    /// <summary>
    /// Lets go of the held body, if any. It keeps its current velocity.
    /// </summary>
    public void Release()
    {
        if (Held is not { } held)
            return;

        if (_linear is not null)
            Entity.Remove(_linear);

        if (_angular is not null)
            Entity.Remove(_angular);

        _linear = null;
        _angular = null;
        Held = null;
        _mouseGrab = false;

        Released?.Invoke(held);
    }

    /// <summary>
    /// Grabs <paramref name="body"/> at <paramref name="hitPoint"/>, carried <paramref name="distance"/>
    /// in front of the camera. The mouse path calls this from a raycast; a game can call it to hand
    /// the player something.
    /// </summary>
    /// <returns><see langword="false"/> when the body cannot be held: kinematic, infinite mass, or already holding one.</returns>
    public bool Grab(BodyComponent body, Vector3 hitPoint, float distance)
    {
        ArgumentNullException.ThrowIfNull(body);

        if (Held is not null || !StillHoldable(body))
            return false;

        var inertia = body.BodyInertia;

        if (inertia.InverseMass <= 0)
            return false;

        _localGrabPoint = GrabberMath.LocalGrabPoint(hitPoint, body.Position, body.Orientation);
        _targetOrientation = body.Orientation;
        HoldDistance = MathF.Max(MinimumHoldDistance, distance);

        // The constraints live on this entity, not the body's: the held entity is left as it was.
        _linear = new OneBodyLinearServoConstraintComponent
        {
            A = body,
            LocalOffset = _localGrabPoint,
            Target = hitPoint,
            SpringFrequency = SpringFrequency,
            SpringDampingRatio = SpringDampingRatio,
            ServoMaximumSpeed = float.MaxValue,
            ServoBaseSpeed = 0,
            ServoMaximumForce = GrabberMath.MaximumForce(ForcePerKilogram, inertia.InverseMass),
        };
        Entity.Add(_linear);

        // A body with locked rotation has nothing for the angular servo to do, and the solver
        // would reject the constraint.
        if (!Bodies.HasLockedInertia(inertia.InverseInertiaTensor))
        {
            _angular = new OneBodyAngularServoConstraintComponent
            {
                A = body,
                TargetOrientation = _targetOrientation,
                SpringFrequency = SpringFrequency,
                SpringDampingRatio = SpringDampingRatio,
                ServoMaximumSpeed = float.MaxValue,
                ServoBaseSpeed = 0,
                ServoMaximumForce = GrabberMath.MaximumTorque(ForcePerKilogram, _localGrabPoint.Length(), inertia.InverseMass),
            };
            Entity.Add(_angular);
        }

        Held = body;
        body.Awake = true;

        Grabbed?.Invoke(body);

        return true;
    }

    /// <summary>
    /// Finds what the pick ray meets first, within <see cref="MaxDistance"/>, looking through trigger
    /// volumes: a sensor is not a thing to hold, and it must not hide what is inside it.
    /// </summary>
    /// <param name="segment">The ray, from the camera outwards.</param>
    /// <param name="hit">The first hit on something that is not a trigger; undefined when there is none.</param>
    /// <returns><see langword="true"/> when something other than a trigger lies on the ray within reach.</returns>
    /// <remarks>
    /// A trigger is a collidable whose <c>ContactEventHandler</c> says <c>NoContactResponse</c>. Everything
    /// else counts, statics included: a wall in front of a body hides it, as it should.
    /// </remarks>
    public bool TryPick(in RaySegment segment, out HitInfo hit)
    {
        hit = default;

        if (Entity?.GetSimulation() is not { } simulation)
            return false;

        var hits = simulation.RayCastPenetrating(segment);

        hits.Sort();

        foreach (var candidate in hits)
        {
            if (candidate.Distance > MaxDistance)
                break;

            if (candidate.Collidable.ContactEventHandler is { NoContactResponse: true })
                continue;

            hit = candidate;

            return true;
        }

        return false;
    }

    private void GrabUnderMouse()
    {
        if (_camera is null)
            return;

        if (!TryPick(_camera.ScreenToWorldRaySegment(Input.MousePosition), out var hit))
            return;

        if (hit.Collidable is BodyComponent body && Grab(body, hit.Point, hit.Distance))
            _mouseGrab = true;
    }

    private void Carry()
    {
        if (_camera is null || Held is not { } held || _linear is null)
            return;

        // A 2D body pulled along the ray would leave its plane and fight its own plane lock, and under
        // an orthographic camera depth is the plane itself, so the wheel changes nothing in either case
        if (held is not Body2DComponent && _camera.Projection != CameraProjectionMode.Orthographic)
            HoldDistance = MathF.Max(MinimumHoldDistance, HoldDistance + Input.MouseWheelDelta * WheelDistanceStep);

        var ray = _camera.GetPickRay(Input.MousePosition);

        _linear.Target = GrabberMath.TargetPoint(ray.Position, ray.Direction, HoldDistance);

        if (_angular is not null && Input.IsKeyDown(RotateKey))
        {
            // Mouse delta is a fraction of the screen; yaw about the camera's up, pitch about its right.
            var delta = Input.MouseDelta * RotationSensitivity;
            var world = Entity.Transform.WorldMatrix;
            var yaw = Quaternion.RotationAxis(Vector3.Normalize((Vector3)world.Row2), -delta.X);
            var pitch = Quaternion.RotationAxis(Vector3.Normalize((Vector3)world.Row1), -delta.Y);

            _targetOrientation = Quaternion.Normalize(_targetOrientation * pitch * yaw);
            _angular.TargetOrientation = _targetOrientation;
        }

        // The demo resets the sleep counter every frame; without this a body held still nods off
        // and stops answering the servo.
        held.Awake = true;
    }

    private static bool StillHoldable(BodyComponent body)
        => !body.Kinematic && body.Entity?.Scene is not null && body.Simulation is not null;
}