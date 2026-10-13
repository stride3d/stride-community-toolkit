namespace Stride.CommunityToolkit.Box2D;

/// <summary>
/// Implemented by objects that want to receive contact and hit events from the simulation.
/// Register with <see cref="Box2DSimulation.RegisterContactEventHandler"/>.
/// </summary>
public interface IContactEventHandler
{
    /// <summary>Called for every dispatched contact event.</summary>
    /// <param name="eventData">The event details.</param>
    void OnContactEvent(ContactEventData eventData);
}