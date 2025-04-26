using Microsoft.AspNetCore.SignalR;

namespace Example17_SignalR_Blazor.Hubs;

public class Screen1Hub : Hub
{
    public async Task SendMessage(string user, string message)
    {
        await Clients.All.SendAsync("ReceiveMessage", user, message);
    }
}