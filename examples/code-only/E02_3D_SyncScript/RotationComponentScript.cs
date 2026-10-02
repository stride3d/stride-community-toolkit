using Stride.BepuPhysics;
using Stride.CommunityToolkit.Engine;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace E02_3D_SyncScript;

/// <summary>
/// Drives its entity around a horizontal circle, once per frame, from a <see cref="SyncScript"/>.
/// </summary>
/// <remarks>
/// <para>
/// The entity carries a physics body, so the simulation owns its transform: a position written to
/// <c>Entity.Transform</c> moves the mesh and leaves the collider behind. The body is made kinematic
/// and given a velocity instead, which is what lets it push dynamic bodies out of its way.
/// </para>
/// <para>
/// A velocity, not <c>SetTargetPose</c>. SetTargetPose derives a velocity from
/// <c>(target - position) / FixedTimeStep</c>, which assumes exactly one physics step will consume
/// it; called every frame, that assumption breaks whenever the frame rate differs from the physics
/// rate, and the body overshoots further each frame. It is the right call from
/// <c>ISimulationUpdate</c>, which runs once per physics step: see E02_3D_GiveMeACube_SimulationUpdate.
/// </para>
/// </remarks>
public class RotationComponentScript : SyncScript
{
    private const float AngularSpeed = 2f;
    private const float Radius = 3f;

    // How strongly the body is steered back onto the circle, per second
    private const float CorrectionStrength = 2f;

    private BodyComponent? _body;
    private Vector3 _centre;
    private float _angle;

    public override void Start()
    {
        _body = Entity.Get<BodyComponent>();

        if (_body is null)
        {
            Log.Error($"{nameof(RotationComponentScript)} expects a {nameof(BodyComponent)} on the same entity.");

            return;
        }

        // Kinematic: moved by this script, immovable to everything else
        _body.Kinematic = true;

        // The entity starts on the circle, at angle zero, so the centre is one radius behind it
        _centre = Entity.Transform.Position - new Vector3(0f, 0f, Radius);
    }

    public override void Update()
    {
        if (_body is null) return;

        // The frame's elapsed time keeps the speed the same at any frame rate
        _angle += AngularSpeed * Game.DeltaTime();

        // The velocity of a point going round a circle, plus a gentle pull back onto the circle so
        // small integration errors cannot add up over time
        var tangent = new Vector3(MathF.Cos(_angle), 0f, -MathF.Sin(_angle)) * (AngularSpeed * Radius);
        var ideal = _centre + new Vector3(MathF.Sin(_angle), 0f, MathF.Cos(_angle)) * Radius;

        _body.LinearVelocity = tangent + (ideal - _body.Position) * CorrectionStrength;

        // Not needed while the cube never stops: a body only sleeps once it has been still for a
        // while. It is the habit worth keeping, because a body that has slept ignores a new velocity
        // until something wakes it, and nothing here would
        _body.Awake = true;
    }
}