using Example17_SignalR.Services;
using Stride.Engine;

namespace Example17_SignalR.Scripts;

public class ScreenManagerScript : AsyncScript
{
    public override async Task Execute()
    {
        var screenService = Services.GetService<ScreenService>();

        if (screenService == null) return;

        try
        {
            await screenService.Connection.StartAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error starting connection: {ex.Message}");
        }
    }
}