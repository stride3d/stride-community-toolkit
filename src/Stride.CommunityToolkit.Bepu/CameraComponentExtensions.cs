using Stride.BepuPhysics;
using Stride.CommunityToolkit.Engine;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace Stride.CommunityToolkit.Bepu;

public static class CameraComponentExtensions
{
    public static HitInfo? RaycastMouse(this CameraComponent camera, ScriptComponent component, float maxDistance = 100, CollisionMask collisionMask = CollisionMask.Everything)
        => camera.Raycast(component.Input.MousePosition, maxDistance, collisionMask);

    public static HitInfo? Raycast(this CameraComponent camera, Vector2 screenPosition, float maxDistance = 100, CollisionMask collisionMask = CollisionMask.Everything)
    {
        var (nearPoint, farPoint) = camera.CalculateRayFromScreenPosition(screenPosition);

        var result = camera.Entity.GetSimulation().RayCast(nearPoint, farPoint, maxDistance, out HitInfo hit, collisionMask);

        if (!result) return null;

        return hit;
    }
}