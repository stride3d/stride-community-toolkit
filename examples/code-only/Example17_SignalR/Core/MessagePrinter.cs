using Example17_SignalR_Shared.Dtos;
using Stride.Profiling;

namespace Example17_SignalR.Core;

/// <summary>
/// Buffers and prints incoming messages via Stride's <see cref="DebugTextSystem"/>.
/// </summary>
public class MessagePrinter
{
    private readonly FixedSizeQueue _messageQueue = new(10);
    private readonly DebugTextSystem _debugTextSystem;

    /// <summary>
    /// Creates a new <see cref="MessagePrinter"/>.
    /// </summary>
    /// <param name="debugTextSystem">Stride debug text system used to render text.</param>
    public MessagePrinter(DebugTextSystem debugTextSystem)
    {
        _debugTextSystem = debugTextSystem;
    }

    /// <summary>
    /// Renders the buffered messages on screen.
    /// </summary>
    public void PrintMessage()
    {
        if (_messageQueue.Count == 0) return;

        var messages = _messageQueue.AsSpan();

        for (int i = 0; i < messages.Length; i++)
        {
            var message = messages[i];

            if (message == null) continue;

            _debugTextSystem.Print(message.Text, new(5, 30 + i * 18), Colors.Map[message.Type]);
        }
    }

    /// <summary>
    /// Enqueues a message for later rendering.
    /// </summary>
    public void Enqueue(MessageDto? message)
    {
        if (message == null) return;

        _messageQueue.Enqueue(message);
    }
}