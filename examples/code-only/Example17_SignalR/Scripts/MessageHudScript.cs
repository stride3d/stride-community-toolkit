using Example17_SignalR.Core;
using Example17_SignalR_Shared.Dtos;
using Stride.Engine;
using Stride.Engine.Events;

namespace Example17_SignalR.Scripts;

/// <summary>
/// Displays messages received from the hub using Stride DebugText.
/// </summary>
public sealed class MessageHudScript : AsyncScript
{
    private MessagePrinter? _printer;

    public override async Task Execute()
    {
        _printer = new MessagePrinter(DebugText);

        var messageReceiver = new EventReceiver<MessageDto>(GlobalEvents.MessageReceivedEventKey);

        while (Game.IsRunning)
        {
            if (messageReceiver.TryReceive(out var messageDto))
            {
                _printer.Enqueue(messageDto);
            }

            _printer.PrintMessage();

            await Script.NextFrame();
        }
    }
}