using Example17_SignalR_Shared.Dtos;
using Stride.Engine.Events;

namespace Example17_SignalR.Core;

// Assuming that we will be broadcasting events to multiple places in the code
// we will be using the EventKey class to create a unique key for each event
public static class GlobalEvents
{
    private const string Category = "Global";

    public static readonly EventKey<CountDto> CountReceivedEventKey = new(Category, "CountReceived");
    public static readonly EventKey<MessageDto> MessageReceivedEventKey = new(Category, "MessageReceived");
    public static readonly EventKey<CountDto> RemoveRequestEventKey = new(Category, "RemoveRequest");
}