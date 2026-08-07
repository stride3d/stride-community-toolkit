using Stride.BepuPhysics;
using Stride.BepuPhysics.Components;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace Example02_GiveMeACube_SimulationUpdate;

/// <summary>
/// Drives its entity around a horizontal circle, stepping in time with the physics simulation rather
/// than with the render loop.
/// </summary>
/// <remarks>
/// This is a <see cref="StartupScript"/>, so it has no per-frame <c>Update</c> at all. All the work
/// happens in <see cref="SimulationUpdate"/>, which Bepu calls once per fixed physics step, just before
/// stepping the simulation. Implementing <see cref="ISimulationUpdate"/> is enough to be called: Stride
/// discovers the component and registers it with the simulation automatically.
/// <para>
/// Running on the physics clock is what makes <see cref="BodyComponent.SetTargetPose(Vector3)"/> safe
/// here. SetTargetPose derives a velocity from <c>(target - position) / FixedTimeStep</c>, which assumes
/// exactly one physics step will consume it. Called from a per-frame <c>Update</c> that assumption breaks
/// whenever the frame rate differs from the physics rate: if two steps run on one velocity the body
/// overshoots as far past the target as it was short, and the error grows every frame. Called from here
/// there is exactly one step per call, so the body lands on the target every time.
/// </para>
/// </remarks>
public class OrbitSimulationScript : StartupScript, ISimulationUpdate
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
    /// Called once when the entity is added to the scene.
    /// </summary>
    public override void Start()
    {
        if (Entity.Get<BodyComponent>() is null)
        {
            Log.Error($"{nameof(OrbitSimulationScript)} expects a kinematic {nameof(BodyComponent)} on the same entity.");
        }
    }

    /// <summary>
    /// Called once per fixed physics step, before the simulation is stepped.
    /// </summary>
    /// <param name="simulation">The simulation this component belongs to.</param>
    /// <param name="simTimeStep">Length of the step about to run, in seconds. Constant, unlike a frame's delta time.</param>
    public void SimulationUpdate(BepuSimulation simulation, float simTimeStep)
    {
        // Resolve the body here rather than in Start. This method can run first: the component is
        // registered with the simulation the moment it enters the scene, while Start waits its turn
        // in the script system.
        _body ??= Entity.Get<BodyComponent>();

        if (_body is null) return;

        // Advancing by the physics step rather than by frame delta time keeps the orbit at the same
        // real-world speed no matter how fast or slow the game is rendering
        _angle += AngularSpeed * simTimeStep;

        var target = Centre + new Vector3(MathF.Sin(_angle), 0, MathF.Cos(_angle)) * Radius;

        // Exactly one physics step will consume the velocity this sets, so the body lands on the target
        _body.SetTargetPose(target);
    }

    /// <summary>
    /// Called once per fixed physics step, after the simulation has been stepped. Useful for reading
    /// results such as contacts or resting velocities; nothing is needed here.
    /// </summary>
    /// <param name="simulation">The simulation this component belongs to.</param>
    /// <param name="simTimeStep">Length of the step that just ran, in seconds.</param>
    public void AfterSimulationUpdate(BepuSimulation simulation, float simTimeStep)
    {
    }
}