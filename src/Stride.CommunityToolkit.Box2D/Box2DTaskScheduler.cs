using Box2D.NET;
using System.Collections.Concurrent;

namespace Stride.CommunityToolkit.Box2D;

/// <summary>
/// The task scheduler behind multithreaded stepping: implements Box2D's enqueue/finish task
/// callbacks over a pool of dedicated threads, each with a fixed worker index.
/// </summary>
/// <remarks>
/// <para>
/// Box2D indexes its per-worker scratch memory by the worker index passed to each task callback, so
/// two concurrently running callbacks must never share an index. Dedicated threads make that
/// invariant structural: every pool thread always reports its own index, no matter how work is
/// batched. The Box2D.NET samples uphold the same invariant by leasing indices from a queue.
/// </para>
/// <para>
/// Box2D v3's solver is deterministic across worker counts: stepping the same scene with 1 or 8
/// workers produces bit-identical results, only faster.
/// </para>
/// </remarks>
internal sealed class Box2DTaskScheduler : IDisposable
{
    /// <summary>One Enqueue call's worth of jobs; Finish blocks on it until the count drains.</summary>
    private sealed class Batch
    {
        private readonly object _gate = new();
        private int _remaining;

        internal Batch(int jobs) => _remaining = jobs;

        internal void CompleteOne()
        {
            if (Interlocked.Decrement(ref _remaining) == 0)
            {
                lock (_gate) Monitor.PulseAll(_gate);
            }
        }

        internal void Wait()
        {
            lock (_gate)
            {
                while (Volatile.Read(ref _remaining) > 0)
                {
                    Monitor.Wait(_gate);
                }
            }
        }
    }

    private readonly struct Job
    {
        internal readonly b2TaskCallback Task;
        internal readonly int Start;
        internal readonly int End;
        internal readonly object TaskContext;
        internal readonly Batch Batch;

        internal Job(b2TaskCallback task, int start, int end, object taskContext, Batch batch)
        {
            Task = task;
            Start = start;
            End = end;
            TaskContext = taskContext;
            Batch = batch;
        }
    }

    private readonly BlockingCollection<Job> _queue = new();
    private readonly Thread[] _threads;

    /// <summary>The number of pool threads, matching the world's worker count.</summary>
    internal int WorkerCount => _threads.Length;

    internal Box2DTaskScheduler(int workerCount)
    {
        _threads = new Thread[workerCount];

        for (int i = 0; i < workerCount; i++)
        {
            int workerIndex = i;

            _threads[i] = new Thread(() =>
            {
                foreach (var job in _queue.GetConsumingEnumerable())
                {
                    job.Task(job.Start, job.End, (uint)workerIndex, job.TaskContext);
                    job.Batch.CompleteOne();
                }
            })
            {
                IsBackground = true,
                Name = $"Box2D worker {i}"
            };

            _threads[i].Start();
        }
    }

    /// <summary>
    /// Box2D's enqueue callback: splits the item range across the pool and returns the handle
    /// <see cref="Finish"/> later waits on.
    /// </summary>
    internal object Enqueue(b2TaskCallback task, int itemCount, int minRange, object taskContext, object userContext)
    {
        var jobs = Math.Clamp(itemCount / Math.Max(1, minRange), 1, _threads.Length);
        var batch = new Batch(jobs);

        // Even a single job goes through the pool: running it inline on the stepping thread would
        // need a worker index, and every index belongs to a pool thread that may be busy with it.
        var chunk = itemCount / jobs;

        for (int i = 0; i < jobs; i++)
        {
            var start = i * chunk;
            var end = i == jobs - 1 ? itemCount : start + chunk;

            _queue.Add(new Job(task, start, end, taskContext, batch));
        }

        return batch;
    }

    /// <summary>
    /// Box2D's finish callback: blocks until every job of the batch returned by <see cref="Enqueue"/> completed.
    /// </summary>
    internal static void Finish(object userTask, object userContext)
    {
        if (userTask is Batch batch) batch.Wait();
    }

    /// <summary>Stops accepting work and joins the pool threads.</summary>
    public void Dispose()
    {
        _queue.CompleteAdding();

        foreach (var thread in _threads) thread.Join();

        _queue.Dispose();
    }
}