---
generated: true
slug: basic-2d-scene
---

# Basic 2D Scene (Capsule)

Create a minimal 2D scene using toolkit helpers and place a single capsule primitive with a flat material.
Demonstrates primitive creation, basic positioning, and attaching the entity to the scene.

The `Program.cs` file shows how to:

- Creating a 2D primitive with Create2DPrimitive
- Applying a flat material with CreateFlatMaterial
- Positioning an entity with Transform.Position
- Adding entities to a Scene (rootScene)
- Using helpers: SetupBase2DScene

![Basic 2D Scene (Capsule)](media/basic-2d-scene.webp)

View on [GitHub](https://github.com/stride3d/stride-community-toolkit/tree/main/examples/code-only/E01_2D_BasicScene).

[!code-csharp[](../../../../examples/code-only/E01_2D_BasicScene/Program.cs?start=1&end=21)]