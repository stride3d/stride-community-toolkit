using System.Collections.Concurrent;

namespace Example17_SignalR.SignalR;

/// <summary>
/// A simple buffered subscription wrapper exposing a queue drain helper.
/// Lifetime of the underlying SignalR handler is managed by SignalRHubClient.
/// </summary>
/// <typeparam name="T">Payload type.</typeparam>
public readonly struct BufferedSubscription<T>(ConcurrentQueue<T> queue)
{
    /// <summary>
    /// Attempts to dequeue the next buffered item.
    /// </summary>
    public bool TryDequeue(out T item) => queue.TryDequeue(out item!);
}