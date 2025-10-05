using Example17_SignalR.Core;
using Example17_SignalR.SignalR;
using Example17_SignalR_Shared.Dtos;
using Example17_SignalR_Shared.Interfaces;
using Microsoft.AspNetCore.SignalR.Client;

namespace Example17_SignalR.Services;

/// <summary>
/// Encapsulates SignalR connection, event buffering and main-thread dispatch via <see cref="GlobalEvents"/>.
/// Also owns a background loop that sequentially forwards removal requests to the hub.
/// </summary>
public class ScreenService
{
    private readonly SignalRHubClient _client;

    private readonly BufferedSubscription<MessageDto> _messages;
    private readonly BufferedSubscription<CountDto> _counts;
    private readonly OutgoingQueue<CountDto> _removals;

    /// <summary>
    /// Active SignalR hub connection.
    /// </summary>
    public HubConnection Connection => _client.Connection;

    public ScreenService(string hubUrl, Microsoft.Extensions.Logging.ILogger<SignalRHubClient>? logger = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hubUrl);

        _client = new SignalRHubClient(new SignalRClientOptions()
        {
            HubUrl = hubUrl
        }, logger);

        // Only enqueue inside callback (keep it very small, no engine interaction / no broadcasts here)
        _messages = _client.RegisterBuffered<MessageDto>(nameof(IScreenClient.ReceiveMessageAsync));
        _counts = _client.RegisterBuffered<CountDto>(nameof(IScreenClient.ReceiveCountAsync));

        // Background sequential sender for units removed
        _removals = _client.CreateOutgoingQueue<CountDto>("SendUnitsRemoved");
    }

    /// <summary>
    /// Drains queued hub events and broadcasts them on the (game) thread calling this method.
    /// Call from the main update loop before EventReceivers.TryReceive.
    /// </summary>
    public void DrainEvents()
    {
        while (_messages.TryDequeue(out var msg))
        {
            GlobalEvents.MessageReceivedEventKey.Broadcast(msg);
        }

        while (_counts.TryDequeue(out var cnt))
        {
            GlobalEvents.CountReceivedEventKey.Broadcast(cnt);
        }
    }

    /// <summary>
    /// Enqueue a units-removed message to be sent by the background sender.
    /// </summary>
    public void EnqueueUnitsRemoved(CountDto dto) => _removals.Enqueue(dto);

    /// <summary>
    /// Starts the connection if not already started.
    /// </summary>
    public Task EnsureStartedAsync(CancellationToken ct = default) => _client.EnsureStartedAsync(ct);

    /// <summary>
    /// Stops the SignalR connection and background sender.
    /// </summary>
    public Task StopAsync(CancellationToken ct = default) => _client.StopAsync(ct);
}