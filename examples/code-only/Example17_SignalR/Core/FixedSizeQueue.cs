using Example17_SignalR_Shared.Dtos;
using System.Collections.Concurrent;

namespace Example17_SignalR.Core;

/// <summary>
/// Fixed-size FIFO queue for <see cref="MessageDto"/> that discards oldest entries when full.
/// </summary>
public class FixedSizeQueue
{
    /// <summary>
    /// Current number of items in the queue.
    /// </summary>
    public int Count => _queue.Count;

    private readonly ConcurrentQueue<MessageDto> _queue;
    private readonly int _maxSize;

    /// <summary>
    /// Creates a new <see cref="FixedSizeQueue"/>.
    /// </summary>
    /// <param name="maxSize">Maximum number of items to keep.</param>
    public FixedSizeQueue(int maxSize)
    {
        _maxSize = maxSize;

        _queue = new ConcurrentQueue<MessageDto>();
    }

    /// <summary>
    /// Enqueues an item, discarding the oldest one when the queue is full.
    /// </summary>
    public void Enqueue(MessageDto? item)
    {
        if (item == null) return;

        if (_queue.Count == _maxSize && !_queue.IsEmpty)
        {
            _queue.TryDequeue(out _);
        }

        _queue.Enqueue(item);
    }

    /// <summary>
    /// Returns a snapshot as a span.
    /// </summary>
    public ReadOnlySpan<MessageDto> AsSpan() => new(_queue.ToArray());

    /// <summary>
    /// Returns a snapshot as a list.
    /// </summary>
    public List<MessageDto> ToList() => _queue.ToList();
}