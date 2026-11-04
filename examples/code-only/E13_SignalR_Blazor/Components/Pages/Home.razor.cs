using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.SignalR.Client;

namespace E13_SignalR_Blazor.Components.Pages;

/// <summary>
/// The web console. A hub client like the game is, with the same shared contract: it sends
/// commands with <c>nameof(IStationHub.X)</c> and registers for events with
/// <c>nameof(IStationClient.Y)</c>. Everything it knows about the deck arrived from the game.
/// </summary>
public partial class Home(NavigationManager navigation) : IAsyncDisposable
{
    private const int FeedLength = 40;

    /// <summary>The game sends a census every second; three missed ones and it is considered gone.</summary>
    private static readonly TimeSpan GameTimeout = TimeSpan.FromSeconds(3.5);

    private readonly List<FeedEntry> _feed = [];
    private readonly PeriodicTimer _lampTimer = new(TimeSpan.FromSeconds(1));

    private HubConnection? _hub;
    private Scheme _scheme = Schemes.Default;
    private DeckSnapshot? _deck;
    private DateTime _lastCensus = DateTime.MinValue;
    private string? _hailText;
    private int _sequence;

    private bool IsHubConnected => _hub?.State == HubConnectionState.Connected;

    private bool IsGameOnline => DateTime.UtcNow - _lastCensus < GameTimeout;

    protected override async Task OnInitializedAsync()
    {
        _hub = new HubConnectionBuilder()
            .WithUrl(navigation.ToAbsoluteUri(Constants.HubPath))
            .WithAutomaticReconnect()
            .Build();

        // Handlers run on SignalR's threads; InvokeAsync moves the state change and the render onto the circuit
        _hub.On<ContainerEvent>(nameof(IStationClient.ContainerReleased), container => Post("released", $"Released #{container.Id} {Describe(container)} from {container.Origin.ToString().ToLowerInvariant()}", container.Paint));
        _hub.On<ContainerEvent>(nameof(IStationClient.ContainerLanded), container => Post("landed", $"Landed #{container.Id} {Describe(container)} at {Where(container)} after {container.AirTime:0.0} s", container.Paint));
        _hub.On<ContainerEvent>(nameof(IStationClient.ContainerLost), container => Post("lost", $"Lost #{container.Id} {Describe(container)} over the edge", container.Paint));
        _hub.On<int>(nameof(IStationClient.DeckCleared), removed => Post("cleared", $"Deck cleared, {removed} removed"));

        _hub.On<DeckSnapshot>(nameof(IStationClient.DeckUpdated), snapshot => InvokeAsync(() =>
        {
            _deck = snapshot;
            _lastCensus = DateTime.UtcNow;

            // The census carries the game's scheme, so a page that opens late is in the right colours
            // after the first one without any special handling
            ApplyScheme(snapshot.Scheme);

            StateHasChanged();
        }));

        // From the game, whichever console chose it. Posted even when this page chose it and has
        // already switched: the line is the game confirming that it followed.
        _hub.On<string>(nameof(IStationClient.SchemeChanged), name => InvokeAsync(() =>
        {
            ApplyScheme(name);
            Post("scheme", $"Scheme {name}, confirmed by the game");
        }));

        // From another browser tab, relayed before the game has confirmed it. Applying it now keeps
        // the tabs in step even when the game is offline.
        _hub.On<string>(nameof(IStationClient.SchemeRequested), name => InvokeAsync(() =>
        {
            ApplyScheme(name);
            StateHasChanged();
        }));

        _hub.Reconnecting += _ => InvokeAsync(StateHasChanged);
        _hub.Reconnected += _ => InvokeAsync(StateHasChanged);
        _hub.Closed += _ => InvokeAsync(StateHasChanged);

        try
        {
            await _hub.StartAsync();
        }
        catch (Exception exception)
        {
            Post("lost", $"Hub not reachable: {exception.Message}");
        }

        // The GAME lamp goes out by the absence of a message, and nothing else re-renders for that
        _ = TickAsync();
    }

