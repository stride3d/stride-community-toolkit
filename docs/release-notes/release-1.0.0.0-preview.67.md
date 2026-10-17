# What's new in Stride Community Toolkit 1.0.0.0-preview.67

This release is built against Stride 4.4.0 and reshapes the toolkit's surface: one package to start, shapes as records that carry their dimensions, the scene setup as data with the physics engine chosen on it, and a surface audit. Read the breaking changes first.

## What's Changed

### 💥 Breaking Changes

#### One package to start

`Stride.CommunityToolkit` brings in `Stride.AssetCompiler` (build-only) and holds `WindowsDpiManager`.

| Before | After | Note |
|---|---|---|
| `<PackageReference Include="Stride.CommunityToolkit.Windows" />` | `<PackageReference Include="Stride.CommunityToolkit" />` | Or any package that depends on it, such as `Stride.CommunityToolkit.Bepu`. |
| `using Stride.CommunityToolkit.Windows;` | `using Stride.CommunityToolkit.Engine;` | For `WindowsDpiManager`. |

- `Stride.CommunityToolkit.Windows` ships once more as an empty package that depends on the core, so a project still naming it keeps building; it will be unlisted. `Stride.CommunityToolkit.Linux` is unchanged.

#### Shapes are records that carry their dimensions

`Primitive3D` and `Primitive2D` in `Stride.CommunityToolkit.Rendering.ProceduralModels`, created through static factories whose arguments are named for what they are. The `PrimitiveModelType` and `Primitive2DModelType` enums and the `Size` and `Vertices` members of every options class are gone, and the three `Create3DPrimitive`/`Create2DPrimitive` pairs are one `CreatePrimitive` overloaded on the shape.

| Before | After | Note |
|---|---|---|
| `game.Create3DPrimitive(PrimitiveModelType.Sphere)` | `game.CreatePrimitive(Primitive3D.Sphere())` | The same for `Create2DPrimitive` and `Primitive2D`. |
| `game.Create3DPrimitive(PrimitiveModelType.Sphere, new() { Size = new Vector3(2, 0, 0) })` | `game.CreatePrimitive(Primitive3D.Sphere(2))` | The dimensions are the factory's arguments. |
| `PrimitiveModelType.Cube` with `Size = new Vector3(2)` | `Primitive3D.Cube(2)` | |
| `PrimitiveModelType.Cube` with `Size = new Vector3(1, 2, 3)` | `Primitive3D.Box(new Vector3(1, 2, 3))` | `PrimitiveModelType.RectangularPrism` is `Primitive3D.Box(size)` too. |
| `PrimitiveModelType.InfinitePlane` | gone | `Add3DGround` and Bullet's `AddInfinite3DGround` build their plane themselves. |
| `Primitive2DModelType.Circle` with `Size = new Vector2(0.5f)` | `Primitive2D.Circle(0.5f)` | Likewise `Rectangle(width, height)`, `Square(size)`, `Capsule(radius, height)`, `Triangle(width, height)`. |
| `Primitive2DModelType.Polygon` with `Size = new Vector2(radius, sides)` | `Primitive2D.RegularPolygon(radius, sides)` | |
| `Primitive2DModelType.Polygon` with `Vertices = points` | `Primitive2D.Polygon(points)` | |
| `entity.AddBepu3DPhysics(PrimitiveModelType.Cube, new() { Size = size })` | `entity.AddBepuPhysics(Primitive3D.Box(size))` | The same for the 2D and the Bullet pairs; the names lose their `3D` and `2D`, see below. |
| `game.Add3DGround(new() { Size = new Vector3(40, 1, 40) })` | `game.Add3DGround(new Vector2(40))` | `Add2DGround`, `Add3DGround` and `AddInfinite3DGround` take a `Vector2? size` first, the options second. |
| `ShapeFixtureBuilder.AttachShape(Primitive2DModelType.Circle, new Vector2(r, r), body)` | `ShapeFixtureBuilder.AttachShape(Primitive2D.Circle(r), body)` | |
| `ConeCollider.Create(Vector3? size)` | `ConeCollider.Create(radius, height)` | Also `Create(ConeShape)`. |
| `TorusCollider.Create(size)` | `TorusCollider.Create(radius, thickness)` | Also `Create(TorusShape)`. |
| `TeapotCollider.Create(float? size)` | `TeapotCollider.Create(size)` | Also `Create(TeapotShape)`. |
| `TriangularPrismCollider.Create(Vector3? size)` | `TriangularPrismCollider.Create(size)` | Also `Create(TriangularPrismShape)`. |
| `PolygonCollider.Create(vertices, radius, sides, depth)` | `PolygonCollider.Create(vertices, depth)` or `Create(radius, sides, depth)` | Also the two `*Shape` overloads. |
| `type == PrimitiveModelType.Sphere` | `shape is Primitive3D.SphereShape sphere` | Which also gives `sphere.Radius`. |
| `type.ToString()` for a label | `shape.Name` | `Cube` for a box with three equal extents, `Square` for a rectangle with equal sides. |

