# Scene Setup

A code-only game starts from nothing: no compositor, no camera, no light. The scene setup is that frame as data, one object with a default for every piece, which `Run` applies before `start` runs. `Start` then holds the scene's own content only.

```csharp
using var game = new Game();

game.Run(new SceneSetup3D().UseBepu().UseSkybox(), start: Start);

void Start(Scene scene)
{
    game.CreatePrimitive(Primitive3D.Capsule()).AddTo(scene).Transform.Position = new Vector3(0, 8, 0);
}
```

The pieces are applied in a fixed order: the compositor, the camera and its controller, the lights, the sky, the ground, the instancing render feature, the overlay and the profiler. A property changes one piece; `null` leaves it out. A physics package chooses the engine with `UseBepu()` or `UseBullet()`, the skyboxes package swaps in a photographed sky with `UseSkybox()`, and `UseInstancing()` from `Stride.CommunityToolkit.Rendering.Instancing` asks for instanced rendering.

## Run

| Call | What it does |
|---|---|
| `game.Run(setup, start: Start, update: Update)` | Applies `setup`, runs `start` once with the root scene, then `update` every frame. `start` may be `async`. |
| `game.Run(start: Start)` | The same with `SceneSetup3D.Default`. |
| `game.Run(SceneSetup.None, start: Start)` | Adds nothing to the scene: for a game that builds its compositor and scene by hand, and for headless tests. The physics engine, game settings, instancing, overlay and profiler still apply when set, so `SceneSetup.None.UseBepu()` is a hand-built scene whose primitives get Bepu bodies. |

`GameSettings` and `Physics` are applied before the engine starts, because the engine reads them while it initialises; everything else once the root scene exists. See [`Run`](xref:Stride.CommunityToolkit.Engine.GameExtensions.Run(Stride.Engine.Game,Stride.CommunityToolkit.Engine.SceneSetup,System.Action{Stride.Engine.Scene},System.Action{Stride.Engine.Scene,Stride.Games.GameTime},Stride.Games.GameContext)).

## SceneSetup3D

[`SceneSetup3D`](xref:Stride.CommunityToolkit.Engine.SceneSetup3D): a forward compositor with the toolkit's UI stage, a perspective camera with the camera controller, a directional light, a gradient sky and a ground plane.

| Property | Default | Meaning |
|---|---|---|
| `ClearColor` | `null` | The compositor's clear colour; `null` is the compositor's own cornflower blue, seen only where nothing is drawn. |
| `Msaa` | `MultisampleCount.None` | The forward renderer's multisampling. |
| `Camera` | `CameraSetup.Perspective().WithController()` | The camera on the compositor's first slot, at `CameraDefaults.Initial3DPosition` looking at the origin, with the fly controller. `null` for no camera. |
| `Lighting` | `LightingSetup.Directional()` | The lights. `null` for none. |
| `Skybox` | `new GradientSkyboxSetup()` | The sky and the image-based light it gives the scene. `null` for none. |
| `Ground` | `GroundSetup.Plane()` | A plane of `GameDefaults.Default3DGroundSize`, with a static collider when the setup has a physics engine. `null` for none. |
| `Physics` | `PhysicsSetupBase.None` | The physics engine `CreatePrimitive` and the grounds attach bodies with. Set through `UseBepu()` or `UseBullet()`. |
| `PostEffects` | `null` | Configures the compositor's post effects, which are otherwise all off but the tone map: `fx => fx.Bloom.Enabled = true`. |
| `GameSettings` | `null` | Configures the engine's `GameSettings` before the window is created: the graphics profile, the colour space, the window size. |
| `TextOverlay` | `false` | Creates the [text overlay](../rendering/text-overlay.md), so the first section added shows at once. |
| `Profiler` | `false` | Adds the frame-time profiler readout. |
| `Instancing` | `false` | Puts the instancing render feature in the compositor, so instanced masters draw from the first frame. Set through `UseInstancing()`. Without it a master draws as one model and the run loop warns. |

## SceneSetup2D

[`SceneSetup2D`](xref:Stride.CommunityToolkit.Engine.SceneSetup2D): a compositor with the UI stage, an orthographic camera looking down Z with the 2D camera controller, and a flat ground box below the origin. No lights and no sky, because 2D materials are flat, and no tone map, so colours stay as written.

