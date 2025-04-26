using Example17_SignalR.Core;
using Example17_SignalR.Services;
using Example17_SignalR_Shared.Dtos;
using Stride.Engine;
using Stride.Engine.Events;

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

        var countReceiver = new EventReceiver<CountDto>(GlobalEvents.CountReceivedEventKey);

        while (Game.IsRunning)
        {
            // This example will be waitig for the event to be received
            // the rest of the code will be executed when the event is received
            //var result = await countReceiver.ReceiveAsync();
            //var formattedMessage = $"From Script: {result.Type}: {result.Count}";
            //Console.WriteLine(formattedMessage);

            // This example will be checking if the event is received
            // the rest of the code will be executed every frame
            if (countReceiver.TryReceive(out var result))
            {
                var formattedMessage = $"From Script: {result.Type}: {result.Count}";

                Console.WriteLine(formattedMessage);
            }

            await Script.NextFrame();
        }
    }
}