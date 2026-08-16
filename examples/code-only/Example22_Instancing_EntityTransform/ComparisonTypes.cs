using Stride.Engine;
using System.Diagnostics;

namespace Example22_Instancing_EntityTransform;

/// <summary>
/// Stride's stock <see cref="InstancingEntityTransform"/> with a stopwatch around
/// <see cref="Update"/>, so the overlay can show its per-frame cost next to the toolkit's.
/// </summary>
/// <remarks>
/// Only the example needs this. <see cref="InstanceComponent"/> auto-registration still works,
/// because this is an <see cref="InstancingEntityTransform"/>.
/// </remarks>
public class TimedInstancingEntityTransform : InstancingEntityTransform
{
    /// <summary>Gets how long the last update took.</summary>
    public double LastUpdateMilliseconds { get; private set; }

    /// <inheritdoc />
    public override void Update()
    {
        var start = Stopwatch.GetTimestamp();

        base.Update();

        LastUpdateMilliseconds = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
    }
}

/// <summary>Which flavour of body a drop produces. See Program.cs.</summary>
public enum ItemKind
{
    /// <summary>Stride's own instancing: InstanceComponent plus an InstancingEntityTransform master.</summary>
    Stock,

    /// <summary>The toolkit's BepuEntityInstancing: skips its update while every body sleeps.</summary>
    Toolkit,

    /// <summary>BepuEntityInstancing wrapped in BufferedEntityInstancing: skips the GPU upload too.</summary>
    ToolkitBuffered,

    /// <summary>No instancing at all: each body keeps its own ModelComponent and draw call.</summary>
    Plain
}
