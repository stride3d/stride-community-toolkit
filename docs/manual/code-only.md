# Code Only

## Why would you use Code Only and not Stride Editor?
- You don't want to install anything on your computer (no Stride installation required)
- You want to start very quickly
- You want to have fun learning C# or game development
- You want to learn C# programming with a nice visual 2D/3D output instead of console
- You want to learn game programming gradually, in the simplest way, without using the game editor
- You find coding and coding tools very complex to understand and navigate around
- You want to start with game development basics before you even start exploring the game editor
- Easy and quick prototyping
- Easy to learn game development concepts and steps
- Performance and feature evaluation
- Any other reason? Suggest here [GitHub Issues](https://github.com/VaclavElias/stride-code-only/issues).

## Visual Studio Code Instructions

1. Create Console App https://docs.microsoft.com/en-us/dotnet/core/tutorials/with-visual-studio-code?pivots=dotnet-6-0
   - ```dotnet new console --framework net6.0```
2. Add package ```dotnet add package CodeCapital.Stride.GameDefaults --prerelease```
   - If you experience any issue (timestamping certificate) adding this package, try again (weird, isn't it?)
3. Paste the example code below in the Program.cs
4. Run ```dotnet run```
5. Enjoy Stride

## Visual Studio 2022 and Rider Instructions
 
1. Create C# Console Application (.NET 6)
2. Add NuGet package **CodeCapital.Stride.GameDefaults** prerelease
   - If you experience any issue (timestamping certificate) adding this package, try again (weird, isn't it?)
3. Paste the example code below in the Program.cs
4. Run
5. Enjoy Stride

## Example Code

```c#
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.GameDefaults.ProceduralModels;
using Stride.GameDefaults.Extensions;

using var game = new Game();

game.Run(start: Start);

void Start(Scene rootScene)
{
    game.SetupBase3DScene();

    var entity = game.CreatePrimitive(PrimitiveModelType.Capsule);

    entity.Transform.Position = new Vector3(0, 8, 0);

    entity.Scene = rootScene;
}
```

`CreatePrimitive()` creates Capsule with rigid body physics, and because we placed the capsule 8 above the ground `new Vector3(0, 8, 0)`, it will fall down, eventually starts rolling till it falls from the ground. Note that we should remove the capsule once it is not visible to release resources, otherwise it remains in the memory and CPU is used to calculate physics.

![image](https://user-images.githubusercontent.com/4528464/180097697-8352e30c-3750-42f1-aef9-ecd6c8e6255e.png)

## Functionality
Some functionality you would expect and which is working in the Stride Editor might not be possible yet. Please add your vote or submit another request in this repo Issues.

## Building Project Issues
1. Error - Could not load native library libcore using CPU architecture x64
   - Make sure you installed Visual C++ Redistributable
```
C:\Users\Vacla\.nuget\packages\stride.core.assets.compilerapp\4.1.0.1728\buildTransitive\Stride.Core.Assets.CompilerApp.targets(132,5): error MSB3073: The command ""C:\Users\Vacla\.nuget\packages\stride.core.assets.compilerapp\4.1.0.1728\buildTransitive\..\tools\net6.0-windows7.0\Stride.Core.Assets.CompilerApp.exe"  --disable-auto- 
compile --project-configuration "Debug" --platform=Windows --project-configuration=Debug --compile-property:StrideGraphicsApi=Direct3D11 --output-path="C:\Projects\StrideDemo\bin\Debug\net6.0\data" --build-path="C:\Projects\StrideDemo\obj\stride\assetbuild\data" --package-file="C:\Projects\StrideDemo\StrideDemo.csproj" --msbuild-up 
todatecheck-filebase="C:\Projects\StrideDemo\obj\Debug\net6.0\stride\assetcompiler-uptodatecheck"" exited with code -532462766. [C:\Projects\StrideDemo\StrideDemo.csproj]
```
2. Error - Package 'runtime.ubuntu.16.10-x64.runtime.native.System.Security.Cryptography.OpenSsl 4.3.0' from source .. : The repository primary signature's timestamping certificate is not trusted by the trust provider
   - Restore the package CodeCapital.Stride.GameDefaults again ```dotnet restore``` 

## References
- https://github.com/stride3d/stride/issues/1295
- https://github.com/stride3d/stride/discussions/1253
- Example games to do https://github.com/abagames/111-one-button-games-in-2021/blob/main/README.md



*Work in progress..*

This library is just a preview, expect breaking changes.

| Class Name | Description
| --- | --- |
| BasicCameraController | Stride.Assets.Presentation, Assets -> Scripts -> Camera|
| CameraComponentExtensions | Implements ```ScreenPointToRay()``` | |
| GameExtensions | See below |
| GameProfiler | Stride.Assets.Presentation, Assets -> Scripts -> Utility |
| GraphicsCompositorBuilder | |
| PrimitiveModelType |  |
| SkyboxGenerator        | |
| SkyboxGeneratorContext |  |

## CameraComponentExtensions.cs

| Extensions | Status | Note
| --- | --- | --- |
| ```ScreenPointToRay()``` |![Done](https://img.shields.io/badge/status-done-green?style=for-the-badge)| Returns near and far vector based on a ray going from camera through a screen point. |

## GameExtensions.cs

The methods which are done can be used but most likely would need some refactoring, because they are duplicating code from the engine. More research needs to be done if this could be moved to Stride engine.

Some extensions return ```Entity``` so it can be further modified.

| Extensions | Status | Note
| --- | --- | --- |
| ```Run()``` |![Done](https://img.shields.io/badge/status-done-green?style=for-the-badge)| Unitialising the game, use ```start``` and ```update``` params |
| ```SetupBase()```|![Done](https://img.shields.io/badge/status-done-green?style=for-the-badge)| Adds Graphics Compositor, Camera and Directional Light |
| ```SetupBase3DScene()``` |![Done](https://img.shields.io/badge/status-done-green?style=for-the-badge)| Same as ```SetupBase()``` plus SkyBox, Ground, MouseLookCamera |
| ```SetupBase2DScene()``` |![Research](https://img.shields.io/badge/status-research-blue?style=for-the-badge)| |
| ```AddGraphicsCompositor()```|![Done](https://img.shields.io/badge/status-done-green?style=for-the-badge) | Adds Graphic Compositor with Clean UI |
| ```AddCamera()```|![Done](https://img.shields.io/badge/status-done-green?style=for-the-badge) ||
| ```AddDirectionalLight()```|![Done](https://img.shields.io/badge/status-done-green?style=for-the-badge) ||
| ```AddSkybox()```|![Done](https://img.shields.io/badge/status-done-green?style=for-the-badge) | |
| ```AddMouseLookCamera()```|![Done](https://img.shields.io/badge/status-done-green?style=for-the-badge) | The camera entity can be moved using W, A, S, D, Q and E, arrow keys |
| ```NewDefaultMaterial()``` |![Done](https://img.shields.io/badge/status-done-green?style=for-the-badge) | Adds basic material |
| ```CreatePrimitive()``` |![Done](https://img.shields.io/badge/status-done-green?style=for-the-badge) | Simplifies primitives creation |
| ```AddProfiler()``` |![Done](https://img.shields.io/badge/status-done-green?style=for-the-badge) | Attaches profile |
| ```AddGizmo()``` |![Research](https://img.shields.io/badge/status-research-blue?style=for-the-badge)| Debug. To see X,Y,Z arrows|
| ```AddEntityNames()``` |![Research](https://img.shields.io/badge/status-research-blue?style=for-the-badge)| Debug. To see entity properties in the game|
| ```AddPhysicsDebugger()``` |![Research](https://img.shields.io/badge/status-research-blue?style=for-the-badge)| Debug. To see colliders. Tracked here [Issue #9](https://github.com/VaclavElias/stride-code-only/issues/9)|

## Other Requests & Features Tracking
| Title | Status | Note
| --- | --- | --- |
| [#8 Load Assets](https://github.com/VaclavElias/stride-code-only/issues/8) |![Research](https://img.shields.io/badge/status-research-blue?style=for-the-badge)|Import and update assets|
| [#2 Code Only + Editor](https://github.com/VaclavElias/stride-code-only/issues/2) |![Research](https://img.shields.io/badge/status-research-blue?style=for-the-badge)|Seamless workflow for both options|
| [#7 dotnet new template](https://github.com/VaclavElias/stride-code-only/issues/2) |![Research](https://img.shields.io/badge/status-research-blue?style=for-the-badge)|Can we use templates?|



