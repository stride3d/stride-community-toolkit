using Example17_SignalR.Core;
using Example17_SignalR.Services;
using Example17_SignalR_Shared.Dtos;
using Microsoft.AspNetCore.SignalR.Client;
using Stride.Engine;

namespace Example17_SignalR.Scripts;

public class RemoveEntityScript2 : AsyncScript
{
    private bool _isBeingRemoved;

    public override async Task Execute()
    {
        var robotComponent = Entity.Get<RobotComponent>();
        var screenService = Services.GetService<ScreenService>();

        if (screenService is null) return;
        if (robotComponent is null) return;

        while (Game.IsRunning)
        {
            //if (!robotComponent.IsDeleted) return;

            if (robotComponent.IsDeleted && !_isBeingRemoved)
            {
                _isBeingRemoved = true;

                Console.WriteLine($"Removing entity: {Entity.Name}");

                var message = new CountDto(robotComponent.Type, 1);

                await screenService.Connection.SendAsync("SendUnitsRemoved", message);

                Entity.Scene = null;
            }

            await Script.NextFrame();
        }
    }
}