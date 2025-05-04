using Example17_SignalR.Core;
using Example17_SignalR_Shared.Core;
using Example17_SignalR_Shared.Dtos;
using Example17_SignalR_Shared.Interfaces;
using Microsoft.AspNetCore.SignalR.Client;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Helpers;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Engine;
using Stride.Input;
using Stride.Rendering;
using System.Collections.Concurrent;

namespace Example17_SignalR.Scripts;

public class ScreenManagerScript2 : AsyncScript
{
    private readonly Dictionary<EntityType, Material> _materials = [];
    private MaterialBuilder? _materialBuilder;
    private HubConnection? _connection;
    private readonly FixedSizeQueue _messageQueue = new(10);
    private readonly ConcurrentQueue<CountDto> _primitiveCreationQueue = new();
    private bool _isCreatingPrimitives;

    private void QueuePrimitiveCreation(CountDto countDto)
    {
        _primitiveCreationQueue.Enqueue(countDto);
    }

    public override async Task Execute()
    {
        //var screenService = Services.GetService<ScreenService>();

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
            _messageQueue.Enqueue(dto);

            var encodedMsg = $"From Hub: {dto.Type}: {dto.Text}";

            Console.WriteLine(encodedMsg);
        });

        _connection.On<CountDto>(nameof(IScreenClient.ReceiveCountAsync), (dto) =>
        {
            QueuePrimitiveCreation(dto);

            var encodedMsg = $"From Hub: {dto.Type}: {dto.Count}";

            Console.WriteLine(encodedMsg);
        });

        _materialBuilder = new MaterialBuilder(Game.GraphicsDevice);

        AddMaterials();

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
            PrintMessage();

            if (Input.IsMouseButtonPressed(MouseButton.Left))
            {
                QueuePrimitiveCreation(new CountDto
                {
                    Type = EntityType.Primary,
                    Count = 10,
                });
            }

            ProcessPrimitiveQueue();

            await Script.NextFrame();
        }
    }

    private void ProcessPrimitiveQueue()
    {
        if (_isCreatingPrimitives) return;

        if (_primitiveCreationQueue.TryDequeue(out CountDto? nextBatch))
        {
            _isCreatingPrimitives = true;

            CreatePrimitives(nextBatch);

            _isCreatingPrimitives = false;
        }
    }

    private void CreatePrimitives(CountDto countDto)
    {
        var formattedMessage = $"From Script: {countDto.Type}: {countDto.Count}";

        Console.WriteLine(formattedMessage);

        for (var i = 0; i < countDto.Count; i++)
        {
            var entity = Game.Create3DPrimitive(PrimitiveModelType.Cube,
                new()
                {
                    EntityName = $"Entity",
                    Material = _materials[countDto.Type],
                });

            entity.Transform.Position = VectorHelper.RandomVector3([-5, 5], [5, 10], [-5, 5]);
            entity.Scene = Entity.Scene;
        }
    }

    private void PrintMessage()
    {
        if (_messageQueue.Count == 0) return;

        var messages = _messageQueue.AsSpan();

        for (int i = 0; i < messages.Length; i++)
        {
            var message = messages[i];

            if (message == null) continue;

            DebugText.Print(message.Text, new(5, 30 + i * 18), Colours.ColourTypes[message.Type]);
        }
    }

    private void AddMaterials()
    {
        if (_materialBuilder == null) return;

        foreach (var colorType in Colours.ColourTypes)
        {
            var material = _materialBuilder.CreateMaterial(colorType.Value);

            _materials.Add(colorType.Key, material);
        }
    }
}