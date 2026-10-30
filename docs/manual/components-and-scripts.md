# Components and Scripts - which one, and why

Everything you attach to an entity is a component, and a script is the easiest kind to write. This
page explains when a plain data component, a script or a physics-derived component is the right
choice. Every rule is illustrated by code in [Cube Collapse](code-only/examples/cube-collapse.md)
(`examples/code-only/E20_3D_CubeCollapse`).

## One entity, many components

An entity in Stride is almost nothing by itself: an ID, a name and a transform. Everything else it
*is* or *does* comes from the components attached to it:

```mermaid
graph LR
    E["Entity 'Cube'"] --> T[TransformComponent<br/><i>where it is</i>]
    E --> M[ModelComponent<br/><i>what it looks like</i>]
    E --> B[BodyComponent<br/><i>how it collides and falls</i>]
    E --> C[CubeComponent<br/><i>what colour it counts as</i>]
```

The first three come from the engine. The last one you write, as a plain data component or as a
script. The two behave differently.

## The three kinds of component you can write

| Base class | It represents | Has per-frame code? | Example in this repo |
|---|---|---|---|
| `EntityComponent` | What a thing **is** - data, identity | No | `CubeComponent` (a colour and a grid position) |
| `ScriptComponent` (`SyncScript`, `AsyncScript`, `StartupScript`) | What a thing **does** over time | Yes - `Update()` / `Execute()` | `ScorePopupScript` (animates a floating score) |
| A specialised engine component, e.g. `BodyComponent` | Behaviour inseparable from an engine system | Via that system's own hooks | `SlidingCubeComponent` (a body that only falls straight down) |

## Why "just make it a script" goes wrong

A script is a component *plus* an enrolment in the script scheduler. That enrolment is the cost, and
it is easy to pay it for nothing.

Consider a match-3 style game where every cube needs a colour. Two ways to store it:

```csharp
// As a data component - what Cube Collapse actually does
public class CubeComponent : EntityComponent
{
    public Color Color { get; set; }
}

// As a script - tempting, works, and wasteful
public class CubeScript : SyncScript
{
    public Color Color { get; set; }

    public override void Update() { } // nothing to do, every frame, times 1000 cubes
}
```

Both compile, and both are found with `entity.Get<T>()`. The script version schedules a
thousand empty `Update()` calls per frame on a 10×10×10 board. A plain `EntityComponent` costs
nothing until something asks for it.

A component's data **lives and dies with its entity**. The usual alternative - a
`Dictionary<Entity, Color>` somewhere - has to be cleaned up by hand every time an
entity is removed, and when someone forgets, it leaks silently. Attach the data to the entity and
that class of bug cannot exist.

Finally, a typed component is **identity you can query**. "Is this a playable cube?" is answered by
`entity.Get<CubeComponent>() is not null` - the compiler checks it, a rename refactors it. The
string alternative (`entity.Name == "Cube"`) matches nothing, silently, the day someone renames the
entity.

## When a script is exactly right

A script is the right tool when something happens over time:

- **`SyncScript`** - logic that runs every frame: animating a score popup's rise and fade, orbiting
  a camera, counting a displayed score up toward the real one. (`ScorePopupScript`,
  `CameraRotationScript`, `ScoreboardScript`.)
- **`AsyncScript`** - logic that is naturally a loop with waits: read input, act, `await NextFrame()`.
  (`CubeClickScript` - raycast on click, clear the group, repeat.)
- **`StartupScript`** - one-time setup that needs the entity to exist in the scene first.

The test is simple: **does this component need to be *called* every frame, or only *asked* when
something else is interested?** Called → script. Asked → data component.

```mermaid
flowchart TD
    A[I need to attach something<br/>to an entity] --> B{Does it do work<br/>every frame or in a loop?}
    B -- "No - it's facts about the entity" --> C["EntityComponent<br/>(data: colour, team, grid cell, hit points)"]
    B -- Yes --> D{Is the behaviour inseparable<br/>from a physics body?}
    D -- No --> E["ScriptComponent<br/>(SyncScript / AsyncScript / StartupScript)"]
    D -- Yes --> F["Derive from BodyComponent<br/>+ ISimulationUpdate"]
    C --> G{Do many of them need<br/>processing as one set?}
    G -- Yes --> H["Add an EntityProcessor<br/>(see below)"]
    G -- No --> I[Done - it just holds data]
```

## The special case: behaviour that belongs to a physics body

Some behaviour is about being a rigid body: fall slower, stay in a lane, never rotate. A
`SyncScript` that finds the entity's `BodyComponent` and pushes it around works, but deriving from
`BodyComponent` is better for four reasons:

