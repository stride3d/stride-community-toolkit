namespace Example17_SignalR.SignalR;

/// <summary>
/// Options for configuring <see cref="SignalRHubClient"/>.
/// </summary>
public sealed class SignalRClientOptions
{
    /// <summary>
    /// Absolute hub URL (e.g. https://localhost:44304/screen1).
    /// </summary>
    public string HubUrl { get; set; } = string.Empty;

    /// <summary>
    /// Optional server timeout override.
    /// </summary>
    public TimeSpan? ServerTimeout { get; set; }

    /// <summary>
    /// Optional keep-alive interval override.
    /// </summary>
    public TimeSpan? KeepAliveInterval { get; set; }

    /// <summary>
    /// Optional handshake timeout override.
    /// </summary>
    public TimeSpan? HandshakeTimeout { get; set; }

    /// <summary>
    /// Minimum reconnect backoff delay. Default 500 ms.
    /// </summary>
    public TimeSpan? ReconnectBackoffMin { get; set; }

    /// <summary>
    /// Maximum reconnect backoff delay. Default 2000 ms.
    /// </summary>
    public TimeSpan? ReconnectBackoffMax { get; set; }
}