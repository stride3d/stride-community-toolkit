---
generated: true
slug: primitives-3d
---

# 3D Primitives

Nine of the toolkit's 3D primitives - cube, box, cone, capsule, sphere, cylinder, teapot, torus and
triangular prism - dropped into one scene so the shapes, their default sizes and their generated
colliders can be compared side by side. Naming each entity is what makes the scene resettable: R
removes everything with that name and rebuilds, which is the simplest safe teardown pattern there
is. P and F11 turn on the collider and debug-mesh overlays.

The `Program.cs` file shows how to:

- Creating nine Primitive3D shapes and comparing their defaults
- Sizing a primitive through its factory arguments, such as Primitive3D.Cone(0.5f, 3)
- Rotating an entity with Transform.Rotation
- Tagging entities with a name so the scene can be torn down and rebuilt
- Inspecting the generated colliders and debug meshes with AddPhysicsDebug, on P and F11
- Adding keyboard instructions as a DebugOverlay section
- Using helpers: SceneSetup3D, AddGrabber, AddPhysicsDebug, CreatePrimitive

![3D Primitives](media/primitives-3d.webp)

View on [GitHub](https://github.com/stride3d/stride-community-toolkit/tree/main/examples/code-only/E02_3D_Primitives).

[!code-csharp[](../../../../examples/code-only/E02_3D_Primitives/Program.cs?start=1&end=98)]