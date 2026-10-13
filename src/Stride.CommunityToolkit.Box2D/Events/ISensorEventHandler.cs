namespace Stride.CommunityToolkit.Box2D;

/// <summary>
/// Implemented by objects that want to receive sensor overlap events from the simulation.
/// Register with <see cref="Box2DSimulation.RegisterSensorEventHandler"/>.
/// </summary>
public interface ISensorEventHandler
{
    /// <summary>Called for every dispatched sensor event.</summary>
    /// <param name="eventData">The event details.</param>
    void OnSensorEvent(SensorEventData eventData);
}