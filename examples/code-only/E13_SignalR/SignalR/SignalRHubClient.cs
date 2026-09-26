using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace E13_SignalR.SignalR;

/// <summary>
/// Reusable SignalR hub client that encapsulates connection lifecycle, reconnection, receivers and
/// the background sender. Keeps SignalR concerns isolated from engine/game threading concerns:
/// handlers run on SignalR's threads, so they should only enqueue, and the game drains on its own.
/// </summary>
public sealed class SignalRHubClient : IAsyncDisposable
{
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private readonly Random _random = new();

    /// <summary>
    /// Cancelled by <see cref="StopAsync"/> so a connect attempt already sleeping on its backoff
    /// wakes immediately instead of holding shutdown open for the rest of the delay.
    /// </summary>
    private readonly CancellationTokenSource _shutdownCts = new();

    /// <summary>
    /// Set before the connection is stopped, so the connect loop can tell an intentional stop from a
    /// dropped connection. Without it, closing the game reconnects instead of shutting down.
    /// </summary>
    private volatile bool _stopRequested;

    /// <summary>The background loop started by <see cref="BeginConnect"/>, awaited on shutdown.</summary>
    private Task? _connectLoop;

    private readonly TimeSpan _minBackoff;
    private readonly TimeSpan _maxBackoff;

    private readonly List<IDisposable> _subscriptions = [];
    private readonly List<IStoppable> _sendQueues = [];
    private readonly ILogger<SignalRHubClient>? _logger;
    private readonly Uri _hubUrl;

    /// <summary>
    /// Active SignalR hub connection.
    /// </summary>
    public HubConnection Connection { get; }

    /// <summary>
    /// Whether the hub is currently reachable. The hub is an optional feature: everything the game
    /// does works without it, so callers use this to show status rather than to decide whether to run.
    /// </summary>
    public bool IsConnected => Connection.State == HubConnectionState.Connected;

    /// <summary>
    /// Initializes a new <see cref="SignalRHubClient"/> with explicit URL.
    /// </summary>
    /// <param name="hubUrl">Absolute hub URL.</param>
    /// <param name="configureBuilder">Optional builder customization callback.</param>
    /// <param name="logger">Optional logger.</param>
    public SignalRHubClient(Uri hubUrl, Action<IHubConnectionBuilder>? configureBuilder = null, ILogger<SignalRHubClient>? logger = null)
        : this(new SignalRClientOptions { HubUrl = hubUrl }, logger, configureBuilder)
    {
    }

    /// <summary>
    /// Initializes a new <see cref="SignalRHubClient"/> using <see cref="SignalRClientOptions"/>.
    /// </summary>
    /// <param name="options">Client options (URL and connection settings).</param>
    /// <param name="logger">Optional logger.</param>
    /// <param name="configureBuilder">Optional builder customization callback.</param>
    public SignalRHubClient(SignalRClientOptions options, ILogger<SignalRHubClient>? logger = null, Action<IHubConnectionBuilder>? configureBuilder = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.HubUrl is null) throw new ArgumentException("HubUrl must be provided", nameof(options));

        _hubUrl = options.HubUrl;

        _logger = logger;

        IHubConnectionBuilder builder = new HubConnectionBuilder().WithUrl(options.HubUrl);
        configureBuilder?.Invoke(builder);
        Connection = builder.Build();

        // Apply optional timeouts, if supplied
        if (options.ServerTimeout.HasValue)
            Connection.ServerTimeout = options.ServerTimeout.Value;
        if (options.KeepAliveInterval.HasValue)
            Connection.KeepAliveInterval = options.KeepAliveInterval.Value;
        if (options.HandshakeTimeout.HasValue)
            Connection.HandshakeTimeout = options.HandshakeTimeout.Value;

        _minBackoff = options.ReconnectBackoffMin ?? TimeSpan.FromMilliseconds(500);
        _maxBackoff = options.ReconnectBackoffMax ?? TimeSpan.FromMilliseconds(2000);
        if (_maxBackoff < _minBackoff)
            (_minBackoff, _maxBackoff) = (_maxBackoff, _minBackoff);

