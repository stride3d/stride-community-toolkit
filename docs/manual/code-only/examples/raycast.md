---
generated: true
slug: raycast
---

# Raycast

Two raycasts with two jobs. Click the ground and the camera's ray finds the point, the first thing
under the mouse; the sphere is kicked towards it. A second ray is cast from the sphere through that
point and on, with every hit listed as the query reports them, which is what the sphere would meet if it flew on,
and the line shows that ray. Click the sphere and it stops dead.

The `Program.cs` file shows how to:

- Turning a screen position into a world ray from the camera, and the first thing it hits
- A second ray from a point in the world through another, as a RaySegment
- Listing every hit along a ray with RayCastPenetrating, unsorted, not only the first
- Applying an impulse towards a point
- Stopping a body by zeroing its velocity
- Drawing the impulse direction as a line from the sphere to the hit point
- Using helpers: SceneSetup3D, UseSkybox, CreatePrimitive, AddGroundGizmo, AddOrientationGizmo

![Raycast](media/raycast.webp)

View on [GitHub](https://github.com/stride3d/stride-community-toolkit/tree/main/examples/code-only/E05_3D_Raycast).

[!code-csharp[](../../../../examples/code-only/E05_3D_Raycast/Program.cs?start=1&end=211)]