- The factories: `Sphere(radius)`, `Capsule(radius, length)`, `Cylinder(radius, height)`, `Cone(radius, height)`, `Torus(radius, thickness)`, `Plane(width, length)`, `Teapot(size)`, `TriangularPrism(size)`, `Cube(size)`, `Box(size)`; each round shape also takes a `tessellation`.

#### The scene setup is data

`SceneSetup3D` and `SceneSetup2D` in `Stride.CommunityToolkit.Engine`, applied by `Run` before `start`, with the physics engine chosen on the setup. `SetupBase3DScene`, `SetupBase2DScene`, `SetupBase3D` and `SetupBase2D` are gone, and so are the Bepu and Bullet overloads of `CreatePrimitive`, `Add3DGround` and `Add2DGround` that a `using` line used to select.

| Before | After | Note |
|---|---|---|
| `game.SetupBase3DScene();` in `Start` | `game.Run(new SceneSetup3D().UseBepu(), start: Start)` | The line leaves `Start`. `SetupBase2DScene` is `new SceneSetup2D().UseBepu()`; the Bullet pair is `.UseBullet()`. |
| `game.SetupBase3D();` then the pieces by hand | `new SceneSetup3D { Ground = null, Camera = CameraSetup.Perspective(), … }` | With the pieces you kept. |
| `game.SetupBase2D(clearColor);` | `new SceneSetup2D { ClearColor = clearColor, … }` | |
| `game.AddSkybox();` | `.UseSkybox()` on the setup, or nothing | A gradient sky is the default; `Skybox = null` leaves it out. `AddSkybox()` itself stays. |
| `game.Add3DCamera(); game.Add3DCameraController();` | `Camera = CameraSetup.Perspective(position, rotation).WithController()` | |
| `game.AddStudioLighting();` | `Lighting = LightingSetup.Studio()` | |
| `game.ConfigurePostEffects(fx => …);` | `PostEffects = fx => …` | The method stays for a scene built by hand. |
| `game.UseGameSettings(s => …);` | `GameSettings = s => …` | The method stays for a scene built by hand. |
| `game.Run(start: Start)` with the scene built by hand in `Start` | `game.Run(SceneSetup.None, start: Start)` | `Run` with no setup now applies `SceneSetup3D.Default`. The headless tests pass `None` too. |
| `game.CreatePrimitive(shape)` with `using Stride.CommunityToolkit.Bepu;` | the same call, with `.UseBepu()` on the setup | The body comes from the engine the setup chose; a setup without one gives a mesh alone. |
| `game.CreatePrimitive(shape, new Primitive3DEntityOptions { … })` in a game with physics | `new Primitive3DEntityOptions { IncludePhysics = false, … }` | To keep that entity physics-free. |
| `game.CreatePrimitive(shape, new() { Component = … })` | `game.CreatePrimitive(shape, new Bepu3DPhysicsOptions { Component = … })` | The core method takes `Primitive3DEntityOptions`, so the Bepu options still fit. |
| `using Stride.CommunityToolkit.Skyboxes;` for `SkyboxGenerator` | `using Stride.CommunityToolkit.Rendering;` | `SkyboxGenerator` and `SkyboxGeneratorContext` moved into the core; the Skyboxes package depends on the core. |

