using Example17_SignalR_Shared.Dtos;
using Stride.Engine.Events;

namespace Example17_SignalR.Core;

/// <summary>
/// Central event keys to broadcast SignalR data onto the Stride event bus.
/// </summary>
public static class GlobalEvents
{
    private const string Category = "Global";

    /// <summary>
    /// Raised when a count batch was received from the hub.
    /// </summary>
    public static readonly EventKey<CountDto> CountReceivedEventKey = new(Category, "CountReceived");

    /// <summary>
    /// Raised when a message was received from the hub.
    /// </summary>
    public static readonly EventKey<MessageDto> MessageReceivedEventKey = new(Category, "MessageReceived");

    /// <summary>
    /// Raised when the game requests removal notification to the hub.
    /// </summary>
    public static readonly EventKey<CountDto> RemoveRequestEventKey = new(Category, "RemoveRequest");
}