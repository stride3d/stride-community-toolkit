using Stride.BepuPhysics;
using Stride.CommunityToolkit.Engine;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace E02_3D_GiveMeACube;

/// <summary>
/// Drives its entity around a horizontal circle about <see cref="Centre"/>.
/// </summary>
/// <remarks>
/// This is the point of the example: behaviour lives in a <see cref="SyncScript"/> attached to the
/// entity, instead of in the update callback passed to <c>game.Run(update: ...)</c>. Stride calls
/// <see cref="Start"/> once when the entity enters the scene and <see cref="Update"/> once per frame.
/// <para>
/// The entity is expected to carry a kinematic <see cref="BodyComponent"/>. The script moves that body
/// rather than the transform, which is what lets the orbiting cube shove the loose cubes out of its way.
/// </para>
/// <para>
/// It sets <see cref="BodyComponent.LinearVelocity"/> rather than calling <c>SetTargetPose</c>, whose
/// velocity, <c>(target - position) / FixedTimeStep</c>, assumes one physics tick per call. Below the
/// physics rate two ticks run on it, the body overshoots, and each correction overshoots further
/// until it diverges. A known velocity has no such feedback loop.
/// </para>
/// </remarks>
public class OrbitScript : SyncScript
{
    private BodyComponent? _body;
    private float _angle;

    /// <summary>
    /// Point the entity travels around. The entity should be spawned on the circle itself, at
    /// <see cref="Centre"/> plus <see cref="Radius"/> along +Z.
    /// </summary>
    public Vector3 Centre { get; set; }

    /// <summary>
    /// Distance from <see cref="Centre"/>, in world units.
    /// </summary>
    public float Radius { get; set; } = 3f;

    /// <summary>
    /// How fast the entity travels around the circle, in radians per second.
    /// </summary>
    public float AngularSpeed { get; set; } = 1f;

    /// <summary>
    /// How strongly the body is steered back onto the circle, per second. Higher values track the
    /// path more tightly; very high values reintroduce the overshoot this is meant to avoid.
    /// </summary>
    public float CorrectionStrength { get; set; } = 2f;

    /// <summary>
    /// Called once when the entity is added to the scene.
    /// </summary>
    public override void Start()
    {
        _body = Entity.Get<BodyComponent>();

        if (_body is null)
        {
            Log.Error($"{nameof(OrbitScript)} expects a kinematic {nameof(BodyComponent)} on the same entity.");
        }
    }

    /// <summary>
    /// Called once per frame.
    /// </summary>
    public override void Update()
    {
        // DeltaTime keeps the motion frame rate independent
        _angle += AngularSpeed * Game.DeltaTime();

        if (_body is null) return;

        // Move the body with a velocity, never Entity.Transform.Position: the sync runs physics to
        // transform only, so a transform write moves the mesh, leaves the collider and pushes nothing.

        // Velocity of a point travelling around a circle is the derivative of its position
        var tangent = new Vector3(MathF.Cos(_angle), 0, -MathF.Sin(_angle)) * (AngularSpeed * Radius);

        // Steer gently back onto the circle so small integration errors cannot accumulate
        var idealPosition = Centre + new Vector3(MathF.Sin(_angle), 0, MathF.Cos(_angle)) * Radius;
        var correction = (idealPosition - _body.Position) * CorrectionStrength;

        _body.LinearVelocity = tangent + correction;

        // Setting a velocity does not wake a sleeping body, so do it explicitly
        _body.Awake = true;
    }
}