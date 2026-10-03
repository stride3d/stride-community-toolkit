---
generated: true
---

# C# Getting Started Examples

Your first code-only Stride application in C#: boilerplate, one helper call, and something on screen. Start here.

## Examples Overview

- [Basic3D Scene (Capsule)](capsule-with-rigid-body.md): Create a minimal 3D scene using toolkit helpers, add a skybox, and place a single capsule primitive.
- [Basic3D Scene (Engine Only)](basic-scene-engine-only.md): A ground, a cube and a camera written against Stride alone, with no toolkit package: the graphics compositor, the camera, the lights, the procedural models and the material that the toolkit's SetupBase3D and Create3DPrimitive stand for.
- [Basic3D Scene (Capsule) - File-Based App](file-based-app.md): The same minimal 3D scene as E01_3D_BasicScene, written as a .NET 10 file-based app: a single C# file with no .csproj.
- [Basic2D Scene (Capsule)](basic-2d-scene.md): Create a minimal 2D scene using toolkit helpers and place a single capsule primitive with a flat material.
- [Basic2D Scene (Capsule) - Bullet Physics](basic-2d-scene-bullet.md): The same first 2D scene as E01_2D_BasicScene, running on the legacy Bullet physics engine instead of Bepu.
- [Basic3D Scene (Capsule) - Bullet Physics](capsule-with-rigid-body-bullet.md): The same first scene as E01_3D_BasicScene, running on the legacy Bullet physics engine instead of Bepu.

> [!NOTE]
> Each example references a handful of toolkit packages. The `using` directives at the top of
> every listing name them, and the linked project file on GitHub is authoritative. A few examples
> also need a third-party package - Box2D.NET, Jitter2, Myra or ImGui - which their page calls out.

[!INCLUDE [basic-examples-outro](../../../includes/manual/examples/basic-examples-outro.md)]