- `game.Add3DGround(size, new Bepu3DPhysicsOptions { … })` is unchanged in spelling: the method is the core's and takes `Primitive3DEntityOptions`. `AddInfinite3DGround` stays in Bullet.

#### One name where the shape says the world

The `3D` and `2D` in a method name repeated what its `Primitive3D` or `Primitive2D` argument already says.

| Before | After | Note |
|---|---|---|
| `entity.AddBepu3DPhysics(shape, options)`, `entity.AddBepu2DPhysics(shape, options)` | `entity.AddBepuPhysics(shape, options)` | One name, overloaded on the shape. |
| `entity.AddBullet3DPhysics(shape, options)`, `entity.AddBullet2DPhysics(shape, options)` | `entity.AddBulletPhysics(shape, options)` | |
| `Procedural3DModelBuilder.Build(shape)`, `Procedural2DModelBuilder.Build(shape)` | `ProceduralModelBuilder.Build(shape)` | |

- Kept with the digit, on purpose: `Add3DGround`/`Add2DGround`, `Add3DCamera`/`Add2DCamera` and the controllers, where nothing but the name says the world, and the types that differ in content (`SceneSetup3D`/`2D`, the options, the controllers).

#### Every helper takes `IGame`

The `Game` extension methods take `IGame`, the type a script's `Game` property has, so a Game Studio script sees the whole toolkit. Only `Run`, `SetWindowSize` and `UseGameSettings` keep `Game`: they act before the engine starts, on members `IGame` does not have.

| Before | After | Note |
|---|---|---|
| `Game.Add3DGround()` in a script did not compile | `Game.Add3DGround()` | The same for every other helper: `AddSkybox()`, `AddProfiler()`, `AddGrid()`, the lights, the renderers. |
| `Action<Game> step = game.AddSkybox;` | `Action<IGame> step = game.AddSkybox;` | A method group passed as a delegate names the new receiver; a call does not change. |
| `ConfigurePostEffects()` returned `Game` | returns `IGame` | |
| `new Chart(Game game, …)`, `SkyboxGeneratorContext(Game game)`, `SkyboxSetupBase.Create(Game game)` | the same with `IGame` | A `Game` still fits. |

- `GetSceneSystem()`, `GetScriptSystem()`, `GetAudioSystem()`, `GetInputManager()` and `GetGraphicsDeviceManager()` on `IGame`, in `GameServiceExtensions`, read the systems a `Game` exposes as properties from the service registry; the helpers use them, and a script can too.

#### One screen anchor

`ScreenAnchor` in `Stride.CommunityToolkit.Rendering` names the nine points of the screen the way `TextAnchor` names the points of a text block: the four corners, the middle of each edge and the centre. It replaces `DisplayPosition` (which also carried `None` and `Custom`, meaningful only to the overlay) and the Shapes package's `ScreenCorner`.

| Before | After | Note |
|---|---|---|
| `DisplayPosition.TopLeft` | `ScreenAnchor.TopLeft` | The same four corner names; `using Stride.CommunityToolkit.Rendering;`. |
| `overlay.Position = DisplayPosition.Custom` | `overlay.Position = null` | `DebugOverlay.Position` is `ScreenAnchor?`; `SetPosition(x, y)` still sets both. |
| `overlay.Position = DisplayPosition.None` | `overlay.Visible = false` | The <kbd>F4</kbd> key flips the same property. |
| `Add3DCameraController(DisplayPosition.None)` | `Add3DCameraController(showHelp: false)` | Also `WithController(showHelp: false)` on a `CameraSetup` and `ShowHelp` on the script. |
| `Add3DCameraController(displayPosition: …)` | `Add3DCameraController(helpPosition: …)` | The parameter is named after what it places. |
| `shapes.Corner(ScreenCorner.BottomLeft)` | `shapes.Anchor(ScreenAnchor.BottomLeft)` | `ScreenAnchor.MiddleCenter` is the centre of the screen. |

