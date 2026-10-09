---
generated: true
slug: falling-shapes-3d
---

# Basic 3D Scene (Falling Shapes)

The next step after the basic 3D scene: thirty shapes of five kinds and five colours, dropped in
a column that topples into a pile. It adds a loop, a list of shapes with a colour each, and a
material per colour. Bepu physics does the rest. The 2D twin, E01_2D_FallingShapes, drops the
same shapes in the same colours.

The `Program.cs` file shows how to:

- Creating many 3D primitives in a loop with Create3DPrimitive
- A list of shape types with a colour each
- Applying a material with CreateMaterial
- Positioning entities so that they fall into a pile
- Using helpers: SetupBase3DScene, AddSkybox

![Basic 3D Scene (Falling Shapes)](media/falling-shapes-3d.webp)

View on [GitHub](https://github.com/stride3d/stride-community-toolkit/tree/main/examples/code-only/E01_3D_FallingShapes).

[!code-csharp[](../../../../examples/code-only/E01_3D_FallingShapes/Program.cs?start=1&end=43)]