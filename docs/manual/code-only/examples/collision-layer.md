---
generated: true
slug: collision-layer
---

# Collision Layer

Four layers, Players, Enemies, Shots and Debris, and the collision matrix that says which pairs
collide, shown twice: as a grid on screen and as the scene itself. Each lane is a shelf on one layer
and a cube of every layer is dropped onto every shelf, so a cube whose layer collides with the shelf's
stays on it and any other falls through to the ground. Keys, or clicks on the grid, flip one pair at a
time and the cubes drop again; the mouse ray shows the same layers filtering a raycast through its mask, and a right click
fires a small body on the Shots layer from the camera, which flies through whatever its layer ignores
and is stopped by whatever it collides with. Layers are the readable choice when the rules are a
table; the collision group example is the tool for rules that follow a formula.

The `Program.cs` file shows how to:

- Putting a body on a collision layer
- Filling in the simulation's collision matrix pair by pair, and changing it while the game runs, by key or by a click on the grid
- Seeing every cell of the matrix as a physical outcome, on a shelf or through it
- Filtering a raycast with a CollisionMask built from the same layers
- Firing a body on one layer and watching the matrix decide what it flies through
- When to prefer a layer matrix over a collision group's index rule
- Using helpers: SceneSetup3D, UseBepu, UseSkybox, CreatePrimitive, AddShapeBatch, AddEntityTextRenderer, AddGrabber, TeleportAtRest, ScreenToWorldRay, TextOverlay

![Collision Layer](media/collision-layer.webp)

View on [GitHub](https://github.com/stride3d/stride-community-toolkit/tree/main/examples/code-only/E05_3D_CollisionLayer).

[!code-csharp[](../../../../examples/code-only/E05_3D_CollisionLayer/Program.cs?start=1&end=417)]