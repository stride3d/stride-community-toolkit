using Example17_SignalR.Builders;
using Example17_SignalR.Core;
using Example17_SignalR.Managers;
using Example17_SignalR_Shared.Core;
using Example17_SignalR_Shared.Dtos;
using Stride.Engine;
using Stride.Engine.Events;
using Stride.Input;

namespace Example17_SignalR.Scripts;

/// <summary>
/// Spawns robots from CountDto events, throttled across frames, and also supports demo input spawns.
/// </summary>
public sealed class RobotSpawnScript : AsyncScript
{
    private readonly Queue<CountDto> _primitiveCreationQueue = new();

    private RobotBuilder? _robotBuilder;
    private MaterialManager? _materialManager;

    // Creation throttling state
    private CountDto? _currentCreationBatch;
    private int _currentCreationIndex;
    public int MaxCreatesPerFrame { get; set; } = 25; // tune per hardware

    public override async Task Execute()
    {
        _materialManager = new MaterialManager(new MaterialBuilder(Game.GraphicsDevice));
        _robotBuilder = new RobotBuilder(Game);

        var countReceiver = new EventReceiver<CountDto>(GlobalEvents.CountReceivedEventKey);

        while (Game.IsRunning)
        {
            if (countReceiver.TryReceive(out var countDto))
            {
                QueuePrimitiveCreation(countDto);
            }

            // Optional demo input spawn
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
        // If no active batch, try to fetch one
        if (_currentCreationBatch is null)
        {
            if (_primitiveCreationQueue.Count == 0) return;

            _currentCreationBatch = _primitiveCreationQueue.Dequeue();
            _currentCreationIndex = 0;
        }

        // Process a limited number per frame
        var remaining = _currentCreationBatch.Count - _currentCreationIndex;
        var toCreate = Math.Min(MaxCreatesPerFrame, remaining);

        for (int i = 0; i < toCreate; i++)
        {
            var id = _currentCreationIndex + i;
            _robotBuilder!.CreateRobot(
                id,
                _currentCreationBatch.Type,
                Entity.Scene!,
                _materialManager!.GetMaterial(_currentCreationBatch.Type)
                );
        }

        _currentCreationIndex += toCreate;

        // If finished, clear current batch so next one can be dequeued next frame
        if (_currentCreationIndex >= _currentCreationBatch.Count)
        {
            _currentCreationBatch = null;
            _currentCreationIndex = 0;
        }
    }
}