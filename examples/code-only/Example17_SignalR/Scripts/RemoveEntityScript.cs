using Example17_SignalR.Core;
using Example17_SignalR_Shared.Core;
using Example17_SignalR_Shared.Dtos;
using Stride.Engine;

namespace Example17_SignalR.Scripts;

public class RemoveEntityScript : AsyncScript
{
    private bool _isBeingRemoved;

    public override async Task Execute()
    {
        var robotComponent = Entity.Get<RobotComponent>();

        if (robotComponent is null) return;

        while (Game.IsRunning)
        {
            if (robotComponent.IsDeleted && !_isBeingRemoved)
            {
                _isBeingRemoved = true;

                Console.WriteLine($"Removing entity: {Entity.Name}");

                if (robotComponent.Type != EntityType.Destroyer)
                {
                    var message = new CountDto(robotComponent.Type, 1);

                    GlobalEvents.RemoveRequestEventKey.Broadcast(message);
                }

                Entity.Scene = null;
            }

            await Script.NextFrame();
        }
    }
}