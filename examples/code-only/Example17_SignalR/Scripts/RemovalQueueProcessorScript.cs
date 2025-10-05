using Example17_SignalR.Core;
using Example17_SignalR_Shared.Core;
using Example17_SignalR_Shared.Dtos;
using Stride.Engine;

namespace Example17_SignalR.Scripts;

/// <summary>
/// Drains entities enqueued for removal by <see cref="ContactTriggerHandler"/>,
/// broadcasts a removal request to the SignalR layer, and detaches entities from the scene.
/// </summary>
public class RemovalQueueProcessorScript : AsyncScript
{
    /// <summary>
    /// Micro-thread entry point. Polls the shared removal queue each frame and processes items.
    /// </summary>
    public override async Task Execute()
    {
        while (Game.IsRunning)
        {
            DrainRemovalQueue();

            await Script.NextFrame();
        }
    }

    /// <summary>
    /// Dequeues pending entities and removes them from the scene after broadcasting a removal request.
    /// </summary>
    private void DrainRemovalQueue()
    {
        while (ContactTriggerHandler.RemovalQueue.TryDequeue(out var entity))
        {
            if (entity is null) continue;

            BroadcastEntityRemovalRequest(entity);

            if (entity.Scene != null)
            {
                entity.Scene = null;
            }
        }
    }

    /// <summary>
    /// Broadcasts a <see cref="CountDto"/> to notify the hub about a unit removal.
    /// Destroyer entities are ignored.
    /// </summary>
    private void BroadcastEntityRemovalRequest(Entity entity)
    {
        var robotComponent = entity.Get<RobotComponent>();

        if (robotComponent is null) return;

        if (robotComponent.Type == EntityType.Destroyer)
        {
            return;
        }

        var message = new CountDto(robotComponent.Type, 1);

        GlobalEvents.RemoveRequestEventKey.Broadcast(message);
    }
}