using Stride.BepuPhysics;
using Stride.CommunityToolkit.Games;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace Example02_GiveMeACube;

/// <summary>
/// Drives its entity around a horizontal circle centred on wherever the entity started.
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
/// It sets <see cref="BodyComponent.LinearVelocity"/> directly rather than calling
/// <c>SetTargetPose</c>. SetTargetPose derives a velocity from <c>(target - position) / FixedTimeStep</c>,
/// which assumes exactly one physics tick per call; when the frame rate drops below the physics rate,
/// two ticks run on that velocity, overshoot past the target, and the correction overshoots further
/// until the body diverges. Integrating a known velocity has no such feedback loop.
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

        // Move the body by giving it a velocity, never by assigning Entity.Transform.Position. The
        // transform sync runs physics to transform and never the other way round, so writing the
        // transform would move the mesh while leaving the collider behind, and nothing would be pushed.

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
