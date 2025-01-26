using Stride.BepuPhysics;
using Stride.CommunityToolkit.Engine;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace Stride.CommunityToolkit.Bepu;

public static class CameraComponentExtensions
{
    public static HitInfo? Raycast(this CameraComponent camera, Vector2 screenPosition, CollisionMask collisionMask = CollisionMask.Everything)
    {
        var (nearPoint, farPoint) = camera.CalculateRayFromScreenPosition(screenPosition);

        //var delta = (farPoint - nearPoint).XYZ();
        //var maxDistance = delta.Length();
        //var dir = delta / maxDistance; // normalize delta

        var result = camera.Entity.GetSimulation().RayCast(nearPoint, farPoint, 100, out HitInfo hit, collisionMask);

        if (!result) return null;

        return hit;
    }
}
