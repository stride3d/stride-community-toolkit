using Microsoft.AspNetCore.SignalR.Client;

namespace Example17_SignalR_Blazor.Components.Pages;

public partial class Home
{
    private HubConnection? _hubConnection;
    private readonly List<string> messages = [];

    protected override async Task OnInitializedAsync()
    {
        _hubConnection = new HubConnectionBuilder()
            .WithUrl(Navigation.ToAbsoluteUri(Constants.HubUrl))
            .Build();

        _hubConnection.On(Constants.ReceiveMessageMethod, (MessageDto dto) =>
        {
            var encodedMsg = $"{dto.Type}: {dto.Text}";

            messages.Add(encodedMsg);

            InvokeAsync(StateHasChanged);
        });

        _hubConnection.On(Constants.ReceiveCountMethod, (CountDto dto) =>
        {
            var encodedMsg = $"{dto.Type}: {dto.Count}";

            messages.Add(encodedMsg);

            InvokeAsync(StateHasChanged);
        });

        await _hubConnection.StartAsync();
    }

    public bool IsConnected =>
        _hubConnection?.State == HubConnectionState.Connected;

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection is not null)
        {
            await _hubConnection.DisposeAsync();
        }
    }

    private async Task OnMessageCallback(MessageDto dto)
    {
        if (_hubConnection is null) return;

        await _hubConnection.SendAsync(Constants.SendMessageMethod, dto);
    }

    private async Task OnCountCallback(CountDto dto)
    {
        if (_hubConnection is null) return;

        await _hubConnection.SendAsync(Constants.SendCountMethod, dto);
    }
}