1. **No finding, no timing bug.** A physics body registers with the simulation the moment it enters
   the scene, but a script's `Start()` waits its turn in the script system - so physics callbacks
   can run *before* `Start`, and a script that cached its body in `Start` reads null. When the
   component *is* the body, `this.LinearVelocity` cannot be null and cannot be stale.
2. **The right hooks exist only there.** Some body state can only be set at the moment the body
   attaches to the simulation - earlier writes are silently ignored. A derived component overrides
   `AttachInner` and acts at exactly that moment; a script can only poll and hope.
3. **Physics-rate, not frame-rate.** `ISimulationUpdate.SimulationUpdate` runs once per fixed
   physics tick. Per-frame velocity math breaks subtly when the frame rate and the physics rate
   diverge; per-tick math cannot.
4. **One component, whole concept.** `entity.Get<SlidingCubeComponent>()` returns the body *and*
   the game meaning in one query - nothing can exist half-configured.

Both live examples are in Cube Collapse: `SlidingCubeComponent` (pins a cube to its column and
locks its rotation) and `SlowFallComponent` (a letter that falls under a fraction of gravity):

```csharp
public class SlowFallComponent : BodyComponent, ISimulationUpdate
{
    public float GravityScale { get; set; } = 0.15f;

    public void SimulationUpdate(BepuSimulation simulation, float simTimeStep)
    {
        if (!Awake) return; // a resting body should be allowed to sleep

        // The step about to run adds one tick of full gravity; remove the share we don't want
        LinearVelocity -= simulation.PoseGravity * ((1f - GravityScale) * simTimeStep);
    }

    public void AfterSimulationUpdate(BepuSimulation simulation, float simTimeStep) { }
}
```

The trade-off: deriving couples the class to one physics engine, and C# single inheritance
means one body cannot stack two derived behaviours. When behaviours must mix and match, or must work
on bodies you don't create yourself, a script acting on the body is the composable choice - just
resolve the body lazily (`_body ??= Entity.Get<BodyComponent>()`) inside the physics callback, never
in `Start`.

## Where this is heading: processors (the "S" in ECS)

The data/behaviour split is the component half of **ECS** (Entity–Component–System). The system
half is the `EntityProcessor`: it registers interest in one component type, and the engine tells it
whenever such a component enters or leaves the scene - anywhere in the entity hierarchy. One object then owns the whole population:

```mermaid
graph TD
    subgraph Scene
        E1[Entity] --> C1[TextComponent]
        E2[Entity] --> C2[TextComponent]
        E3[Child entity] --> C3[TextComponent]
    end
    P["EntityTextProcessor<br/><i>told about every add and remove</i>"]
    C1 -.-> P
    C2 -.-> P
    C3 -.-> P
    P --> R["Renderer<br/><i>draws the whole set each frame</i>"]
```

This is why data components scale: attach one attribute
(`[DefaultEntityComponentProcessor(typeof(MyProcessor))]`) and your plain data component gains
engine-managed collection with correct lifetimes, with the per-frame work done once, centrally,
instead of in a thousand per-instance `Update()` calls. The toolkit's `EntityTextComponent` and
`WorldTextComponent` both work this way - the component is pure data describing text; a processor
tracks them; a renderer draws them.

## Rules of thumb

- **Data → `EntityComponent`.** Colour, team, hit points, grid position. No `Update`. Free until queried.
- **Per-frame behaviour → a script.** Animation, input loops, timers.
- **Behaviour that *is* the physics body → derive from `BodyComponent`** and use `ISimulationUpdate`.
- **Many instances processed as one set → a processor**, keeping the component pure data.
- Give every component a **public parameterless constructor** - serialization needs it (the
  `STRDIAG010` analyser will remind you). Pass dependencies as `required` init-only properties, not
  constructor parameters.
- Identify entities by **component presence**, not by name strings.
- Never mirror per-entity data in an external dictionary that you must remember to clean up -
  attach it, and let it die with the entity.

## See it all in one example

`examples/code-only/E20_3D_CubeCollapse` uses every kind:

| Piece | Kind | Why |
|---|---|---|
| `CubeComponent` | `EntityComponent` | A cube's colour and grid cell - pure data, a thousand of them |
| `ScorePopupScript`, `ScoreboardScript`, `CameraRotationScript` | `SyncScript` | Things that visibly change every frame |
| `CubeClickScript` | `AsyncScript` | An input loop: click, clear, await the next frame |
| `SlidingCubeComponent`, `SlowFallComponent` | `BodyComponent` + `ISimulationUpdate` | Behaviour inseparable from the rigid body |
| `EntityTextComponent` + `EntityTextProcessor` (toolkit) | data + processor | Hundreds of texts, collected centrally, drawn by one renderer |

## See also

- [Cube Collapse example](code-only/examples/cube-collapse.md)
- [Bepu: Who Owns the Transform?](physics-extensions/bepu-transform-ownership.md)
- [Game Studio](game-studio.md)