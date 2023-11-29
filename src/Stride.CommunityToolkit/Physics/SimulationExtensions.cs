using Stride.Engine;
using Stride.Physics;

namespace Stride.CommunityToolkit.Physics;

public static class SimulationExtensions
{
    /// <summary>
    /// A Raycast method based on the example in the fps demo
    /// <para>Make sure you are using the actual rotating Entity otherwise you will waste hours like I did debugging a non issue</para>
    /// </summary>
    public static HitResult Raycast(this Simulation simulation, Entity entityPosition, float length = 1, CollisionFilterGroupFlags collisionFlags = CollisionFilterGroupFlags.AllFilter, EFlags eFlags = EFlags.None)
    {
        var raycastStart = entityPosition.Transform.WorldMatrix.TranslationVector;
        var forward = entityPosition.Transform.WorldMatrix.Forward;
        var raycastEnd = raycastStart + forward * length;

        var result = simulation.Raycast(raycastStart, raycastEnd, filterFlags: collisionFlags, eFlags: eFlags);

        return result;
    }

    /// <summary>
    /// A Raycast method based on the example in the fps demo
    /// </summary>
    public static HitResult Raycast(this Simulation simulation, Entity entityPosition, Vector3 direction, float length = 1, CollisionFilterGroupFlags collisionFlags = CollisionFilterGroupFlags.AllFilter, EFlags eFlags = EFlags.None)
    {
        var raycastStart = entityPosition.Transform.WorldMatrix.TranslationVector;
        var raycastEnd = raycastStart + direction * length;

        var result = simulation.Raycast(raycastStart, raycastEnd, filterFlags: collisionFlags, eFlags: eFlags);

        return result;
    }
}