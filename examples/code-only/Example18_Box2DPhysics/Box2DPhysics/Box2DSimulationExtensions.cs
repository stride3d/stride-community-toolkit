namespace Example18_Box2DPhysics.Box2DPhysics;

public interface ISimulationUpdate
{
    void SimulationUpdate(Box2DSimulation simulation, float deltaTime);
    void AfterSimulationUpdate(Box2DSimulation simulation, float deltaTime);
}