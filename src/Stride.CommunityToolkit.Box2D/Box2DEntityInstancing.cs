using Stride.CommunityToolkit.Rendering.Instancing;
using Stride.Engine;
using System.Runtime.InteropServices;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Worlds;

namespace Stride.CommunityToolkit.Box2D;

/// <summary>
/// An <see cref="EntityInstancing"/> for Box2D bodies that stops working entirely once the
/// bodies fall asleep, mirroring <c>BepuEntityInstancing</c> from Stride.CommunityToolkit.Bepu.
/// </summary>
/// <remarks>
/// <para>
/// Box2D puts bodies to sleep when they come to rest, and a sleeping body's transform cannot change.
/// That makes re-reading every transform, re-inverting every matrix and re-computing the bounding box
/// pure waste, which is what the engine does forever once a pile settles. This class checks the
/// bodies instead and reuses the previous frame's results while they are all asleep.
/// </para>
/// <para>
/// The check is a scan over the registered bodies, so it costs a little while things are moving - it
/// gives up at the first awake body - and pays for itself many times over when they are not. Instances
/// registered without a <see cref="Box2DBodyComponent"/> disable skipping altogether, since nothing
/// indicates when they move; a component whose body id is invalid (not yet created, or destroyed)
/// counts as asleep, because its transform cannot change either.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// game.AddInstancingSupport();
///
/// var instancing = new Box2DEntityInstancing();
/// var master = new Entity("Master") { new ModelComponent(model), new InstancingComponent { Type = instancing } };
/// master.Scene = scene;
///
/// // Instances carry a Box2DBodyComponent but no ModelComponent - the master draws them
/// foreach (var body in bodies) instancing.AddInstance(body);
/// </code>
/// </example>
public class Box2DEntityInstancing : EntityInstancing
{
    private readonly List<Box2DBodyComponent?> _bodies = [];

    /// <summary>
    /// Gets the number of registered instances that have no <see cref="Box2DBodyComponent"/>, and so
    /// prevent the sleep skip.
    /// </summary>
    public int InstancesWithoutBody { get; private set; }

    /// <summary>
    /// Determines whether every registered body is asleep, in which case no transform can have changed.
    /// </summary>
    /// <returns><see langword="true"/> when the previous frame's gather is still valid.</returns>
    protected override bool CanSkipUpdate()
    {
        if (InstancesWithoutBody > 0) return false;

        foreach (var body in CollectionsMarshal.AsSpan(_bodies))
        {
            // Null-forgiving: InstancesWithoutBody being zero means every entry is non-null.
            // An invalid id means the body is gone or not created yet - its transform is frozen too.
            var bodyId = body!.BodyId;

            if (b2Body_IsValid(bodyId) && b2Body_IsAwake(bodyId)) return false;
        }

        return true;
    }

    /// <inheritdoc />
    protected override void OnInstanceAdded(Entity entity)
    {
        // Resolved once here rather than per frame
        var body = entity.Get<Box2DBodyComponent>();

        if (body is null) InstancesWithoutBody++;

        _bodies.Add(body);
    }

    /// <inheritdoc />
    protected override void OnInstanceRemoved(int index, int lastIndex)
    {
        if (_bodies[index] is null) InstancesWithoutBody--;

        // Mirror the base class's swap-remove exactly, or the bodies stop matching the transforms
        if (index != lastIndex) _bodies[index] = _bodies[lastIndex];

        _bodies.RemoveAt(lastIndex);
    }

    /// <inheritdoc />
    protected override void OnInstancesCleared()
    {
        _bodies.Clear();
        InstancesWithoutBody = 0;
    }
}