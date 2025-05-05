using Example17_SignalR_Shared.Dtos;
using Stride.Profiling;

namespace Example17_SignalR.Core;

public class MessagePrinter
{
    private readonly FixedSizeQueue _messageQueue = new(10);
    private readonly DebugTextSystem _debugTextSystem;

    public MessagePrinter(DebugTextSystem debugTextSystem)
    {
        _debugTextSystem = debugTextSystem;
    }

    public void PrintMessage()
    {
        if (_messageQueue.Count == 0) return;

        var messages = _messageQueue.AsSpan();

        for (int i = 0; i < messages.Length; i++)
        {
            var message = messages[i];

            if (message == null) continue;

            _debugTextSystem.Print(message.Text, new(5, 30 + i * 18), Colours.ColourTypes[message.Type]);
        }
    }

    public void Enqueue(MessageDto? message)
    {
        if (message == null) return;

        _messageQueue.Enqueue(message);
    }
}
