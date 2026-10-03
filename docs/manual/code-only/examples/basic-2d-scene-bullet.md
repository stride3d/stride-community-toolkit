---
generated: true
slug: basic-2d-scene-bullet
---

# Basic2D Scene (Capsule) - Bullet Physics

The same first 2D scene as E01_2D_BasicScene, running on the legacy Bullet physics engine instead
of Bepu. The scene code is character-for-character identical; the only difference is which toolkit
package is referenced and which namespace is opened. That is the point of the example - physics is
swapped at the project level, not by rewriting the scene.

The `Program.cs` file shows how to:

- Running the base 2D scene on the legacy Bullet physics engine
- Switching engine by namespace: Stride.CommunityToolkit.Bullet in place of .Bepu
- Why the scene code needs no change when the physics engine does
- Using helpers: SetupBase2DScene, Create2DPrimitive, CreateFlatMaterial

![Basic2D Scene (Capsule) - Bullet Physics](media/basic-2d-scene-bullet.webp)

View on [GitHub](https://github.com/stride3d/stride-community-toolkit/tree/main/examples/code-only/E01_2D_BasicScene_Bullet).

[!code-csharp[](../../../../examples/code-only/E01_2D_BasicScene_Bullet/Program.cs?start=1&end=21)]