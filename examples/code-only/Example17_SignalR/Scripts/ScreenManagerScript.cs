using Example17_SignalR.Core;
using Example17_SignalR.Services;
using Example17_SignalR_Shared.Core;
using Example17_SignalR_Shared.Dtos;
using Microsoft.AspNetCore.SignalR.Client;
using Stride.Engine;
using Stride.Engine.Events;
using Stride.Input;
using System.Collections.Concurrent;

namespace Example17_SignalR.Scripts;

public class ScreenManagerScript : AsyncScript
{
    private readonly ConcurrentQueue<CountDto> _primitiveCreationQueue = new();
    private readonly ConcurrentQueue<CountDto> _removeRequestQueue = new();
    private RobotBuilder? _primitiveBuilder;
    private MessagePrinter? _messagePrinter;
    private ScreenService? _screenService;
    private bool _isCreatingPrimitives;

    public override async Task Execute()
    {
        _screenService = Services.GetService<ScreenService>();

        if (_screenService == null) return;

        try
        {
            await _screenService.Connection.StartAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error starting connection: {ex.Message}");
        }

        var materialManager = new MaterialManager(new MaterialBuilder(Game.GraphicsDevice));
        _primitiveBuilder = new RobotBuilder(Game, materialManager);

        _messagePrinter = new MessagePrinter(DebugText);

        var countReceiver = new EventReceiver<CountDto>(GlobalEvents.CountReceivedEventKey);
        var messageReceiver = new EventReceiver<MessageDto>(GlobalEvents.MessageReceivedEventKey);
        var removeRequestReceiver = new EventReceiver<CountDto>(GlobalEvents.RemoveRequestEventKey);

        while (Game.IsRunning)
        {
            // This example will be waiting for the event to be received
            // the rest of the code will be executed when the event is received
            //var result = await countReceiver.ReceiveAsync();
            //var formattedMessage = $"From Script: {result.Type}: {result.Count}";
            //Console.WriteLine(formattedMessage);

            // This example will be checking if the event is received
            // the rest of the code will be executed every frame
            if (countReceiver.TryReceive(out var countDto))
            {
                QueuePrimitiveCreation(countDto);
            }

            if (removeRequestReceiver.TryReceive(out var countDto2))
            {
                Console.WriteLine($"Broadcast received");

                QueueRemoveRequest(countDto2);
            }

            if (messageReceiver.TryReceive(out var messageDto))
            {
                _messagePrinter.Enqueue(messageDto);
            }

            _messagePrinter.PrintMessage();

            if (Input.IsMouseButtonPressed(MouseButton.Left))
            {
                Console.WriteLine($"---------------------------------------------------------");

                QueuePrimitiveCreation(new CountDto
                {
                    Type = EntityType.Destroyer,
                    Count = 10,
                });
            }

            ProcessPrimitiveQueue();

            // Fire and forget - don't wait for SignalR operation to complete
            _ = ProcessRemoveQueue();

            await Script.NextFrame();
        }
    }

    private void QueuePrimitiveCreation(CountDto countDto)
        => _primitiveCreationQueue.Enqueue(countDto);

    private void QueueRemoveRequest(CountDto countDto)
        => _removeRequestQueue.Enqueue(countDto);

    private void ProcessPrimitiveQueue()
    {
        if (_isCreatingPrimitives) return;

        if (_primitiveCreationQueue.TryDequeue(out CountDto? nextBatch))
        {
            if (nextBatch == null) return;

            _isCreatingPrimitives = true;

            _primitiveBuilder!.CreatePrimitives(nextBatch, Entity.Scene);

            _isCreatingPrimitives = false;
        }
    }

    private async Task ProcessRemoveQueue()
    {
        if (_removeRequestQueue.TryDequeue(out CountDto? nextRemoveRequest))
        {
            if (nextRemoveRequest == null) return;

            await _screenService!.Connection.SendAsync("SendUnitsRemoved", nextRemoveRequest);
        }
    }
}