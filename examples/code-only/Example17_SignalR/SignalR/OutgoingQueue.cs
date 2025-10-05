using Microsoft.AspNetCore.SignalR.Client;
using System.Collections.Concurrent;

namespace Example17_SignalR.SignalR;

public class OutgoingQueue<T> : IStoppable
{
    private readonly SignalRHubClient _owner;
    private readonly string _methodName;
    private readonly ConcurrentQueue<T> _queue = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _loopTask;

    internal OutgoingQueue(SignalRHubClient owner, string methodName)
    {
        _owner = owner;
        _methodName = methodName;
        _loopTask = Task.Run(() => LoopAsync(_cts.Token));
    }

    public void Enqueue(T item) => _queue.Enqueue(item);

    public void Stop()
    {
        try
        {
            _cts.Cancel();
            _loopTask.Wait(250);
        }
        catch { }
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
                if (_queue.TryDequeue(out var next) && next is not null)
                {
                    try
                    {
                        await _owner.Connection.SendAsync(_methodName, next, token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch
                    {
                        // Ignore and continue; consider backoff/retry if needed
                    }
                    await Task.Yield();
                }
                else
                {
                    await Task.Delay(1, token).ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // normal during shutdown
        }
    }
}