---
generated: true
slug: sync-script
---

# SyncScript - moving a body every frame

A cube driven in a circle by a SyncScript, which is the ordinary way to run code every frame. The
part worth copying is how it moves: the body is made kinematic and given a velocity, rather than
a position written to Transform.Position. With a physics body attached the simulation owns the
transform, so a position written there moves the mesh and leaves the collider behind; a velocity
moves both, which is what lets the cube push dynamic bodies out of the way. SetTargetPose is the
call for once-per-physics-step code, not for a per-frame Update.

The `Program.cs` file shows how to:

- Running per-frame logic by deriving from SyncScript
- Attaching a script to an entity with Entity.Add
- Fetching a sibling component with Entity.Get
- Why physics owns the transform once a body is attached
- Moving a kinematic body with LinearVelocity, not Transform.Position
- Why SetTargetPose belongs in ISimulationUpdate and not in a per-frame Update
- Framerate independence with Game.DeltaTime
- Using helpers: SetupBase3DScene, AddSkybox, AddProfiler, Create3DPrimitive

![SyncScript - moving a body every frame](media/sync-script.webp)

View on [GitHub](https://github.com/stride3d/stride-community-toolkit/tree/main/examples/code-only/E02_3D_SyncScript).

[!code-csharp[](../../../../examples/code-only/E02_3D_SyncScript/Program.cs?start=1&end=28)]