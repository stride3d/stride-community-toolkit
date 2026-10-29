using Microsoft.AspNetCore.SignalR.Client;
using System.Collections.Concurrent;

namespace E13_SignalR.SignalR;

/// <summary>
/// A background sender: game code enqueues from the update loop and returns at once, a single loop
/// sends in order. One queue for every hub method, rather than one per method, because order across
/// methods matters - a landing must never reach the page before the release it belongs to.
/// </summary>
public sealed class OutgoingQueue : IStoppable, IDisposable
{
    private readonly SignalRHubClient _owner;
    private readonly ConcurrentQueue<(string Method, object Payload)> _queue = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _loopTask;
    private int _stopped;

    internal OutgoingQueue(SignalRHubClient owner)
    {
        _owner = owner;
        _loopTask = Task.Run(() => LoopAsync(_cts.Token));
    }

    /// <summary>Queues one call of <paramref name="method"/> on the hub with <paramref name="payload"/> as its argument.</summary>
    public void Enqueue(string method, object payload) => _queue.Enqueue((method, payload));

    /// <summary>
    /// Same as <see cref="Stop"/>: cancels the loop and disposes the <see cref="CancellationTokenSource"/>
    /// this type creates. Safe to call twice.
    /// </summary>
    public void Dispose() => Stop();

    /// <inheritdoc />
    public void Stop()
    {
        // Once only: the client stops every queue it handed out, and the owner may dispose it too
        if (Interlocked.Exchange(ref _stopped, 1) == 1) return;

        try
        {
            _cts.Cancel();

            // Bounded: a send in flight against a hub that has just gone away is not worth waiting for
            _loopTask.Wait(250);
        }
        catch (AggregateException)
        {
            // Not expected: the loop catches its own cancellation and failures
        }
        finally
        {
            _cts.Dispose();
        }
    }

    private async Task LoopAsync(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                if (!_queue.TryDequeue(out var next))
                {
                    await Task.Delay(1, token).ConfigureAwait(false);

                    continue;
                }

                try
                {
                    await _owner.Connection.SendAsync(next.Method, next.Payload, token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception)
                {
                    // The hub is optional: a report that cannot be delivered is dropped, not retried.
                    // The next census puts the page right within a second of the link returning.
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal during shutdown
        }
    }
}