---
generated: true
slug: stress-pile-2d
---

# Basic2D Scene (Stress Pile)

Thousands of 2D physics bodies piling up, drawn in a single draw call through instancing, with the
shape, batch size and spawn layout switchable while it runs. Because one master entity draws every
body, all of them share a single Model and shapes cannot be mixed - changing shape clears and
respawns the pile, which the example uses to show how to tear a pile down safely. Models are cached
per shape and the instancing object is reused rather than recreated, so switching costs nothing.
Grid spawns are deliberately jittered: a perfectly regular lattice of touching bodies degenerates
Bepu's broad-phase tree.

The `Program.cs` file shows how to:

- Drawing thousands of physics bodies in a single draw call
- Confining bodies to the XY plane with Body2DComponent
- Sharing one Model across every body instead of generating one each
- Tearing down an instanced pile safely, clearing the instancing before the entities
- Switching shape, batch size and layout at runtime with DebugTextDropdown
- Why a perfectly regular spawn lattice must be jittered
- Using helpers: AddInstancingSupport, AddInstancingBufferUpload, AddBepu3DPhysics

![Basic2D Scene (Stress Pile)](media/stress-pile-2d.webp)

View on [GitHub](https://github.com/stride3d/stride-community-toolkit/tree/main/examples/code-only/Example01_Basic2DScene_StressPile).

[!code-csharp[](../../../../examples/code-only/Example01_Basic2DScene_StressPile/Program.cs?start=1&end=349)]