| Property | Default | Meaning |
|---|---|---|
| `ClearColor` | `null` | As in 3D. |
| `Msaa` | `MultisampleCount.None` | As in 3D. Thin lines flicker without it; charts use `X4`. |
| `Camera` | `CameraSetup.Orthographic().WithController()` | At `CameraDefaults.Initial2DPosition`, `(0, 0, 50)`, with the pan-and-zoom controller. `null` for no camera. |
| `Ground` | `GroundSetup.Plane(…)` | A box `GameDefaults.Default2DGroundSize` wide and high at `GameDefaults.Default2DGroundPosition`, with a static collider when the setup has a physics engine. `null` for none. |
| `Physics`, `PostEffects`, `GameSettings`, `TextOverlay`, `Profiler`, `Instancing` | as in 3D | Asking for `PostEffects` installs the clean post-effect set, tone map included. |

## The pieces

### CameraSetup

[`CameraSetup`](xref:Stride.CommunityToolkit.Engine.CameraSetup) is built through a factory, then `WithController()`.

| Call | Meaning |
|---|---|
| `CameraSetup.Perspective(position, rotation)` | A perspective camera. Both arguments optional; the defaults are `CameraDefaults.Initial3DPosition` and `Initial3DRotation`. |
| `CameraSetup.Orthographic(position, rotation, orthographicSize)` | An orthographic camera; `orthographicSize` is the height of the view in world units, 10 by default. |
| `.WithController(helpPosition, showHelp)` | Adds the camera controller after the camera is aimed, so H resets to the aimed pose. `helpPosition` is the `ScreenAnchor` the 3D controller's key help goes to; `showHelp: false` registers none. |

The rotation is in degrees, packed as `X = Yaw`, `Y = Pitch`, `Z = Roll`: the convention `Add3DCamera` uses and the order the controller's F2 panel prints, so a view found by flying can be copied into the setup.

```csharp
Camera = CameraSetup.Perspective(position: new Vector3(0, 7, -21), rotation: new Vector3(180, -6, 0)).WithController()
```

### LightingSetup

[`LightingSetup`](xref:Stride.CommunityToolkit.Engine.LightingSetup), one of three rigs.

| Call | Meaning |
|---|---|
| `LightingSetup.Directional(intensity, shadows)` | One directional light from above and behind the camera, intensity 20, shadows on: what `AddDirectionalLight()` adds. |
| `LightingSetup.Studio(intensity, yawDegrees, shadows)` | The key, fill and rim lights of `AddStudioLighting()`. |
| `LightingSetup.AllDirections(intensity)` | Six lights, one down each world axis, no shadows: nothing is left dark. |

### GroundSetup

[`GroundSetup`](xref:Stride.CommunityToolkit.Engine.GroundSetup): a size and whether it collides.

| Call | Meaning |
|---|---|
| `GroundSetup.Plane(size)` | The width (X) and length (Z, or height in 2D) of the ground; the default size when omitted. With a physics engine on the setup it gets a static collider. |
| `GroundSetup.Visual(size)` | The same ground with no collider, whatever the setup's physics. |

### The sky

[`GradientSkyboxSetup`](xref:Stride.CommunityToolkit.Engine.GradientSkyboxSetup) is the core's sky without a texture: a small cubemap built from three colours and lit like a photographed sky. `Zenith`, `Horizon`, `Nadir` and `LightIntensity` are its properties:

```csharp
Skybox = new GradientSkyboxSetup { Zenith = new Color(40, 60, 120), Horizon = new Color(200, 180, 160) }
```

`UseSkybox()` from `Stride.CommunityToolkit.Skyboxes` replaces it with the bundled HDR photograph, `Skies.Cloudy`. Any class deriving from [`SkyboxSetupBase`](xref:Stride.CommunityToolkit.Engine.SkyboxSetupBase) can be a sky.

### The physics engine

`UseBepu()` and `UseBullet()` set [`PhysicsSetupBase`](xref:Stride.CommunityToolkit.Engine.PhysicsSetupBase) on the setup. From then on `CreatePrimitive` attaches that engine's body with the collider fitted to the shape, and `Add3DGround` and `Add2DGround` a static collider. `Body = BodyMode.None` on the primitive options leaves one entity without physics; the engine-named entity extensions, `AddBepuPhysics` and `AddBulletPhysics`, attach physics explicitly whatever the setup chose, so two engines can share a game. See [Physics Extensions](../physics-extensions/index.md).

### GameSettings

A Game Studio project carries a `GameSettings` asset; a code-only game has none, so the engine's subsystems run on their fallbacks. `GameSettings` on the setup builds the asset in code and registers it before the window is created, the way [`UseGameSettings()`](xref:Stride.CommunityToolkit.Engine.GameSettingsExtensions.UseGameSettings(Stride.Engine.Game,System.Action{Stride.Engine.Design.GameSettings})) does. Each subsystem reads its own configuration, added with `GetOrCreateConfiguration<T>()`; one that is not added keeps the engine's default.

