using Example17_SignalR_Shared.Dtos;
using Stride.Engine.Events;

namespace Example17_SignalR.Core;

public static class GlobalEvents
{
    private const string Category = "Global";

    public static EventKey<CountDto> CountReceivedEventKey = new(Category, "CountReceived");

    public static EventKey<MessageDto> MessageReceivedEventKey = new(Category, "MessageReceived");
}
