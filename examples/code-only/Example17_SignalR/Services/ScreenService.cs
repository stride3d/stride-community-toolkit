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

        Connection.On<string, string>("ReceiveMessage", (user, message) =>
        {
            var encodedMsg = $"{user}: {message}";

            Console.WriteLine(encodedMsg);

            //messages.Add(encodedMsg);
            //StateHasChanged();
        });
    }
}