using Example17_SignalR.Core;
using Example17_SignalR.Services;
using Example17_SignalR_Shared.Core;
using Example17_SignalR_Shared.Dtos;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Helpers;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Engine;
using Stride.Engine.Events;
using Stride.Rendering;

namespace Example17_SignalR.Scripts;

public class ScreenManagerScript : AsyncScript
{
    private readonly Dictionary<EntityType, Material> _materials = [];
    private MaterialBuilder? _materialBuilder;
    private readonly FixedSizeQueue _messageQueue = new(10);

    public override async Task Execute()
    {
        var screenService = Services.GetService<ScreenService>();

        if (screenService == null) return;

        _materialBuilder = new MaterialBuilder(Game.GraphicsDevice);

        AddMaterials();

        try
        {
            await screenService.Connection.StartAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error starting connection: {ex.Message}");
        }

        var countReceiver = new EventReceiver<CountDto>(GlobalEvents.CountReceivedEventKey);

        var messageReceiver = new EventReceiver<MessageDto>(GlobalEvents.MessageReceivedEventKey);

        while (Game.IsRunning)
        {
            // This example will be waitig for the event to be received
            // the rest of the code will be executed when the event is received
            //var result = await countReceiver.ReceiveAsync();
            //var formattedMessage = $"From Script: {result.Type}: {result.Count}";
            //Console.WriteLine(formattedMessage);

            // This example will be checking if the event is received
            // the rest of the code will be executed every frame
            if (countReceiver.TryReceive(out var countDto))
            {
                CreatePrimitives(countDto);
            }

            if (messageReceiver.TryReceive(out var messageDto))
            {
                _messageQueue.Enqueue(messageDto);
            }

            PrintMessage();

            await Script.NextFrame();
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