| Configuration | Read by | Properties |
|---|---|---|
| `RenderingSettings` (`Stride.Graphics`) | The graphics device, at creation | `DefaultBackBufferWidth`, `DefaultBackBufferHeight`, `AdaptBackBufferToScreen`, `DefaultGraphicsProfile`, `ColorSpace`, `DisplayOrientation` |
| `AudioEngineSettings` (`Stride.Audio`) | The audio system | `HrtfSupport` |
| `PhysicsSettings` (`Stride.Physics`) | Bullet | `Gravity`, `FixedTimeStep`, `MaxSubSteps`, `MaxTickDuration` |
| `BepuConfiguration` (`Stride.BepuPhysics`) | Bepu | `BepuSimulations`, a list of `BepuSimulation`: `PoseGravity`, `FixedTimeStep`, `SolverSubStep`, `SolverIteration`, `MaxStepPerFrame`, `TimeScale`, `Deterministic`, `ParallelUpdate`, `ThreadCount`, `PoseLinearDamping`, `PoseAngularDamping`, `SoftStartDuration`, `SoftStartSubstepFactor`, `UsePerBodyAttributes` |
| `NavigationSettings` (`Stride.Navigation`) | The dynamic navigation mesh | `EnableDynamicNavigationMesh`, `IncludedCollisionGroups`, `BuildSettings` |
| `GameSettings` itself | The effect compiler | `CompilationMode`, `EffectCompilation`, `RecordUsedEffects` |

One lambda can set several:

```csharp
var sceneSetup = new SceneSetup3D
{
    GameSettings = settings =>
    {
        // A fixed window and feature level 11, for compute shaders and tessellation
        var rendering = settings.GetOrCreateConfiguration<RenderingSettings>();
        rendering.DefaultBackBufferWidth = 1600;
        rendering.DefaultBackBufferHeight = 900;
        rendering.DefaultGraphicsProfile = GraphicsProfile.Level_11_0;
        rendering.ColorSpace = ColorSpace.Linear;

        // Spatial audio
        settings.GetOrCreateConfiguration<AudioEngineSettings>().HrtfSupport = true;

        // Bepu: the first simulation is the default one; a second can be selected per body
        var bepu = settings.GetOrCreateConfiguration<BepuConfiguration>();
        bepu.BepuSimulations.Add(new BepuSimulation { PoseGravity = new Vector3(0, -10, 0), SolverSubStep = 8, SolverIteration = 1 });

        // Bullet, when UseBullet() is the engine
        var physics = settings.GetOrCreateConfiguration<PhysicsSettings>();
        physics.Gravity = new Vector3(0, -9.81f, 0);
        physics.FixedTimeStep = 1f / 120f;
        physics.MaxSubSteps = 2;
    }
}.UseBepu();
```

`SetWindowSize(width, height)` on the game is the short form of the two back-buffer lines. On Direct3D 11, `CompilationMode` makes no difference to the shaders: `Debug` and `Release` produce the same bytecode.

## Changing one thing

```csharp
new SceneSetup3D { Ground = null }                                        // no ground
new SceneSetup3D { ClearColor = new Color(16, 18, 28), Skybox = null }    // a dark background instead of a sky
new SceneSetup3D { Lighting = LightingSetup.Studio() }
new SceneSetup3D { Camera = CameraSetup.Perspective(position: new(0, 20, 0), rotation: new(0, -90, 0)) }   // aimed, no controller
new SceneSetup3D { PostEffects = fx => fx.Bloom.Enabled = true }
new SceneSetup3D { Msaa = MultisampleCount.X4 }
new SceneSetup3D { GameSettings = s => s.GetOrCreateConfiguration<RenderingSettings>().DefaultGraphicsProfile = GraphicsProfile.Level_11_0 }
new SceneSetup2D { ClearColor = Color.CornflowerBlue }.UseBepu()
```

When several pieces change, a variable reads better than a long argument:

```csharp
var sceneSetup = new SceneSetup3D
{
    Camera = CameraSetup.Perspective(position: new Vector3(-1, 8, -26), rotation: new Vector3(180, -12, 0)).WithController(),
    Ground = GroundSetup.Plane(new Vector2(40)),
    Profiler = true
}.UseBepu().UseSkybox();

game.Run(sceneSetup, start: Start, update: Update);
```

## A Game Studio project

The setup is a layer over the `Game` extension methods, never a precondition for them. In a Game Studio project the editor's scene already has its compositor, camera and lights, so a script calls `Game.CreatePrimitive(...)`, `Game.AddSkybox()` or any other helper directly, with no setup object. The helpers take `IGame`, which is what a script's `Game` property is; only `Run`, `SetWindowSize` and `UseGameSettings`, which act before the engine starts, need a `Game`.