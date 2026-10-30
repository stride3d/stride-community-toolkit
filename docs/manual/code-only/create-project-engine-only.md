# Create Project - Engine Only

The [Create Project](create-project.md) page builds a scene with the toolkit. This page builds the same
kind of scene - a ground, a cube and a camera - against Stride alone, with no toolkit package, so you
can see what `SceneSetup3D` and `CreatePrimitive()` stand for. It is not the recommended route.

> [!NOTE]
> The prerequisites in [Getting Started](../getting-started.md) still apply.

## Steps

1. Create a console app:
   ```
   dotnet new console --framework net10.0 --name YourProjectName
   cd YourProjectName
   ```
2. Add the engine, which is the runtime:
   ```
   dotnet add package Stride.Engine
   ```
3. Add the asset compiler, which is the build step that compiles the engine's shaders into your
   output. A code-only project has no assets of its own, but the engine's shaders still have to be
   compiled for the graphics API you run on, and without this the first frame fails with
   `Shader ShaderBase could not be found`:
   ```
   dotnet add package Stride.AssetCompiler
   ```
4. Make that reference build-only. Open the `.csproj` and add `IncludeAssets` to the line the previous
   step wrote, so it reads:
   ```xml
   <PackageReference Include="Stride.AssetCompiler" Version="4.4.1" IncludeAssets="build;buildTransitive" />
   ```
   Without it the package's own assemblies are copied next to your app and it fails at start-up with
   `Could not load file or assembly 'Stride.NuGetResolver'`. The `Stride.CommunityToolkit` package
   references the asset compiler build-only, which is why the [Create Project](create-project.md)
   steps never mention it.
5. Replace `Program.cs` with the code below.
6. Run it:
   ```
   dotnet run
   ```

## The code

This is the toolkit's `E01_3D_BasicScene_EngineOnly` example, verbatim.

[!code-csharp[](../../../examples/code-only/E01_3D_BasicScene_EngineOnly/Program.cs)]

## What each part replaces

| Engine code | Toolkit helper |
|---|---|
| `game.Script.AddTask(Start)` before `game.Run()` | `game.Run(start: Start)` - the toolkit's `Run` adds the task for you and hands `Start` the root scene. |
| `GraphicsCompositorHelper.CreateDefault(...)` assigned to `SceneSystem.GraphicsCompositor` | `AddGraphicsCompositor()`, which `SceneSetup3D` calls. Without a compositor nothing is drawn. |
| An entity with a `CameraComponent` bound to `compositor.Cameras[0]`, placed and rotated | `Add3DCamera()`, which `SceneSetup3D` calls with the same position and angles. |
| A `LightDirectional` with shadows and a `LightAmbient` fill | `AddDirectionalLight()`, which `SceneSetup3D` calls. The setup's sky stands in for the ambient light, which also lights the shaded faces. |
| `PlaneProceduralModel` / `CubeProceduralModel` → `Generate(...)` → `Model` → `ModelComponent` | `CreatePrimitive(Primitive3D.Cube())`. |
| `Material.New(device, new MaterialDescriptor { ... })` | `game.CreateMaterial(colour)`. |

What the engine-only version leaves out, because each is a page of its own without the toolkit:

- **Physics.** With `UseBepu()` on the setup, the ground gets a static collider and `CreatePrimitive()`
  gives the cube a rigid body, so it can fall and land. Here the cube is placed on the ground by hand
  and nothing moves.
- **A camera controller.** The toolkit's `Add3DCameraController()` flies the camera with the keyboard
  and mouse; here the camera is fixed where `Start` put it.
- **A sky.** The setup's gradient sky lights and surrounds the scene, and `UseSkybox()` swaps in a
  photograph; here a cornflower clear colour and an ambient light do the minimum.
- **DPI awareness.** `DpiAware = true` on the setup calls the toolkit's
  `WindowsDpiManager.EnablePerMonitorV2()`; without it the window is stretched on a scaled display.
  It is off by default, in the toolkit version too.

The [file-based app](create-file-based-app-engine-only.md) version of this page runs the same code
from a single file.