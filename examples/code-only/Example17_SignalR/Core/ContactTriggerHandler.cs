using Example17_SignalR_Shared.Core;
using Stride.BepuPhysics;
using Stride.BepuPhysics.Definitions.Contacts;
using Stride.Engine;
using System.Collections.Concurrent;

namespace Example17_SignalR.Core;

/// <summary>
/// Handles contact events between entities and enqueues them for removal when colliding with a Destroyer.
/// </summary>
public class ContactTriggerHandler : IContactEventHandler
{
    /// <summary>
    /// Entities pending removal from the scene; drained by <see cref="Scripts.RemovalQueueProcessorScript"/>.
    /// </summary>
    public static readonly ConcurrentQueue<Entity> RemovalQueue = new();

    /// <inheritdoc />
    public bool NoContactResponse => false;

    void IContactEventHandler.OnStartedTouching<TManifold>(CollidableComponent eventSource, CollidableComponent other,
        ref TManifold contactManifold,
        bool flippedManifold,
        int workerIndex,
        BepuSimulation bepuSimulation)
    {
        if (eventSource?.Entity == null || other?.Entity == null)
            return;

        var sourceRobot = eventSource.Entity.Get<RobotComponent>();
        var otherRobot = other.Entity.Get<RobotComponent>();

        if (sourceRobot is null || otherRobot is null) return;

        if (sourceRobot.IsDeleted || otherRobot.IsDeleted) return;

        if (sourceRobot.Type == EntityType.Destroyer && otherRobot.Type == EntityType.Destroyer) return;

        if (sourceRobot.Type == EntityType.Destroyer || otherRobot.Type == EntityType.Destroyer)
        {
            QueueForRemoval(sourceRobot);
            QueueForRemoval(otherRobot);
        }
    }

    /// <summary>
    /// Schedules the entity that owns the given <paramref name="robotComponent"/> for removal.
    /// </summary>
    public static void QueueForRemoval(RobotComponent? robotComponent)
    {
        if (robotComponent is null) return;

        robotComponent.IsDeleted = true;

        var entity = robotComponent.Entity;

        if (entity != null)
        {
            RemovalQueue.Enqueue(entity);
        }
    }
}