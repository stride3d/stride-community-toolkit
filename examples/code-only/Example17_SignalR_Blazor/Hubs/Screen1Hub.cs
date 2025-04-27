using Microsoft.AspNetCore.SignalR;

namespace Example17_SignalR_Blazor.Hubs;

public class Screen1Hub : Hub
{
    public Task SendMessage(MessageDto dto)
        => Clients.All.SendAsync(Constants.ReceiveMessageMethod, dto);

    public Task SendCount(CountDto dto)
        => Clients.All.SendAsync(Constants.ReceiveCountMethod, dto);

    public Task SendUnitsRemoved(CountDto dto)
        => Clients.All.SendAsync(Constants.ReceiveCountMethod, dto);
}