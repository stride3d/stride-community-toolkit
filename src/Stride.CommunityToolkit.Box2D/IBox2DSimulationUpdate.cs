namespace Stride.CommunityToolkit.Box2D;

/// <summary>
/// Implemented by objects that want to run logic before and after each simulation update,
/// mirroring the ISimulationUpdate pattern of Stride's Bepu physics integration.
/// </summary>
public interface IBox2DSimulationUpdate
{
    /// <summary>Called before the simulation advances. Does nothing unless implemented.</summary>
    /// <param name="simulation">The simulation being stepped.</param>
    /// <param name="deltaTime">The time step in seconds.</param>
    void SimulationUpdate(Box2DSimulation simulation, float deltaTime) { }

    /// <summary>Called after the simulation has advanced. Does nothing unless implemented.</summary>
    /// <param name="simulation">The simulation that was stepped.</param>
    /// <param name="deltaTime">The time step in seconds.</param>
    void AfterSimulationUpdate(Box2DSimulation simulation, float deltaTime) { }
}