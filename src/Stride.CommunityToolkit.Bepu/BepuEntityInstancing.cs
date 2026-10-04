using Stride.BepuPhysics;
using Stride.CommunityToolkit.Rendering.Instancing;
using Stride.Engine;
using System.Runtime.InteropServices;

namespace Stride.CommunityToolkit.Bepu;

/// <summary>
/// An <see cref="EntityInstancing"/> for Bepu physics bodies that stops working entirely once the
/// bodies fall asleep.
/// </summary>
/// <remarks>
/// <para>
/// A sleeping body's transform cannot change. While every registered body is asleep, this class
/// skips the per-frame transform read, matrix inversion and bounding-box update, and reuses the
/// previous frame's results.
/// </para>
/// <para>
/// Pair it with <see cref="BufferedEntityInstancing"/> to stop the redundant GPU upload as well.
/// </para>
/// <para>
/// The check scans the registered bodies and stops at the first awake one. Instances
/// registered without a <see cref="BodyComponent"/> (a static or kinematic entity moved by script, say)
/// disable skipping altogether, since nothing indicates when they move.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// game.AddInstancingSupport();
///
/// var instancing = new BepuEntityInstancing();
/// var master = new Entity("Master") { new ModelComponent(model), new InstancingComponent { Type = instancing } };
/// master.Scene = scene;
///
/// // Instances carry a body but no ModelComponent - the master draws them
/// foreach (var cube in cubes) instancing.AddInstance(cube);
/// </code>
/// </example>
public class BepuEntityInstancing : EntityInstancing
{
    private readonly List<BodyComponent?> _bodies = [];

    private int _instancesWithoutBody;

    /// <summary>
    /// Gets the number of registered instances that have no <see cref="BodyComponent"/>, and so
    /// prevent the sleep skip.
    /// </summary>
    public int InstancesWithoutBody => _instancesWithoutBody;

    /// <summary>
    /// Determines whether every registered body is asleep, in which case no transform can have changed.
    /// </summary>
    /// <returns><see langword="true"/> when the previous frame's gather is still valid.</returns>
    protected override bool CanSkipUpdate()
    {
        if (_instancesWithoutBody > 0) return false;

        foreach (var body in CollectionsMarshal.AsSpan(_bodies))
        {
            // Null-forgiving: _instancesWithoutBody being zero means every entry is non-null
            if (body!.Awake) return false;
        }

        return true;
    }

    /// <inheritdoc />
    protected override void OnInstanceAdded(Entity entity)
    {
        // Resolved once here rather than per frame. Body2DComponent derives from BodyComponent, so
        // 2D bodies are covered too
        var body = entity.Get<BodyComponent>();

        if (body is null) _instancesWithoutBody++;

        _bodies.Add(body);
    }

    /// <inheritdoc />
    protected override void OnInstanceRemoved(int index, int lastIndex)
    {
        if (_bodies[index] is null) _instancesWithoutBody--;

        // Mirror the base class's swap-remove exactly, or the bodies stop matching the transforms
        if (index != lastIndex)
        {
            _bodies[index] = _bodies[lastIndex];
        }

        _bodies.RemoveAt(lastIndex);
    }

    /// <inheritdoc />
    protected override void OnInstancesCleared()
    {
        _bodies.Clear();
        _instancesWithoutBody = 0;
    }
}