using Example17_SignalR.Services;
using Stride.Engine;

namespace Example17_SignalR.Scripts;

/// <summary>
/// Starts the ScreenService and drains buffered hub events on the main thread every frame.
/// </summary>
public sealed class HubEventPumpScript : AsyncScript
{
    private ScreenService? _screenService;

    public HubEventPumpScript()
    {
        // Run early to make received events available to other scripts
        Priority = -100;
    }

    public override async Task Execute()
    {
        _screenService = Services.GetService<ScreenService>();

        if (_screenService is null) return;

        try
        {
            await _screenService.EnsureStartedAsync(CancellationToken);
        }
        catch
        {
            // Swallow; reconnect will be attempted by the service
        }

        try
        {
            while (Game.IsRunning)
            {
                _screenService.DrainEvents();

                await Script.NextFrame();
            }
        }
        finally
        {
            try { await _screenService.StopAsync(); } catch { }
        }
    }
}