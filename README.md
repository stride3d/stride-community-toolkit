![Stride](https://media.githubusercontent.com/media/stride3d/stride/master/sources/data/images/Logo/stride-logo-readme.png)

This repo contains some C# helpers and extensions to run [Stride](https://github.com/stride3d/stride) easily without the editor/Game Studio.

## Prerequisites

You must install (unless it is installed already) the following, otherwise you won't be able to build the proejct.

1. Install Visual C++ Redistributable
   - From [Microsoft Visual C++](https://docs.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist?view=msvc-170#visual-studio-2015-2017-2019-and-2022) page or direct link [vc_redist.x64.exe  ](https://aka.ms/vs/17/release/vc_redist.x64.exe)  
2. Install .NET 6 SDK https://dotnet.microsoft.com/en-us/download
3. Install editor of your choice (Visual Studio 2022, Visual Studio Code, Rider, ..)

## Visual Studio Code Instructions

1. Install Visual Studio Code
2. Create Console App https://docs.microsoft.com/en-us/dotnet/core/tutorials/with-visual-studio-code?pivots=dotnet-6-0
3. Add package ```dotnet add package CodeCapital.Stride.GameDefaults --prerelease```
4. Paste the code below
5. Run

## Visual Studio 2022 Instructions
1. Install Visual Studio 2022
   - [Community version](https://visualstudio.microsoft.com/vs/) is free
2. Create C# console application (.NET 6)
2. Add NuGet package CodeCapital.Stride.GameDefaults (prerelease)
4. Paste the code below
5. Run

```c#
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.GameDefaults.ProceduralModels;
using Stride.GameDefaults.Extensions;

using (var game = new Game())
{
    game.Run(start: Start);

    void Start(Scene rootScene)
    {
        game.SetupBase3DScene();

        var entity = game.CreatePrimitive(PrimitiveModelType.Capsule);
        
        entity.Transform.Position = new Vector3(0, 8, 0);
        entity.Scene = rootScene;
    }
}
```
## Why would you use Code Only and not Stride Editor?
- You don't want to install anything on your computer (no Stride installation required)
- You want to start very quickly
- You want to learn C# programming with a nice visual output instead of console
- You want to learn game programming gradually, in the simplest way, without using the game editor
- You find coding and coding tools very complex to understand and navigate around
- You want to start with game development basics before you even start exploring the game editor
- Easy and quick prototyping
- Easy to learn game development concepts and steps
- Performance and feature evaluation

## Issues
1. Error - Could not load native library libcore using CPU architecture x64
   - Make sure you installed Visual C++ Redistributable
```
C:\Users\Vacla\.nuget\packages\stride.core.assets.compilerapp\4.1.0.1728\buildTransitive\Stride.Core.Assets.CompilerApp.targets(132,5): error MSB3073: The command ""C:\Users\Vacla\.nuget\packages\stride.core.assets.compilerapp\4.1.0.1728\buildTransitive\..\tools\net6.0-windows7.0\Stride.Core.Assets.CompilerApp.exe"  --disable-auto- 
compile --project-configuration "Debug" --platform=Windows --project-configuration=Debug --compile-property:StrideGraphicsApi=Direct3D11 --output-path="C:\Projects\StrideDemo\bin\Debug\net6.0\data" --build-path="C:\Projects\StrideDemo\obj\stride\assetbuild\data" --package-file="C:\Projects\StrideDemo\StrideDemo.csproj" --msbuild-up 
todatecheck-filebase="C:\Projects\StrideDemo\obj\Debug\net6.0\stride\assetcompiler-uptodatecheck"" exited with code -532462766. [C:\Projects\StrideDemo\StrideDemo.csproj]
```
2. Error - Package 'runtime.ubuntu.16.10-x64.runtime.native.System.Security.Cryptography.OpenSsl 4.3.0' from source .. : The repository primary signature's timestamping certificate is not trusted by the trust provider
   - Restore the package CodeCapital.Stride.GameDefaults again ```dotnet restore``` 


References
- https://github.com/stride3d/stride/issues/1295
- https://github.com/stride3d/stride/discussions/1253
- Example games to do https://github.com/abagames/111-one-button-games-in-2021/blob/main/README.md
