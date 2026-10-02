---
generated: true
slug: instancing
---

# GPU Instancing

Render two walls of the same cube built two different ways, side by side. The right wall uses one
entity per cube and costs one draw call each; the left wall uses a single entity with an
InstancingComponent and an array of world matrices, and costs one draw call in total. Each wall
shares one Model across its cubes, so the only rendering difference is instancing. Toggle each wall to compare the frame rate, and
note that the InstancingRenderFeature has to be added to the compositor by hand in code-only projects.

The `Program.cs` file shows how to:

- Reducing draw calls with an InstancingComponent
- Building an InstancingUserArray from world matrices
- Registering InstancingRenderFeature on the MeshRenderFeature
- Sharing one Model between many entities
- Toggling a ModelComponent to compare rendering cost
- Using helpers: SetupBase3D
- Using helpers: Add3DCameraController
- Using helpers: AddProfiler

![GPU Instancing](media/instancing.webp)

View on [GitHub](https://github.com/stride3d/stride-community-toolkit/tree/main/examples/code-only/E10_3D_Instancing).

[!code-csharp[](../../../../examples/code-only/E10_3D_Instancing/Program.cs?start=1&end=211)]