#### One `GetComponentInChildren`

Two searches shared the name: a depth-first one in `EntityExtensions` that checked the entity itself first, and the breadth-first, children-only one in `EntitySearchExtensions`, whose parameterless form had been pushed to `GetComponentInChildrenBFS` to make room.

| Before | After | Note |
|---|---|---|
| `entity.GetComponentInChildren<T>()` (entity first, then depth-first) | `entity.GetComponentInDescendantsAndSelf<T>()` | The singular the `Descendants` family lacked; `T` must be an `EntityComponent`, as in the rest of the family. |
| `entity.GetComponentInChildrenBFS<T>()` | `entity.GetComponentInChildren<T>()` | Breadth-first, children only, as the `includeDisabled` overload beside it always was. |

#### Every helper returns what it makes

The seventeen `Add*` and `Show*` extensions that returned `void` now return the thing they add, or their receiver when they add nothing, so a chain of setup calls continues. Existing calls compile unchanged; in F#, pipe the result to `ignore`.

| Helper | Returns |
|---|---|
| `entity.Add3DCameraController()`, `Add2DCameraController()` | The controller script |
| `entity.AddGizmo()`, `AddLightDirectionalGizmo()` | The gizmo |
| `game.AddAllDirectionLighting()` | The six light entities |
| `game.AddGroundGizmo()` | The gizmo entity, or `null` without a ground |
| `game.AddWorldTextRenderer()`, `AddEntityTextRenderer()`, `AddEntityDebugSceneRenderer()` | The renderer, the existing one when there was one |
| `game.AddDebugShapes()` | The `ImmediateDebugRenderSystem` to draw with |
| `compositor.AddImmediateDebugRenderFeature()` | The feature |
| `game.AddSceneRenderer()`, `AddRootRenderFeature()`, `AddParticleRenderer()`, `ShowColliders()` | The game |
| `compositor.AddRootRenderFeature()`, `AddParticleStagesAndFeatures()` | The compositor |
| `compositor.EnsureSceneRenderer()` | The renderer of that type, instead of the compositor |

#### Namespaces that were folders

Nine namespaces that only mirrored a source folder are folded into the area a user already imports. A removed namespace is a `using` to delete, which the compiler names; nothing else changes.

| Before | After | Note |
|---|---|---|
| `Stride.CommunityToolkit.Scripts.Utilities` | `Stride.CommunityToolkit.Rendering.Text` | `DebugOverlay`, `DebugOverlaySection`, `TextElement`, `DebugTextDropdown` and its item, next to the text renderers and `TextAnchor`. |
| `Stride.CommunityToolkit.Rendering.Utilities` | `Stride.CommunityToolkit.Rendering` | `TextureCanvas`, `MeshBuilder`, `LetterMeshFactory`, `EarClipping`, `Anchor`, `Stretch`, `IndexingType`. |
| `Stride.CommunityToolkit.Extensions`, `.Graphics` | `Stride.CommunityToolkit.Rendering` | `TextureExtensions`, `TextureCanvasExtensions`, `BufferExtensions`, `GraphicsDeviceExtensions`. |
| `Stride.CommunityToolkit.Renderers` | `Stride.CommunityToolkit.Rendering.Text` | `EntityDebugSceneRenderer` and its options, a text renderer like the two beside it; `AddEntityDebugRenderer()` and `AddEntityDebugSceneRenderer()` in `EntityDebugRendererExtensions`. |
| `Stride.CommunityToolkit.Helpers` | `Stride.CommunityToolkit.Mathematics` | `VectorHelper`. |
| `Stride.CommunityToolkit.DebugShapes.Code` | `Stride.CommunityToolkit.DebugShapes` | The whole package; its root had no public type before. |
| `Stride.CommunityToolkit.Bepu.Colliders`, `.Bepu.Extensions` | `Stride.CommunityToolkit.Bepu` | The collider helpers and `ToConvexHullCollider()`, found without a second `using`. |
| `Stride.CommunityToolkit.Box2D.Events` | `Stride.CommunityToolkit.Box2D` | The contact and sensor event types. |
| `Stride.CommunityToolkit.Charts.Lines` | `Stride.CommunityToolkit.Charts` | `PolylineSampling`. |

