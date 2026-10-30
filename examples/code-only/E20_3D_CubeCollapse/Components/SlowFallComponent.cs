using Stride.BepuPhysics;
using Stride.BepuPhysics.Components;
using Stride.Engine;

namespace CubeCollapse.Components;

/// <summary>
/// A rigid body that falls under a fraction of gravity, for objects that should drift down rather
/// than drop - the game-over letters, which nobody can read at full falling speed.
/// </summary>
/// <remarks>
/// <para>
/// This exists because Bepu offers no per-body gravity scale: <see cref="BodyComponent.Gravity"/> is
/// only on or off, and the simulation's damping is global, so slowing one body down that way would
/// slow every cube with it. The workable per-body lever is the same one
/// <see cref="SlidingCubeComponent"/> uses: adjust the velocity in
/// <see cref="ISimulationUpdate.SimulationUpdate"/>, before the solver runs.
/// </para>
/// <para>
/// Each tick the integrator adds one tick of full gravity; this component removes
/// (1 - <see cref="GravityScale"/>) of it just before, so the net acceleration is gravity scaled.
/// Cancelling acceleration rather than clamping speed keeps the motion natural: the letters still
/// accelerate, bounce and tumble.
/// </para>
/// </remarks>
[ComponentCategory("Cube Collapse")]
public class SlowFallComponent : BodyComponent, ISimulationUpdate
{
    /// <summary>
    /// Gets or sets how much of normal gravity applies, from 0 (hangs in the air) to 1 (ordinary
    /// fall). Defaults to 0.15.
    /// </summary>
    public float GravityScale { get; set; } = 0.15f;

    /// <summary>
    /// Cancels the unwanted share of this tick's gravity, leaving the body on scaled gravity.
    /// </summary>
    /// <param name="simulation">The simulation stepping this body.</param>
    /// <param name="simTimeStep">The fixed time step, in seconds.</param>
    public void SimulationUpdate(BepuSimulation simulation, float simTimeStep)
    {
        // A sleeping body is resting on something; waking it every tick to cancel gravity it is not
        // experiencing would just keep it from ever sleeping
        if (!Awake) return;

        LinearVelocity -= simulation.PoseGravity * ((1f - GravityScale) * simTimeStep);
    }

    /// <summary>
    /// Method called after the simulation has run on the body.
    /// </summary>
    /// <param name="simulation">The simulation that stepped this body.</param>
    /// <param name="simTimeStep">The fixed time step, in seconds.</param>
    /// <remarks>Does nothing; the whole correction happens before the solve.</remarks>
    public void AfterSimulationUpdate(BepuSimulation simulation, float simTimeStep) { }
}