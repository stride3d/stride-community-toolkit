---
generated: true
slug: gpu-picking
---

# GPU Picking

What is under the mouse, answered by the renderer instead of by physics. Nothing in the scene
has a collider - a teapot, a ring of pillars, a few primitives and a field of crates drawn as
one instanced model - and the GPU picker names the entity, mesh, material, instance and
surface point under the pointer by drawing ids into a hidden target and reading back one
pixel. One call, AddGpuPicker, and a result two frames later.

The `Program.cs` file shows how to:

- Picking without colliders through AddGpuPicker, and what the call builds in the compositor
- Why the answer is two frames late, and why that does not matter for hover and click
- Reading the entity, mesh, material and instance index from a PickResult
- The hit point rebuilt from the depth the picking pass wrote
- Picking an instanced model and highlighting the one instance under the pointer
- Where physics raycasts still win, and where they cannot see at all

View on [GitHub](https://github.com/stride3d/stride-community-toolkit/tree/main/examples/code-only/E09_3D_GpuPicking).

[!code-csharp[](../../../../examples/code-only/E09_3D_GpuPicking/Program.cs?start=1&end=253)]