#### Members that lied or did nothing

| Before | After | Note |
|---|---|---|
| `Bepu3DPhysicsOptions.IncludeCollider = false` and the 2D, Bullet twins | Gone | `IncludePhysics = false` leaves the physics out; a `Component` whose collider already holds a shape is kept as it is, in Bullet too now. |
| `Box2DBodyComponent.BodyType`, `Mass`, `Friction`, `Restitution`, `LinearDamping`, `AngularDamping`, `CollisionGroup`, `IsSensor` | Gone | Nothing read them; a body's type, density and friction are given to `Box2DSimulation` and `ShapeFixtureBuilder`. `BodyId` and the live velocity, force and impulse members stay. |
| `TextureCanvas.DrawRect()` | Gone | It ignored every parameter and drew a quad. |
| `TextureCanvas.Size = …`, `PixelFormat = …` | Resample the canvas | The setters assigned the field before the resample compared against it, so nothing ever happened. `Resample()` honours its `samplingPattern` and the relative `DrawTexture` overload its destination rectangle. |
| `SetMaxFPS(0)`, `SetFocusLostFPS(0)` threw | No cap | Zero or less removes the minimum update interval. |
| `AxialGizmoBase.Create()` returned `null` | `CreateMaterials()`, `void` | Protected; a derived gizmo calls it before building its entities. |
| `DebugTextDropdown.Draw()`, `Position` | Gone | The dropdown is lines for a `DebugOverlay` section, through `GetLines()`; the standalone path ignored the overlay's keys and nothing used it. |

### 🎉 New Features

- `Primitive3D` and `Primitive2D`, closed sets of shape records with factories; the mesh builder and every physics package switch over the same set, so a shape missed by one is a compile error, not a mesh that does not match its collider. The round shapes take a `tessellation`, which the enums could not express.
- `EntityExtensions.ColliderFor(Primitive3D)` and `ColliderFor(Primitive2D, depth)` in the Bepu and Bullet packages: the collider fitted to a shape on its own, for a body built by hand.
- `SceneSetup3D` and `SceneSetup2D`: the compositor, camera, lighting, sky, ground, physics engine, post effects, game settings, overlay and profiler of a code-only scene as one object with defaults, applied by `game.Run(setup, start:, update:)` before `start`; every piece a property, `null` to leave one out. `CameraSetup` (`Perspective(position, rotation).WithController(helpPosition)`, `Orthographic(position, rotation, orthographicSize)`), `LightingSetup`, `GroundSetup` and `SkyboxSetupBase` are the pieces; `SceneSetup.None` applies nothing.
- `GradientSkyboxSetup`, the core's sky without a texture: a small cubemap built from zenith, horizon and nadir colours and lit like a photographed sky, so a scene has a sky and image-based light with the core package alone. `SkyboxGenerator` lives in the core with it.
- `PhysicsSetupBase`, the physics engine of a setup: `UseBepu()` and `UseBullet()` set `BepuPhysicsSetup` or `BulletPhysicsSetup`, and the core's `CreatePrimitive`, `Add3DGround` and `Add2DGround` attach that engine's bodies and colliders. `IncludePhysics` on the primitive options opts one entity out; `PhysicsSetupBase.Of(game)` reads the engine back. Both packages' namespaces can be imported together.
- `Skies.Cloudy` and `UseSkybox()` in the Skyboxes package choose the photographed sky for a setup.
- `entity.AddTo(scene)` and `entity.AddTo(parent)` return the entity, so creation, placement and adding read as one line.
- `ScreenAnchor`: the overlay, anchored `EntityTextComponent` text and `ShapeBatch.Anchor()` can sit on the middle of an edge or the centre of the screen, not only in a corner; `ScreenAnchor.At(size)` gives the point for any rectangle.
- `game.AddPhysicsDebug()` in the Bepu package: one entity with the collidable gizmos (P) and the physics debug meshes (F11), both of which act on the whole scene, so a second copy of either script would only toggle the first one back.
- `Mass` on `Bepu3DPhysicsOptions`, `Bepu2DPhysicsOptions`, `Bullet3DPhysicsOptions` and `Bullet2DPhysicsOptions`: the mass of the fitted collider shape (Bepu) or the rigid body (Bullet), `null` for the engine's default of 1, so a heavier sphere no longer needs a hand-built compound.

