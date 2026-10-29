# Making components work in Game Studio

A toolkit library that ships `EntityComponent` types has to satisfy four requirements before those
components are usable from Game Studio. None of them produces an error naming the cause, and each
fails at a different moment.

| Missing | What the user sees |
|---|---|
| `Module.cs` | The component never appears in the Add-component list at all |
| `[DataContract]` | It appears, then throws `No serializer available for type ...` when added |
| A processor that provisions its own renderer | It adds and configures cleanly, and draws nothing until the game runs |
| Editor-safe camera and font lookups | The viewport draws nothing, and the Output pane shows an exception from the renderer |

A component works in code-only projects without any of this, so the library looks finished while
the editor path is never exercised. `ShapeComponent`, `WorldTextComponent` and
`EntityTextComponent` meet all four, and each section names the code to copy.

## 1. Register the assembly for scanning

Game Studio only inspects assemblies that have registered themselves. Without this file the
component types are invisible to the editor - not greyed out, not erroring, simply absent.

Add a `Module.cs` at the root of the library:

```csharp
using Stride.Core;
using Stride.Core.Reflection;
using System.Reflection;

namespace Stride.CommunityToolkit.YourLibrary;

internal static class Module
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        AssemblyRegistry.Register(typeof(Module).GetTypeInfo().Assembly, AssemblyCommonCategories.Assets);
    }
}
```

> [!NOTE]
> `ModuleInitializer` here is **`Stride.Core.ModuleInitializerAttribute`**, not the C#
> `System.Runtime.CompilerServices` attribute of the same name. The `using Stride.Core;` matters.

`Stride.CommunityToolkit`, `DebugShapes`, `Effects`, `ImGui`, `Shapes`, `Bepu` and `Box2D` each carry
a copy of this file. Scripts need it too: a script with every editor attribute stays out of the
editor when its assembly has no `Module.cs`.

## 2. Make the component serializable, and editable

The editor clones a component from the asset side to the game side through a generated
`DataSerializer`, and Stride's assembly processor only generates one for types marked
`[DataContract]`. Without it, adding the component throws immediately.

Follow the shape the engine's own components use:

```csharp
[DefaultEntityComponentProcessor(typeof(ShapeProcessor), ExecutionMode = ExecutionMode.All)]
[DataContract("ShapeComponent")]
[Display("Shape", Expand = ExpandRule.Once)]
[ComponentCategory("Rendering")]
public sealed class ShapeComponent : ActivableEntityComponent
```

`DataContract`, `Display`, `ExpandRule` and `DataMemberIgnore` live in `Stride.Core`;
`ComponentCategory` lives in `Stride.Engine`. Category names are free-form strings - the engine uses
*Lights*, *Model*, *Physics*, *Scripts*, *Sprites*, *UI* and friends, and anything else you write
becomes a new group in the list.

`ExecutionMode.All` is the default and the right value: the scene editor is a running game, and the
processor is what draws the component there (section 3). Say `Runtime` only for a processor that
truly cannot work in the editor.

The serializer and the editor also need a public parameterless constructor (analyser `STRDIAG010`).
Declaring any other constructor removes the one the compiler supplies, so declare the parameterless
one too.

### Audit every public property before you add it

`[DataContract]` turns the whole public surface into editor-authorable state, and the property grid
has rules of its own:

- **A live runtime handle** has to be excluded, or the editor tries to serialize something
  meaningless:

  ```csharp
  /// <summary>The batch this shape draws through, or null for the game's default.</summary>
  [DataMemberIgnore]
  public ShapeBatch? Batch { get; set; }
  ```

- **Collections are `List<T>`, never arrays.** The grid shows an array read-only, and the asset
  serializer loads an array only into an instance of exactly the saved size - a property that
  defaults to an empty array can never load anything. `ShapeComponent.Vertices` is the worked example.
- **No nullable value types.** The grid cannot edit a `float?`. Express "not set" with a sentinel the
  value cannot legitimately take, the way `ShapeComponent.Inherit` uses a negative width, or with 0
  where 0 means off, the way the text components' `MaxDistance` does.
- **A read-only hint property does not work.** `ObjectDescriptor` forces `DataMemberMode.Never` for a
  getter-only `string` or value type, even with an explicit `[DataMember]`, and Stride has no
  description or tooltip attribute. The display name is the only guaranteed-visible text.

## 3. The processor provisions what draws it

A component is data; something else draws it - a scene renderer, or a render feature with a render
object. In a code-only game the setup code registers that with an `Add*` call. Game Studio never
runs a game's setup code, so the processor has to do it, and it has to do it right:

- **On the owning scene system's compositor.** Not on whatever the render context carries: in the
  editor that is the gizmo compositor's render system, and a renderer registered there draws in
  nothing. `SceneRendererRegistration` in `Rendering.Compositing` finds the compositor of the scene
  system whose scene instance is the processor's entity manager.
- **Again whenever the compositor changes.** The editor starts on a fallback compositor and swaps in
  the project's asset, and swaps again on every change of the viewport's view mode. Check per frame
  against the compositor you last registered on; it is one reference comparison.
- **Only when there is something to draw**, so an empty scene gets no renderer.
- **In `Draw`, not `OnSystemAdd`.** The processor's `Draw` runs before the compositor each frame, so
  the first frame's compositor is known there, and a swap is seen the frame it happens.

For a scene renderer that is one call in the processor's `Draw`:

```csharp
SceneRendererRegistration.Ensure(Services, EntityManager, ref _ensuredOn, () => new WorldTextRenderer());
```

`ShapeProcessor` does the same steps by hand, because it registers a `RootRenderFeature` and a
`ShapeBatch` render object into the visibility group rather than a scene renderer. Keep the game's
manual call: it still gives a code-only user the handle, the ordering and the policy choices the
automatic path decides for them, and a processor should prefer a batch or renderer the game
registered over one of its own.

## 4. Cameras and fonts the editor way

Two things a renderer takes for granted in a game are not there in the editor:

- **The camera is not in a slot.** A game's compositor names its camera in slot zero; the editor
  clears the slots and hands its own camera to the top-level camera renderer as an external camera.
  `CompositorCameras.Find(context)` in `Rendering.Compositing` tries the slot and then walks the
  renderer tree, and is what every toolkit renderer outside the camera renderer uses.
- **The built-in font is not in the content database.** The editor's database holds the project's
  built assets, and its not-found path throws `NotImplementedException` rather than returning. Ask
  `Content.Exists` first and fall back to a system font: `RendererDefaults.LoadDefaultFont` in
  `Rendering.Text` does exactly that and is what the text renderers call.

Two limits remain, and belong in the user docs rather than in code: clicking a component's drawing
in the viewport does not select its entity, because Stride's picking pass renders entity ids for
models only; and the editor draws with a system font where a game draws with Stride's.

## Checklist

- [ ] `Module.cs` registers the assembly with `AssemblyRegistry`
- [ ] Every `EntityComponent` has `[DataContract]`, `[Display]`, `[ComponentCategory]` and a public parameterless constructor
- [ ] Runtime-only handles on those components are `[DataMemberIgnore]`
- [ ] Collections are `List<T>`; no nullable value types; no read-only hint properties
- [ ] The processor runs in the editor and provisions its renderer on the owning compositor, re-checked per frame
- [ ] Renderers resolve the camera through `CompositorCameras` and fonts through `RendererDefaults.LoadDefaultFont`
- [ ] Verified in Game Studio's viewport, in both view modes, against a package built with
      [`build/pack-local.cs`](building.md#building-local-nuget-packages) - a `ProjectReference` does not
      exercise any of this