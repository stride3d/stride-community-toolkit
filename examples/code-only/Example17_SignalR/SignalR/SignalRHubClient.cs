using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Example17_SignalR.SignalR;

/// <summary>
/// Reusable SignalR hub client that encapsulates connection lifecycle, reconnection,
/// buffered receivers, and background queued senders.
/// Keeps SignalR concerns isolated from engine/game threading concerns.
/// </summary>
public sealed class SignalRHubClient : IAsyncDisposable
{
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private readonly Random _random = new();
    private volatile bool _reconnecting;

    private readonly List<IDisposable> _subscriptions = [];
    private readonly List<IStoppable> _sendQueues = [];
    private readonly ILogger<SignalRHubClient>? _logger;

    /// <summary>
    /// Active SignalR hub connection.
    /// </summary>
    public HubConnection Connection { get; }

    /// <summary>
    /// Initializes a new <see cref="SignalRHubClient"/> with explicit URL.
    /// </summary>
    /// <param name="hubUrl">Absolute hub URL.</param>
    /// <param name="configureBuilder">Optional builder customization callback.</param>
    /// <param name="logger">Optional logger.</param>
    public SignalRHubClient(string hubUrl, Action<IHubConnectionBuilder>? configureBuilder = null, ILogger<SignalRHubClient>? logger = null)
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
        if (string.IsNullOrWhiteSpace(options.HubUrl)) throw new ArgumentException("HubUrl must be provided", nameof(options));

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

        var minBackoff = options.ReconnectBackoffMin ?? TimeSpan.FromMilliseconds(500);
        var maxBackoff = options.ReconnectBackoffMax ?? TimeSpan.FromMilliseconds(2000);
        if (maxBackoff < minBackoff)
            (minBackoff, maxBackoff) = (maxBackoff, minBackoff);

        Connection.Closed += async (error) =>
        {
            if (_reconnecting) return;

            _reconnecting = true;

            try
            {
                var delayMs = _random.Next((int)minBackoff.TotalMilliseconds, (int)maxBackoff.TotalMilliseconds + 1);

                _logger?.LogWarning(error, "SignalR connection closed. Attempting reconnect in {Delay} ms...", delayMs);

                await Task.Delay(delayMs).ConfigureAwait(false);

                await EnsureStartedAsync().ConfigureAwait(false);

                _logger?.LogInformation("SignalR reconnected. State={State}, ConnectionId={ConnectionId}", Connection.State, Connection.ConnectionId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "SignalR reconnect attempt failed.");
                // swallow – further reconnect attempts will happen on future Closed events
            }
            finally
            {
                _reconnecting = false;
            }
        };
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

        if (Connection.State == HubConnectionState.Connected)
        {
            return;
        }

        await _connectionLock.WaitAsync(ct).ConfigureAwait(false);

        try
        {
            if (Connection.State == HubConnectionState.Disconnected)
            {
                _logger?.LogInformation("Starting SignalR connection to {Url}...", Connection?.GetType() != null ? Connection?.ToString() : "<unknown>");

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

        await Connection.DisposeAsync();
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
    /// Registers a buffered receiver for the given hub method that enqueues values for later draining on caller's thread.
    /// </summary>
    /// <typeparam name="T">Payload type.</typeparam>
    /// <param name="methodName">Hub method name.</param>
    /// <returns>Buffered subscription that exposes TryDequeue.</returns>
    public BufferedSubscription<T> RegisterBuffered<T>(string methodName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(methodName);

        var queue = new ConcurrentQueue<T>();
        var sub = Connection.On<T>(methodName, (payload) =>
        {
            if (payload is null) return;

            queue.Enqueue(payload);
        });

        _subscriptions.Add(sub);
        _logger?.LogDebug("Registered buffered handler for method {Method}", methodName);

        return new BufferedSubscription<T>(queue);
    }

    /// <summary>
    /// Creates a background queued sender for the specified hub method name.
    /// Call <see cref="OutgoingQueue{T}.Enqueue"/> to schedule items for sending.
    /// </summary>
    /// <typeparam name="T">Payload type.</typeparam>
    /// <param name="methodName">Hub method to send to.</param>
    /// <returns>Outgoing queue instance.</returns>
    public OutgoingQueue<T> CreateOutgoingQueue<T>(string methodName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(methodName);

        var q = new OutgoingQueue<T>(this, methodName);

        _sendQueues.Add(q);

        _logger?.LogDebug("Created outgoing queue for method {Method}", methodName);

        return q;
    }
}