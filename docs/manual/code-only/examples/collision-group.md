---
generated: true
slug: collision-group
---

# Collision Group

Collision groups are a number rule inside a layer: bodies with the same group Id ignore each other
when their indexes are the same or adjacent, and collide when two or more apart. Two stations show
the two uses: three lanes of named spheres running at each other, where A meets A and B meets B
pass through and A meets B bounces, and a chain of four links end to end on a swinging hook, with
indexes 0 to 3, where the ignored neighbours let it bend freely and links further apart still
collide. Keys switch the teams' groups off, so every lane bounces, and the chain's filtering off,
so the touching links collide at every bend. Groups suit rules that follow a formula; the collision
layer example is the table-driven choice.

The `Program.cs` file shows how to:

- Giving bodies a collision group Id and an index
- The rule: same Id, indexes less than two apart, no collision
- Teams that pass through each other but bounce off opponents
- A chain whose neighbouring links bend without colliding
- Counting collisions with a contact handler
- When an index rule beats a layer matrix
- Using helpers: SceneSetup3D, UseBepu, UseSkybox, CreatePrimitive, AddGrabber, TeleportAtRest, TextOverlay, WorldTextComponent

![Collision Group](media/collision-group.webp)

View on [GitHub](https://github.com/stride3d/stride-community-toolkit/tree/main/examples/code-only/E05_3D_CollisionGroup).

[!code-csharp[](../../../../examples/code-only/E05_3D_CollisionGroup/Program.cs?start=1&end=331)]