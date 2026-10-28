---
generated: true
slug: first-person-character
---

# First-Person Character (Bepu)

A first-person character built entirely from code - no Game Studio scene - on a Bepu
CharacterComponent, with boxes to walk into and jump onto. Two details make it work. The scene uses
a camera without the setup's controller, because the fly-around debug camera would fight the
controller for the same camera. And the controller is a component plus a
processor pair, registered automatically by DefaultEntityComponentProcessor, which is how you add
per-frame behaviour to many entities without a script on each one.

The `Program.cs` file shows how to:

- Building a character controller with Bepu CharacterComponent
- Attaching a collider by passing the component through BepuPhysicsOptions
- Why the setup's camera has no controller when the scene brings its own
- Pairing an EntityComponent with an EntityProcessor
- Auto-registering a processor with [DefaultEntityComponentProcessor]
- Driving a physics body from the camera entity
- Using helpers: SceneSetup3D, UseSkybox, CreatePrimitive

![First-Person Character (Bepu)](media/first-person-character.webp)

View on [GitHub](https://github.com/stride3d/stride-community-toolkit/tree/main/examples/code-only/E05_3D_FirstPersonCharacter).

[!code-csharp[](../../../../examples/code-only/E05_3D_FirstPersonCharacter/Program.cs?start=1&end=69)]