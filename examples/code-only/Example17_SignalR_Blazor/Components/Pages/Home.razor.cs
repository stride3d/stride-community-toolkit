using Example17_SignalR_Blazor.Hubs;
using Example17_SignalR_Shared.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace Example17_SignalR_Blazor.Components.Pages;

public partial class Home(NavigationManager navigation) : IAsyncDisposable
{
    private readonly NavigationManager _navigation = navigation;
    private HubConnection? _hubConnection;
    private readonly List<MessageDto> _messages = [];
    private int _totalEntitiesRequested;
    private int _totalEntitiesRemoved;
    private readonly Queue<string> _funnyMessages = new(
    [
        "Why did the programmer quit his job?",
        "Because he didn't get arrays!",
        "Is your refrigerator running?",
        "Better go catch it!",
        "I told my wife she was drawing her eyebrows too high.",
        "She looked surprised.",
        "My code doesn't work and I don't know why.",
        "My code works and I don't know why.",
        "Just burned 2,000 calories.",
        "That's the last time I leave brownies in the oven while napping.",
        "I'm on a seafood diet.",
        "I see food and I eat it.",
        "Why don't scientists trust atoms?",
        "Because they make up everything!",
        "I'd tell you a UDP joke, but you might not get it.",
        "What's the object-oriented way to become wealthy?",
        "Inheritance!",
        "I would tell you a joke about null references.",
        "But then I'd have nothing to say.",
        "Did you hear about the mathematician who's afraid of negative numbers?",
        "He'll stop at nothing to avoid them!"
    ]);

    protected override async Task OnInitializedAsync()
    {
        _hubConnection = new HubConnectionBuilder()
            .WithUrl(_navigation.ToAbsoluteUri(Constants.HubUrl))
            .Build();

        _hubConnection.On(nameof(IScreenClient.ReceiveMessageAsync), (MessageDto dto) =>
        {
            _messages.Add(dto);

            InvokeAsync(StateHasChanged);
        });

        _hubConnection.On(nameof(IScreenClient.ReceiveCountAsync), (CountDto dto) =>
        {
            _messages.Add(new()
            {
                Text = dto.Count.ToString(),
                Type = dto.Type
            });

            InvokeAsync(StateHasChanged);
        });

        _hubConnection.On(nameof(IScreenClient.ReceiveUnitsRemovedAsync), (CountDto dto) =>
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

    private async Task OnMessageCallback(MessageDto dto)
    {
        if (_hubConnection is null) return;

        UpdateMessageText(dto);

        await _hubConnection.SendAsync(nameof(Screen1Hub.SendMessage), dto);
    }

    private async Task OnCountCallback(CountDto dto)
    {
        if (_hubConnection is null) return;

        _totalEntitiesRequested += dto.Count;

        await _hubConnection.SendAsync(nameof(Screen1Hub.SendCount), dto);
    }

    private void UpdateMessageText(MessageDto dto)
    {
        if (dto.Text != Constants.DefaultMessage) return;

        if (_funnyMessages.Count == 0) return;

        dto.Text = _funnyMessages.Dequeue();

        _funnyMessages.Enqueue(dto.Text);
    }

    public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection is null) return;

        await _hubConnection.DisposeAsync();
    }
}