### 📄 Docs

- Create Project, Create Project - Engine Only, Game Extensions, Physics Extensions, Bepu transform ownership, Entity Text, Engine patterns, the Glossary and the ShapeBatch HUD tutorial use `CreatePrimitive` and the shape factories; the example pages are regenerated.
- Scene Setup, a new manual page under Code-Only: `Run`, every property of `SceneSetup3D` and `SceneSetup2D` with its default and meaning, the pieces, the engine configurations `GameSettings` can carry, and the one-line changes. Code-Only Extensions, Game Extensions, Physics Extensions, Camera Controllers, Create Project, Create Project - Engine Only, the file-based app page, the FAQ, GPU Picking, the Pick and Aim and ShapeBatch HUD tutorials describe the scene setup instead of `SetupBase*`; the example pages are regenerated.
- Create Project: the command-line steps are Windows and Linux tabs. The Linux tab adds the `linux-x64` runtime identifier, `Stride.CommunityToolkit.Linux` as its own step for a distribution's `dotnet` package, the first-build workaround for the asset compiler's signature check (`DOTNET_NUGET_SIGNATURE_VERIFICATION=false`), and the route that builds for Linux from Windows with `StridePlatform`.

### 🎓 Examples

- `E02_2D_Material`, the 2D twin of `E02_3D_Material`: what each material helper does on a flat shape, with a checkerboard built in code.
- The E02 cluster lined up with E01: a twin directly after its 3D example with matching titles (`3D Primitives` / `2D Primitives`, `3D Material` / `2D Material`), the script examples ordered lean to full (`E02_3D_SyncScript`, `E02_3D_GiveMeACube`, then `_SimulationUpdate`), both primitives examples in the five colours of the first examples, and `E02_3D_Primitives` and `E08_2D_DebugRender` on `AddPhysicsDebug()`, so P and F11 do what the overlay says for every shape; `E08_3D_CollidableGizmo` and `E08_3D_DebugRenderComponent` host their one script on an empty entity, as the helper does, since each acts on the whole scene.
- Every example uses the shape factories; a size that was a `Size` option is now the factory's argument, so `Primitive3D.Cylinder(0.3f, 2.5f)` reads as a radius and a height where `new Vector3(0.3f, 0, 2.5f)` did not.
- Every program, the F# and VB ones, the gold scenes and the tests run through a scene setup: `new SceneSetup3D().UseBepu().UseSkybox()` for the typical one, and `var sceneSetup = new SceneSetup3D { Camera = CameraSetup.Perspective(position, rotation).WithController(), Ground = …, Profiler = true }` where the program aims its camera or frames its scene, so `Start` holds the scene's own content only; `SceneSetup.None` for the tests.
- `E06_Box2D`: the keys spawn the factory's named models (`Square`, `Rectangle`, `Circle`, `LargeCircle`, `Triangle`, `Capsule`) instead of looking a kind of shape up; a square and a rectangle are the same kind.
- `E09_3D_PostEffects`: film grain at `Amount = 0.6f` and `GrainSize = 2f` (was 0.35 and 1), so the G key shows in a screenshot.

### 🔧 Engineering

- NuGet packages bumped. Two audit advisories on `System.Security.Cryptography.Pkcs` 8.0.1, reached through `Stride.AssetCompiler`, are suppressed in `Directory.Build.props` the way the ImageSharp ones are, until Stride moves off that version.