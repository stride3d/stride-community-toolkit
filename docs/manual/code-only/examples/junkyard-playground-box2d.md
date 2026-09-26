---
generated: true
slug: junkyard-playground-box2d
---

# Junkyard Playground (Box2D)

The playground sibling of the Junkyard replica: the same walled yard and sweeping plow, built
the Stride way - every shape is an entity carrying ShapeComponent and a Box2D body, so
components, scripts, events and the camera all join in. Pentagons, circles, capsules and boxes
fall and mix freely, switchable at runtime. A sensor gate turns anything passing through gold via
the library's sensor events; the mouse picks shapes up and throws them through Grabber2DScript,
N drops a new one at the cursor; middle-click makes the camera follow one through the pile.

The `Program.cs` file shows how to:

- One entity per shape with ShapeComponent - the testbed look through the component system
- Mixing shape kinds freely in one scene, impossible with per-model instanced masters
- Circles and capsules through the SDF shader's rounding radius
- Building the collider from the same vertices as the visual, so they always agree
- Sensor fixtures and the library's sensor events driving gameplay colour
- Picking shapes up and throwing them with Grabber2DScript, a kinematic anchor plus a motor joint
- Camera follow through Basic2DCameraController.FollowTarget

> [!NOTE]
> This example references `Stride.CommunityToolkit.Box2D`, which is not on NuGet yet. Run it from a clone of the
> repository, where the package is a project reference; a copy of the project on its own will not build.

![Junkyard Playground (Box2D)](media/junkyard-playground-box2d.webp)

View on [GitHub](https://github.com/stride3d/stride-community-toolkit/tree/main/examples/code-only/E06_Box2D_JunkyardInteractive).

[!code-csharp[](../../../../examples/code-only/E06_Box2D_JunkyardInteractive/Program.cs?start=1&end=489)]