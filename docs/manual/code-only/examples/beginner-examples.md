---
generated: true
---

# C# Beginner Examples

One new idea at a time, on top of the base scene. Toolkit helpers only, with no engine extension points to understand first.

## Examples Overview

- [Basic3D Scene (Every Primitive)](primitives-3d.md): Every 3D primitive the toolkit can build - cube, cone, capsule, sphere, cylinder, teapot, torus and triangular prism - dropped into one scene so the shapes, their default sizes and their generated colliders can be compared side by side.
- [Material](material.md): A row of cubes that differ only in their material, so the effect of each property is visible in isolation.
- [Mesh Line](mesh-line.md): A line drawn between two spheres, built as a real mesh rather than a debug primitive.
- [Give Me a Cube](give-me-cube-body.md): Add behaviour to an entity with a SyncScript component instead of the update callback of game.Run.
- [SyncScript - moving a body every frame](sync-script.md): A cube driven in a circle by a SyncScript, which is the ordinary way to run code every frame.
- [Entity Text (Screen-Space)](entity-text.md): A gallery of everything EntityTextComponent can do, one feature per pole: anchoring, shadows, backgrounds, scaling, rotation, opacity, distance fading, several texts on one entity, and HUD text pinned to window corners that survives resizing.
- [World Text (In-Scene)](world-text.md): A gallery of everything WorldTextComponent can do, one setting per station: billboarding that stays upright, free billboarding, text fixed in place, text lying flat on the ground, world-unit sizing, depth-tested text hidden behind a wall next to text drawn through it, and distance fading.
- [Basic2D Scene (Multiple Primitives)](primitives-2d.md): Create a minimal 2D scene using toolkit helpers and place multiple different primitive shapes.
- [Basic2D Scene (Falling Shapes)](falling-shapes-2d.md): Create a minimal 2D scene using toolkit helpers and place multiple capsule primitives with flat materials.
- [Basic2D Scene (Debug Rendering)](debug-render-2d.md): A pile of falling 2D shapes with the physics debug overlays turned on, so what the simulation is actually solving can be seen rather than inferred.
- [DPI-Aware Window](dpi-aware.md): The capsule scene again, with one difference that is not in the C# at all: an app.manifest declaring the process per-monitor DPI aware, referenced from the csproj.

> [!NOTE]
> Each example references a handful of toolkit packages. The `using` directives at the top of
> every listing name them, and the linked project file on GitHub is authoritative. A few examples
> also need a third-party package - Box2D.NET, Jitter2, Myra or ImGui - which their page calls out.

[!INCLUDE [basic-examples-outro](../../../includes/manual/examples/basic-examples-outro.md)]
