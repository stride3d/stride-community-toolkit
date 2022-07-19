![Stride](https://media.githubusercontent.com/media/stride3d/stride/master/sources/data/images/Logo/stride-logo-readme.png)

This repo contains C# helpers and extensions to run [Stride](https://github.com/stride3d/stride) easily without the Stride editor/Game Studio. The documentation and more fun examples will follow up. This repository is here to collect feedback before any major updates are done in the Stride engine itself. This NuGet is in preview, expect breaking changes.

## Content
- Prerequisites
- Visual Studio Code Instructions
- Visual Studio 2022 Instructions
- Example Code
- Why would you use Code Only and not Stride Editor?
- Functionality
- Issues

## Prerequisites

You must install the following, otherwise you won't be able to build/run the project. If you are using Stride **4.1**+ already, these should be already installed.

<!---
- https://download.visualstudio.microsoft.com/download/pr/0c1cfec3-e028-4996-8bb7-0c751ba41e32/1abed1573f36075bfdfc538a2af00d37/vc_redist.x86.exe
- https://download.visualstudio.microsoft.com/download/pr/cc0046d4-e7b4-45a1-bd46-b1c079191224/9c4042a4c2e6d1f661f4c58cf4d129e9/vc_redist.x64.exe
-->

1. Install Microsoft Visual C++ 2013 Redistributable
   - [vcredist_x86.exe](http://download.microsoft.com/download/2/E/6/2E61CFA4-993B-4DD4-91DA-3737CD5CD6E3/vcredist_x86.exe)
   - [vcredist_x64.exe](http://download.microsoft.com/download/2/E/6/2E61CFA4-993B-4DD4-91DA-3737CD5CD6E3/vcredist_x64.exe)
2. Install Microsoft Visual C++ 2015-2019 Redistributable
   - [vcredist_x64.exe](https://download.visualstudio.microsoft.com/download/pr/cc0046d4-e7b4-45a1-bd46-b1c079191224/9c4042a4c2e6d1f661f4c58cf4d129e9/vc_redist.x64.exe)
3. Install .NET 6 SDK x64 https://dotnet.microsoft.com/en-us/download
4. Install IDE of your choice
   - Visual Studio 2022
      - [Visual Studio 2022 Community](https://visualstudio.microsoft.com/vs/) is free
      - Make sure that **.NET desktop development** workload is selected when installing Visual Studio
   - [Visual Studio Code](https://code.visualstudio.com/) (free)
      -  Make sure you install also **C# for Visual Studio Code (powered by OmniSharp)** extension
      -  Restart Visual Studio Code otherwise ```dotnet``` command might not work
   - Rider (paid)

## Visual Studio Code Instructions

1. Create Console App https://docs.microsoft.com/en-us/dotnet/core/tutorials/with-visual-studio-code?pivots=dotnet-6-0
   - ```dotnet new console --framework net6.0```
2. Add package ```dotnet add package CodeCapital.Stride.GameDefaults --prerelease```
   - If you experience any issue (timestamping certificate) adding this package, try again (weird, isn't it?)
3. Paste the example code below in the Program.cs
4. Run ```dotnet run```
5. Enjoy Stride

## Visual Studio 2022 Instructions
 
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

## Functionality
Some functionality you would expect and which is working in the Stride Editor might not be possible yet. Please add your vote or submit another request in the Issues.

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
