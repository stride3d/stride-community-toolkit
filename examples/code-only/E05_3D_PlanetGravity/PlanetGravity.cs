using Stride.BepuPhysics;
using Stride.BepuPhysics.Components;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace E05_3D_PlanetGravity;

/// <summary>
/// Pulls every body in <see cref="Bodies"/> towards <see cref="Centre"/> once per physics step, by the
/// acceleration of a point mass: <see cref="StandardGravityParameter"/> over the distance squared.
/// </summary>
/// <remarks>
/// Gravity in Bepu is a velocity change per step and nothing more, so a planet's gravity is the same
/// thing with a direction and a strength that depend on where the body is. The change is applied to the
/// body's linear velocity directly: an acceleration needs no mass, where an impulse would have to be
/// scaled by each body's mass to pull a heavy body as hard as a light one. The script implements
/// <see cref="ISimulationUpdate"/>, so the physics system calls it before every step of the simulation
/// its entity belongs to.
/// </remarks>
public class PlanetGravity : StartupScript, ISimulationUpdate
{
    /// <summary>Where the pull points.</summary>
    public Vector3 Centre { get; set; }

    /// <summary>The planet's mass times the gravitational constant: at distance r the acceleration is this over r squared.</summary>
    public float StandardGravityParameter { get; set; }

    /// <summary>Below this distance from the centre the pull stops growing, so a body at the centre is not flung off at infinite speed.</summary>
    public float MinimumDistance { get; set; } = 1f;

    /// <summary>The bodies the planet pulls on.</summary>
    public List<BodyComponent> Bodies { get; } = [];

    /// <summary>One step's pull on every body: a velocity change of acceleration times the step, towards the centre.</summary>
    public void SimulationUpdate(BepuSimulation simulation, float simTimeStep)
    {
        for (var i = 0; i < Bodies.Count; i++)
        {
            var body = Bodies[i];
            var offset = body.Position - Centre;
            var distance = MathF.Max(offset.Length(), MinimumDistance);
            var acceleration = StandardGravityParameter / (distance * distance);

            body.LinearVelocity -= offset / distance * (acceleration * simTimeStep);
        }
    }

    /// <summary>Nothing to read back after the step.</summary>
    public void AfterSimulationUpdate(BepuSimulation simulation, float simTimeStep)
    {
    }
}