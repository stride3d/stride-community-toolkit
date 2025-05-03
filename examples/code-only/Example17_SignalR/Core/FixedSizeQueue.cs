using Example17_SignalR_Shared.Dtos;

namespace Example17_SignalR.Core;

public class FixedSizeQueue
{
    public int Count => _queue.Count;

    private readonly Queue<MessageDto> _queue;
    private readonly int _maxSize;

    public FixedSizeQueue(int maxSize)
    {
        _maxSize = maxSize;
        _queue = new Queue<MessageDto>(maxSize);
    }
    public void Enqueue(MessageDto item)
    {
        if (_queue.Count == _maxSize)
        {
            _queue.Dequeue();
        }
        _queue.Enqueue(item);
    }

    public ReadOnlySpan<MessageDto> AsSpan() => new(_queue.ToArray());

    public List<MessageDto> ToList() => _queue.ToList();
}