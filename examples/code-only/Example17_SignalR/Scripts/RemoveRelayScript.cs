using Example17_SignalR.Core;
using Example17_SignalR.Services;
using Example17_SignalR_Shared.Dtos;
using Stride.Engine;
using Stride.Engine.Events;

namespace Example17_SignalR.Scripts;

/// <summary>
/// Relays remove requests to the hub via ScreenService. No scene graph access here.
/// </summary>
public sealed class RemoveRelayScript : AsyncScript
{
    private ScreenService? _screenService;

    public override async Task Execute()
    {
        _screenService = Services.GetService<ScreenService>();

        if (_screenService is null) return;

        var removeRequestReceiver = new EventReceiver<CountDto>(GlobalEvents.RemoveRequestEventKey);

        while (Game.IsRunning)
        {
            if (removeRequestReceiver.TryReceive(out var removeDto))
            {
                _screenService.EnqueueUnitsRemoved(removeDto);
            }

            await Script.NextFrame();
        }
    }
}