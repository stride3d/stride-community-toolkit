using E13_SignalR.SignalR;
using E13_SignalR.Station;
using E13_SignalR_Shared;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace E13_SignalR.Net;

/// <summary>
/// The game's end of the hub, typed. Inbound requests are queued as they arrive on SignalR's
/// threads and replayed on the game thread by <see cref="Drain"/>; outbound reports go through one
/// background queue in order. Everything is optional: with no hub, <see cref="Drain"/> finds
/// nothing and the reports are dropped.
/// </summary>
public sealed class StationLink : IAsyncDisposable
{
    private readonly SignalRHubClient _client;
    private readonly OutgoingQueue _outgoing;

    // One queue for every kind of request, so they replay in the order they arrived
    private readonly ConcurrentQueue<Action<StationCommands>> _inbox = new();

    public StationLink(Uri hubUrl, ILogger? logger = null)
    {
        _client = new SignalRHubClient(hubUrl, logger);
        _outgoing = _client.CreateOutgoingQueue();

        // Handlers only enqueue. They run on SignalR's threads, and the deck is not theirs to touch.
        _client.RegisterHandler<ReleaseRequest>(nameof(IStationClient.ReleaseRequested), request => _inbox.Enqueue(commands => commands.Release(request)));
        _client.RegisterHandler<int>(nameof(IStationClient.BatchRequested), count => _inbox.Enqueue(commands => commands.ReleaseBatch(count)));
        _client.RegisterHandler(nameof(IStationClient.ClearRequested), () => _inbox.Enqueue(commands => commands.Clear()));
        _client.RegisterHandler(nameof(IStationClient.ShakeRequested), () => _inbox.Enqueue(commands => commands.Shake()));
        _client.RegisterHandler<string>(nameof(IStationClient.SchemeRequested), name => _inbox.Enqueue(commands => commands.SetScheme(name)));
        _client.RegisterHandler<string>(nameof(IStationClient.HailReceived), text => _inbox.Enqueue(commands => commands.Hail(text)));
    }

    public bool IsConnected => _client.IsConnected;

    /// <summary>Starts connecting in the background and keeps trying. Returns at once and never throws.</summary>
    public void BeginConnect() => _client.BeginConnect();

    /// <summary>Replays every request that arrived since the last frame. Call from the game's update.</summary>
    public void Drain(StationCommands target)
    {
        while (_inbox.TryDequeue(out var command))
        {
            command(target);
        }
    }

    public void ReportReleased(ContainerEvent container) => Send(nameof(IStationHub.ReportReleased), container);

    public void ReportLanded(ContainerEvent container) => Send(nameof(IStationHub.ReportLanded), container);

    public void ReportLost(ContainerEvent container) => Send(nameof(IStationHub.ReportLost), container);

    public void ReportCleared(int removed) => Send(nameof(IStationHub.ReportCleared), removed);

    public void ReportScheme(string name) => Send(nameof(IStationHub.ReportScheme), name);

    public void ReportDeck(DeckSnapshot snapshot) => Send(nameof(IStationHub.ReportDeck), snapshot);

    public ValueTask DisposeAsync() => _client.DisposeAsync();

    private void Send(string method, object payload)
    {
        // Not queued while offline: nothing would deliver it, and the next census after the link
        // returns tells the page everything a missed report would have
        if (!IsConnected) return;

        _outgoing.Enqueue(method, payload);
    }
}