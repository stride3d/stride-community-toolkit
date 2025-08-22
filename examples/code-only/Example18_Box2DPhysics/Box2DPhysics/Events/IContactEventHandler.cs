namespace Example18_Box2DPhysics.Box2DPhysics.Events;

public interface IContactEventHandler
{
    void OnContactEvent(ContactEventData eventData);
}