        // Closed only reports. Reconnecting from here was the old design, and it had two faults: it
        // could not tell an intentional StopAsync from a dropped connection, and it never fired at
        // all when the very first connect failed - so a game started before the hub stayed offline
        // for its whole run. The connect loop below handles both cases with one piece of code.
        Connection.Closed += error =>
        {
            if (!_stopRequested)
            {
                _logger?.LogWarning(error, "SignalR connection lost. The game continues; reconnecting in the background.");
            }

            return Task.CompletedTask;
        };
    }

    /// <summary>
    /// Starts connecting in the background and keeps trying until the hub answers or the client is
    /// stopped. Returns immediately and never throws.
    /// </summary>
    /// <remarks>
    /// This is the whole "SignalR is optional" contract: the caller does not await a connection, is
    /// not told to handle a failure, and cannot be broken by a hub that is missing, slow or restarted
    /// halfway through. A hub that appears later is picked up on the next attempt.
    /// </remarks>
    public void BeginConnect()
    {
        if (_stopRequested || _connectLoop is not null)
        {
            return;
        }

        _connectLoop = Task.Run(() => ConnectLoopAsync(_shutdownCts.Token));
    }

    /// <summary>
    /// Keeps the connection up for the client's lifetime: connects when disconnected, waits a
    /// randomised backoff, and looks again. One loop covers the first connect and every reconnect.
    /// </summary>
    private async Task ConnectLoopAsync(CancellationToken ct)
    {
        // The hub being down is normal here, and a message per attempt would bury the console. Say
        // it once, then drop to debug until something changes.
        var announced = false;

        while (!ct.IsCancellationRequested && !_stopRequested)
        {
            if (Connection.State == HubConnectionState.Disconnected)
            {
                try
                {
                    await EnsureStartedAsync(ct).ConfigureAwait(false);

                    announced = false;
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    if (!announced)
                    {
                        _logger?.LogWarning("Hub at {Url} is not answering ({Reason}). The game runs without it and will keep trying.", _hubUrl, ex.Message);

                        announced = true;
                    }
                    else
                    {
                        _logger?.LogDebug(ex, "SignalR connect attempt failed.");
                    }
                }
            }

            try
            {
                var delayMs = _random.Next((int)_minBackoff.TotalMilliseconds, (int)_maxBackoff.TotalMilliseconds + 1);

                await Task.Delay(delayMs, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    /// <summary>
    /// Starts the connection if currently disconnected.
    /// </summary>
    public async Task EnsureStartedAsync(CancellationToken ct = default)
    {
        if (Connection is null)
        {
            throw new InvalidOperationException("Connection is not initialized.");
        }

        // Stopping is terminal for this client - StopAsync is followed by DisposeAsync - so a start
        // arriving afterwards is a race, not a request. Returning is right; throwing would only move
        // the exception from StartAsync to here.
        if (_stopRequested)
        {
            return;
        }

        if (Connection.State == HubConnectionState.Connected)
        {
            return;
        }

        await _connectionLock.WaitAsync(ct).ConfigureAwait(false);

        try
        {
            if (Connection.State == HubConnectionState.Disconnected)
            {
                // Debug, not information: the connect loop retries on a backoff, so an unreachable
                // hub would otherwise print this line every second or two for the whole session.
                _logger?.LogDebug("Starting SignalR connection to {Url}...", _hubUrl);

                await Connection!.StartAsync(ct).ConfigureAwait(false);

                _logger?.LogInformation("SignalR connected. State={State}, ConnectionId={ConnectionId}", Connection.State, Connection.ConnectionId);
            }
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    /// <summary>
    /// Stops the connection and any background send queues.
    /// </summary>
    public async Task StopAsync(CancellationToken ct = default)
    {
        // All three happen before the lock is taken. The connect loop takes the same lock inside
        // EnsureStartedAsync, so it has to be finished before this method can hold it - and a flag
        // set after the lock would be set too late for the loop to see.
        _stopRequested = true;

        await _shutdownCts.CancelAsync().ConfigureAwait(false);

        if (_connectLoop is not null)
        {
            try
            {
                await _connectLoop.ConfigureAwait(false);
            }
            catch
            {
                // The loop swallows its own failures; this only unwraps cancellation.
            }

            _connectLoop = null;
        }

        await _connectionLock.WaitAsync(ct).ConfigureAwait(false);

        try
        {
            foreach (var q in _sendQueues)
            {
                q.Stop();
            }

            if (Connection.State != HubConnectionState.Disconnected)
            {
                _logger?.LogInformation("Stopping SignalR connection...");

                await Connection.StopAsync(ct).ConfigureAwait(false);

                _logger?.LogInformation("SignalR connection stopped.");
            }
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    /// <summary>
    /// Disposes the client, stops connection and subscriptions.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);

        foreach (var s in _subscriptions)
        {
            s.Dispose();
        }

        _subscriptions.Clear();

        await Connection.DisposeAsync().ConfigureAwait(false);

        _shutdownCts.Dispose();
        _connectionLock.Dispose();
    }

    /// <summary>
    /// Registers a simple pass-through receiver for a hub method.
    /// </summary>
    /// <typeparam name="T">Payload type.</typeparam>
    /// <param name="methodName">Hub method name.</param>
    /// <param name="handler">Callback to invoke with received payload.</param>
    /// <returns>Disposable subscription.</returns>
    public IDisposable RegisterHandler<T>(string methodName, Action<T> handler)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(methodName);
        ArgumentNullException.ThrowIfNull(handler);

        var sub = Connection.On<T>(methodName, (payload) =>
        {
            if (payload is null) return;

            handler(payload);
        });

        _subscriptions.Add(sub);
        _logger?.LogDebug("Registered handler for method {Method}", methodName);

        return sub;
    }

    /// <summary>
    /// Registers a receiver for a hub method that carries no payload.
    /// </summary>
    /// <param name="methodName">Hub method name.</param>
    /// <param name="handler">Callback to invoke when the method is received.</param>
    /// <returns>Disposable subscription.</returns>
    public IDisposable RegisterHandler(string methodName, Action handler)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(methodName);
        ArgumentNullException.ThrowIfNull(handler);

        var sub = Connection.On(methodName, handler);

        _subscriptions.Add(sub);
        _logger?.LogDebug("Registered handler for method {Method}", methodName);

        return sub;
    }

    /// <summary>
    /// Creates the background sender for this connection. Call <see cref="OutgoingQueue.Enqueue"/> to
    /// schedule a hub call; it goes out in order with everything queued before it.
    /// </summary>
    /// <returns>Outgoing queue instance.</returns>
    public OutgoingQueue CreateOutgoingQueue()
    {
        var q = new OutgoingQueue(this);

        _sendQueues.Add(q);

        _logger?.LogDebug("Created outgoing queue");

        return q;
    }
}