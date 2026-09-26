using E13_SignalR_Shared;
using Stride.Core.Mathematics;

namespace E13_SignalR.Station;

/// <summary>
/// The live cargo: releases containers from the hatch, notices when they land or fall off, clears
/// and shakes them, and counts them for the census. Everything here runs on the game thread; the
/// events it raises are how the rest of the game - the console log and the hub link - hear about it.
/// </summary>
public sealed class Deck(ContainerFactory factory)
{
    /// <summary>Where cargo appears. High enough to tumble on the way down.</summary>
    public const float HatchHeight = 12f;

    /// <summary>Below this a container has clearly left the deck for good.</summary>
    private const float LostBelow = -12f;

    /// <summary>A container slower than this, once it has had time to fall, has landed - or gone to sleep, which is the same thing.</summary>
    private const float SettledSpeed = 0.2f;

    /// <summary>A container is not "landed" the frame it appears; gravity has to have had a say first.</summary>
    private const float MinAirTime = 0.4f;

    /// <summary>A batch is staggered so the containers do not spawn inside each other.</summary>
    private const float BatchInterval = 0.15f;

    private readonly List<Container> _containers = [];
    private readonly Queue<(ReleaseRequest Request, CommandOrigin Origin)> _pending = new();
    private readonly Random _random = new();

    private int _nextId = 1;
    private int _released;
    private int _lost;
    private float _time;
    private float _untilNextPending;

    public event Action<ContainerEvent>? Released;

    public event Action<ContainerEvent>? Landed;

    public event Action<ContainerEvent>? Lost;

    public event Action<int>? Cleared;

    public int OnDeck => _containers.Count;

    public int PendingCount => _pending.Count;

    /// <summary>The live container with this id, or <see langword="null"/> once it has landed for good or been lost.</summary>
    public Container? Find(int id) => _containers.Find(container => container.Id == id);

    /// <summary>Drops one container now. Unspecified size or paint is chosen at random.</summary>
    public Container Release(ReleaseRequest request, CommandOrigin origin)
    {
        var size = request.Size ?? _random.GetItems(Enum.GetValues<ContainerSize>(), 1)[0];
        var paint = request.Paint ?? _random.GetItems(Paints.All.AsSpan(), 1)[0];

        // A little scatter under the hatch and a random tilt, so no two drops are the same and stacks
        // form by accident rather than by alignment
        var position = new Vector3(Jitter(2f), HatchHeight, Jitter(2f));
        var rotation = Quaternion.RotationYawPitchRoll(Jitter(MathF.PI), Jitter(0.35f), Jitter(0.35f));

        var entity = factory.Create(_nextId, size, paint, position, rotation);

        var container = new Container
        {
            Id = _nextId++,
            Size = size,
            Paint = paint,
            Origin = origin,
            Entity = entity,
            Body = entity.Get<Stride.BepuPhysics.BodyComponent>(),
            ReleasedAt = _time,
        };

        _containers.Add(container);
        _released++;

        Released?.Invoke(container.ToEvent());

        return container;
    }

    /// <summary>Queues a run of random containers; <see cref="Update"/> drops them a few frames apart.</summary>
    public void ReleaseBatch(int count, CommandOrigin origin)
    {
        for (var i = 0; i < count; i++)
        {
            _pending.Enqueue((new ReleaseRequest(), origin));
        }
    }

    /// <summary>Removes every container, pending ones included, and reports how many were on the deck.</summary>
    public int Clear()
    {
        var removed = _containers.Count;

        foreach (var container in _containers)
        {
            container.Entity.Scene = null;
        }

        _containers.Clear();
        _pending.Clear();

        Cleared?.Invoke(removed);

        return removed;
    }

    /// <summary>One upward-biased impulse on every container, scaled by mass so they all jump alike.</summary>
    public void Shake()
    {
        foreach (var container in _containers)
        {
            var direction = Vector3.Normalize(new Vector3(Jitter(1f), 0.6f + _random.NextSingle() * 0.6f, Jitter(1f)));

            // A body that has settled is asleep, and an impulse on a sleeping body is ignored
            container.Body.Awake = true;
            container.Body.ApplyLinearImpulse(direction * container.Mass * 6f);
        }
    }

    /// <summary>Drops pending batch containers and checks every container for landing or loss.</summary>
    public void Update(float deltaSeconds)
    {
        _time += deltaSeconds;

        _untilNextPending -= deltaSeconds;

        if (_pending.Count > 0 && _untilNextPending <= 0f)
        {
            var (request, origin) = _pending.Dequeue();

            Release(request, origin);

            _untilNextPending = BatchInterval;
        }

        // Backwards, because a lost container is removed as it is found
        for (var i = _containers.Count - 1; i >= 0; i--)
        {
            var container = _containers[i];
            var position = container.Entity.Transform.Position;

            if (position.Y < LostBelow)
            {
                container.Entity.Scene = null;
                _containers.RemoveAt(i);
                _lost++;

                Lost?.Invoke(container.ToEvent(new Point3(position.X, position.Y, position.Z)));

                continue;
            }

            var airTime = _time - container.ReleasedAt;

            if (!container.Landed && airTime >= MinAirTime && container.Body.LinearVelocity.LengthSquared() < SettledSpeed * SettledSpeed)
            {
                container.Landed = true;

                Landed?.Invoke(container.ToEvent(new Point3(position.X, position.Y, position.Z), airTime));
            }
        }
    }

    /// <summary>The census: what is on the deck right now, and the running totals.</summary>
    public DeckSnapshot Snapshot(string scheme, float uptimeSeconds)
    {
        var bySize = new int[Enum.GetValues<ContainerSize>().Length];
        var byPaint = new int[Paints.All.Length];
        var totalMass = 0f;

        foreach (var container in _containers)
        {
            bySize[(int)container.Size]++;
            byPaint[(int)container.Paint]++;
            totalMass += container.Mass;
        }

        return new DeckSnapshot(_containers.Count, _released, _lost, totalMass, scheme, uptimeSeconds, bySize, byPaint);
    }

    private float Jitter(float amplitude) => (_random.NextSingle() * 2f - 1f) * amplitude;
}