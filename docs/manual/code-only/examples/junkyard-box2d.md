---
generated: true
slug: junkyard-box2d
---

# Junkyard (Box2D)

A faithful replica of the Box2D.NET BenchmarkJunkyard sample: 8,000 small five-sided rocks rain
into a walled yard and a kinematic plow sweeps back and forth through the pile, driven by a
target transform once per fixed step. Rendering works exactly like the Box2D testbed: no meshes,
materials or entities - every shape is submitted each frame to the toolkit's ShapeBatch,
whose shader (a port of the testbed's solid_polygon shader) draws them all in one instanced
call with the 60%-alpha fill and pixel-constant border computed per fragment. Body states show
as the testbed's colours - pink awake, salmon fast-movers, gray sleepers.

The `Program.cs` file shows how to:

- Replicating a Box2D testbed benchmark scene in Stride, rendering included
- Immediate-mode shape drawing with ShapeBatch - no meshes, materials or entities
- An SDF shader computing fill, border and transparency per fragment, stable under any zoom
- Entity-less physics bodies as the single source of truth, read directly each frame
- Driving a kinematic body with SetTargetTransform once per fixed step
- Hooking per-fixed-step logic through IBox2DSimulationUpdate
- Colour-coding awake, fast and sleeping bodies straight from body state

> [!NOTE]
> This example references `Stride.CommunityToolkit.Box2D`, which is not on NuGet yet. Run it from a clone of the
> repository, where the package is a project reference; a copy of the project on its own will not build.

![Junkyard (Box2D)](media/junkyard-box2d.webp)

View on [GitHub](https://github.com/stride3d/stride-community-toolkit/tree/main/examples/code-only/E06_Box2D_Junkyard).

[!code-csharp[](../../../../examples/code-only/E06_Box2D_Junkyard/Program.cs?start=1&end=312)]