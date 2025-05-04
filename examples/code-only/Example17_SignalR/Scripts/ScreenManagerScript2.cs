using Example17_SignalR.Core;
using Example17_SignalR_Shared.Core;
using Example17_SignalR_Shared.Dtos;
using Example17_SignalR_Shared.Interfaces;
using Microsoft.AspNetCore.SignalR.Client;
using Stride.Engine;
using Stride.Input;
using System.Collections.Concurrent;

namespace Example17_SignalR.Scripts;

public class ScreenManagerScript2 : AsyncScript
{
    private readonly ConcurrentQueue<CountDto> _primitiveCreationQueue = new();
    private HubConnection? _connection;
    private PrimitiveBuilder? _primitiveBuilder;
    private MessagePrinter? _messagePrinter;
    private bool _isCreatingPrimitives;

    public override async Task Execute()
    {
        var materialManager = new MaterialManager(new MaterialBuilder(Game.GraphicsDevice));
        _primitiveBuilder = new PrimitiveBuilder(Game, materialManager);

        _messagePrinter = new MessagePrinter(DebugText);

        _connection = new HubConnectionBuilder()
              .WithUrl("https://localhost:44369/screen1")
              .Build();

        _connection.Closed += async (error) =>
        {
            await Task.Delay(new Random().Next(0, 5) * 1000);
            await _connection.StartAsync();
        };

        _connection.On<MessageDto>(nameof(IScreenClient.ReceiveMessageAsync), (dto) =>
        {
            _messagePrinter.Enqueue(dto);

            var encodedMsg = $"From Hub: {dto.Type}: {dto.Text}";

            Console.WriteLine(encodedMsg);
        });

        _connection.On<CountDto>(nameof(IScreenClient.ReceiveCountAsync), (dto) =>
        {
            QueuePrimitiveCreation(dto);

            var encodedMsg = $"From Hub: {dto.Type}: {dto.Count}";

            Console.WriteLine(encodedMsg);
        });

        try
        {
            await _connection.StartAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error starting connection: {ex.Message}");
        }

        while (Game.IsRunning)
        {
            _messagePrinter.PrintMessage();

            if (Input.IsMouseButtonPressed(MouseButton.Left))
            {
                QueuePrimitiveCreation(new CountDto
                {
                    Type = EntityType.Destroyer,
                    Count = 10,
                });
            }

            ProcessPrimitiveQueue();

            await Script.NextFrame();
        }
    }

    private void QueuePrimitiveCreation(CountDto countDto)
        => _primitiveCreationQueue.Enqueue(countDto);

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
}