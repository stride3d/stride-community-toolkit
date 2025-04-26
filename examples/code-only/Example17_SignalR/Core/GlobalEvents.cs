using Example17_SignalR_Shared.Dtos;
using Stride.Engine.Events;

namespace Example17_SignalR.Core;

public static class GlobalEvents
{
    public static EventKey<CountDto> CountReceivedEventKey = new EventKey<CountDto>("Global", "CountReceived");
}
