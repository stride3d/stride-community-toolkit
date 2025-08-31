using Example17_SignalR_Shared.Dtos;
using System.Collections.Concurrent;

namespace Example17_SignalR.Core;

public class FixedSizeQueue
{
    public int Count => _queue.Count;

    private readonly ConcurrentQueue<MessageDto> _queue;
    private readonly int _maxSize;

    public FixedSizeQueue(int maxSize)
    {
        _maxSize = maxSize;

        _queue = new ConcurrentQueue<MessageDto>();
    }

    public void Enqueue(MessageDto? item)
    {
        if (item == null) return;

        if (_queue.Count == _maxSize && !_queue.IsEmpty)
        {
            _queue.TryDequeue(out _);
        }

        _queue.Enqueue(item);
    }

    public ReadOnlySpan<MessageDto> AsSpan() => new(_queue.ToArray());

    public List<MessageDto> ToList() => _queue.ToList();
}