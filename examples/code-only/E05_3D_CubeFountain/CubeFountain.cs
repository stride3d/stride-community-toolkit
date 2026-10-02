using Stride.BepuPhysics;
using Stride.BepuPhysics.Components;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace E05_3D_CubeFountain;

/// <summary>
/// Launches bodies at a steady rate, counted on the physics clock and not on the render loop.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="StartupScript"/> with no per-frame <c>Update</c>: Bepu calls
/// <see cref="SimulationUpdate"/> once per fixed physics step, with a time step that is always the
/// same. Counting bodies in those steps ties the fountain to the simulation. At any frame rate the
/// same number of bodies has left the nozzle after ten simulated seconds, and when the simulation is
/// slowed or paused the fountain slows or stops with it, with no code here to make it so.
/// </para>
/// <para>
/// A spawner in a per-frame <c>Update</c> counts wall-clock time instead. It keeps producing bodies
/// while the simulation is paused, all at the same spot, and they burst apart when it resumes.
/// </para>
/// </remarks>
public class CubeFountain : StartupScript, ISimulationUpdate
{
    private readonly List<(BodyComponent Body, int Kind)> _bodies = [];
    private readonly Random _random = new(1);

    // Bodies owed but not launched yet. A rate of 20 a second at 60 steps a second is a third of a
    // body per step: the fraction is carried over, so no step rounds it away
    private float _owed;

    // The next body to take back once every body is in use
    private int _oldest;

    /// <summary>Makes one body of a kind at a position and puts it in the scene.</summary>
    public required Func<int, Vector3, Entity> Create { get; init; }

    /// <summary>Takes a body out of the scene for good: called when its place goes to a body of another kind.</summary>
    public required Action<Entity> Remove { get; init; }

    /// <summary>The kind of body launched from now on. What a kind is - a shape, a colour - is the caller's business.</summary>
    public int Kind { get; set; }

    /// <summary>How many bodies the fountain owns. Past that, the oldest is launched again.</summary>
    public int Capacity { get; init; } = 500;

    /// <summary>Bodies per second of simulated time.</summary>
    public float Rate { get; set; } = 20f;

    /// <summary>The velocity a body leaves the nozzle with.</summary>
    public Vector3 Velocity { get; set; } = new(0f, 11f, 0f);

    /// <summary>How far a body's velocity may differ from <see cref="Velocity"/>, per axis.</summary>
    public Vector3 Spread { get; set; } = new(1.6f, 1f, 1.6f);

    /// <summary>How many bodies exist.</summary>
    public int Count => _bodies.Count;

    /// <summary>How many times a body's place was taken for a new launch.</summary>
    public int Recycled { get; private set; }

    /// <summary>Every body the fountain owns.</summary>
    public IEnumerable<BodyComponent> Bodies => _bodies.Select(owned => owned.Body);

    /// <summary>The simulation the fountain runs in, known from the first physics step on.</summary>
    public BepuSimulation? Simulation { get; private set; }

    /// <summary>Called once per fixed physics step, before the simulation is stepped.</summary>
    /// <param name="simulation">The simulation this component belongs to.</param>
    /// <param name="simTimeStep">Length of the step about to run, in seconds. Constant, unlike a frame's delta time.</param>
    public void SimulationUpdate(BepuSimulation simulation, float simTimeStep)
    {
        Simulation = simulation;

        _owed += MathF.Max(Rate, 0f) * simTimeStep;

        while (_owed >= 1f)
        {
            _owed -= 1f;

            // A millimetre of scatter at the nozzle: two bodies launched in one step must not start
            // in exactly the same place
            var from = Entity.Transform.Position + new Vector3(Scatter(0.001f), Scatter(0.001f), Scatter(0.001f));
            var velocity = Velocity + new Vector3(Scatter(Spread.X), Scatter(Spread.Y), Scatter(Spread.Z));

            Launch(from, velocity);
        }
    }

    /// <summary>Called once per fixed physics step, after the simulation has been stepped. Nothing to do here.</summary>
    public void AfterSimulationUpdate(BepuSimulation simulation, float simTimeStep)
    {
    }

    /// <summary>
    /// Sends a body of the current <see cref="Kind"/> from a position at a velocity: a new one while
    /// there is room, otherwise in the place of the oldest.
    /// </summary>
    public void Launch(Vector3 from, Vector3 velocity)
    {
        BodyComponent body;

        if (_bodies.Count < Capacity)
        {
            body = Create(Kind, from).Get<BodyComponent>();

            _bodies.Add((body, Kind));
        }
        else
        {
            var oldest = _bodies[_oldest];

            if (oldest.Kind == Kind)
            {
                // The same kind is reused as it is. Teleport moves the body and nothing else: it
                // keeps the velocity it had and stays asleep if it was. Both are set below
                body = oldest.Body;
                body.Teleport(from, Quaternion.Identity);
            }
            else
            {
                // Another kind is another collider and another model: the old body leaves and a
                // new one takes its place in the store
                Remove(oldest.Body.Entity);

                body = Create(Kind, from).Get<BodyComponent>();
                _bodies[_oldest] = (body, Kind);
            }

            _oldest = (_oldest + 1) % _bodies.Count;

            Recycled++;
        }

        // A body at rest in the pile is asleep, and a sleeping body ignores a new velocity
        body.Awake = true;
        body.LinearVelocity = velocity;
        body.AngularVelocity = new Vector3(Scatter(3f), Scatter(3f), Scatter(3f));
    }

    /// <summary>A random value from minus the range to plus the range.</summary>
    private float Scatter(float range) => (_random.NextSingle() * 2f - 1f) * range;
}