using Example17_SignalR.Core;
using Example17_SignalR_Shared.Dtos;
using Example17_SignalR_Shared.Interfaces;
using Microsoft.AspNetCore.SignalR.Client;

namespace Example17_SignalR.Services;

public class ScreenService
{
    public HubConnection Connection { get; set; }

    public ScreenService()
    {
        Connection = new HubConnectionBuilder()
              .WithUrl("https://localhost:44369/screen1")
              .Build();

        Connection.Closed += async (error) =>
        {
            await Task.Delay(new Random().Next(0, 5) * 1000);
            await Connection.StartAsync();
        };

        Connection.On<MessageDto>(nameof(IScreenClient.ReceiveMessageAsync), (dto) =>
        {
            GlobalEvents.MessageReceivedEventKey.Broadcast(dto);

            var encodedMsg = $"From Hub: {dto.Type}: {dto.Text}";

            Console.WriteLine(encodedMsg);
        });

        Connection.On<CountDto>(nameof(IScreenClient.ReceiveCountAsync), (dto) =>
        {
            GlobalEvents.CountReceivedEventKey.Broadcast(dto);

            var encodedMsg = $"From Hub: {dto.Type}: {dto.Count}";

            Console.WriteLine(encodedMsg);
        });
    }
}