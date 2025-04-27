using Microsoft.AspNetCore.SignalR.Client;

namespace Example17_SignalR_Blazor.Components.Pages;

public partial class Home
{
    private HubConnection? _hubConnection;
    private readonly List<MessageDto> _messages = [];
    private int _totalEntitiesRequested;
    private int _totalEntitiesRemoved;

    protected override async Task OnInitializedAsync()
    {
        _hubConnection = new HubConnectionBuilder()
            .WithUrl(Navigation.ToAbsoluteUri(Constants.HubUrl))
            .Build();

        _hubConnection.On(Constants.ReceiveMessageMethod, (MessageDto dto) =>
        {
            _messages.Add(dto);

            InvokeAsync(StateHasChanged);
        });

        _hubConnection.On(Constants.ReceiveCountMethod, (CountDto dto) =>
        {
            _messages.Add(new()
            {
                Text = dto.Count.ToString(),
                Type = dto.Type
            });

            InvokeAsync(StateHasChanged);
        });

        _hubConnection.On("SendUnitsRemoved", (CountDto dto) =>
        {
            _messages.Add(new()
            {
                Text = $"Removed: {dto.Count}",
                Type = dto.Type
            });

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

        _totalEntitiesRequested += dto.Count;

        await _hubConnection.SendAsync(Constants.SendCountMethod, dto);
    }
}