    private async Task TickAsync()
    {
        try
        {
            while (await _lampTimer.WaitForNextTickAsync())
            {
                await InvokeAsync(StateHasChanged);
            }
        }
        catch (OperationCanceledException)
        {
            // The timer was disposed with the page
        }
    }

    private Task ReleaseAsync(ContainerSize? size, ContainerPaint? paint)
        => SendAsync(nameof(IStationHub.ReleaseContainer), new ReleaseRequest(size, paint));

    private Task ReleaseBatchAsync() => SendAsync(nameof(IStationHub.ReleaseBatch), Constants.BatchSize);

    private Task ClearAsync() => SendAsync(nameof(IStationHub.ClearDeck));

    private Task ShakeAsync() => SendAsync(nameof(IStationHub.ShakeDeck));

    private async Task SelectSchemeAsync(Scheme scheme)
    {
        // Applied here at once - the page should not wait for a round trip through the game to
        // change its own colours - and then requested, so the game and the other tabs follow
        ApplyScheme(scheme.Name);

        await SendAsync(nameof(IStationHub.SetScheme), scheme.Name);
    }

    private async Task HailAsync()
    {
        if (string.IsNullOrWhiteSpace(_hailText)) return;

        var text = _hailText.Trim();

        _hailText = null;

        await SendAsync(nameof(IStationHub.Hail), text);

        Post("hail", $"Hailed the game: {text}");
    }

    private Task HailOnEnterAsync(KeyboardEventArgs args) => args.Key == "Enter" ? HailAsync() : Task.CompletedTask;

    private async Task SendAsync(string method, object? argument = null)
    {
        if (_hub is null || !IsHubConnected) return;

        try
        {
            if (argument is null)
            {
                await _hub.SendAsync(method);
            }
            else
            {
                await _hub.SendAsync(method, argument);
            }
        }
        catch (Exception exception)
        {
            Post("lost", $"{method} failed: {exception.Message}");
        }
    }

    private bool ApplyScheme(string name)
    {
        var scheme = Schemes.Find(name);

        if (scheme is null || scheme == _scheme) return false;

        _scheme = scheme;

        return true;
    }

    private void Post(string kind, string text, ContainerPaint? paint = null) => InvokeAsync(() =>
    {
        _feed.Insert(0, new FeedEntry(++_sequence, DateTime.Now, kind, text, paint));

        if (_feed.Count > FeedLength)
        {
            _feed.RemoveAt(_feed.Count - 1);
        }

        StateHasChanged();
    });

    private static string Describe(ContainerEvent container) => $"{container.Size.ToString().ToLowerInvariant()} {container.Paint.ToString().ToLowerInvariant()}";

    private static string Where(ContainerEvent container) => container.Position is { } p ? $"({p.X:0.0}, {p.Z:0.0})" : "?";

    private static string FormatUptime(float seconds) => TimeSpan.FromSeconds(seconds).ToString(@"h\:mm\:ss");

    private static int Count(int[]? counts, int index) => counts is not null && index < counts.Length ? counts[index] : 0;

    /// <summary>A bar's width as a share of the largest count in its group, so the longest bar is always full.</summary>
    private static string BarWidth(int[]? counts, int index)
    {
        if (counts is null || counts.Length == 0) return "0%";

        var max = counts.Max();

        return max == 0 ? "0%" : $"{100 * counts[index] / max}%";
    }

    public async ValueTask DisposeAsync()
    {
        // There is no finalizer to suppress here, but CA1816 asks for the call and it costs nothing
        GC.SuppressFinalize(this);

        _lampTimer.Dispose();

        if (_hub is not null)
        {
            await _hub.DisposeAsync();
        }
    }

    /// <summary>One line of the feed. Sequence is the render key; the paint puts a swatch in front of the text.</summary>
    private sealed record FeedEntry(int Sequence, DateTime At, string Kind, string Text, ContainerPaint? Paint);
}