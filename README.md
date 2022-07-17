![Stride](https://media.githubusercontent.com/media/stride3d/stride/master/sources/data/images/Logo/stride-logo-readme.png)

This repo contains some C# helpers and extensions to run [Stride](https://github.com/stride3d/stride) easily without the editor/Game Studio.

**Prerequisites**

You must install (unless it is installed already) the following, otherwise you won't be able to build the proejct.

1. Install Visual C++ Redistributable
   - From [Microsoft Visual C++](https://docs.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist?view=msvc-170#visual-studio-2015-2017-2019-and-2022) page or direct link [vc_redist.x64.exe  ](https://aka.ms/vs/17/release/vc_redist.x64.exe)  
2. Install .NET 6 SDK https://dotnet.microsoft.com/en-us/download
3. Install editor of your choice (Visual Studio 2022, Visual Studio Code, Rider, ..)

**Visual Studio Code Instructions**
1. Install Visual Studio Code
2. Create Console App https://docs.microsoft.com/en-us/dotnet/core/tutorials/with-visual-studio-code?pivots=dotnet-6-0
3. Add package dotnet add package ```CodeCapital.Stride.GameDefaults --prerelease```
4. Paste the code below
5. Run

**Visual Studio 2022 Instructions**
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

**Why would you use Code Only and not Stride Editor?**
- You don't want to install anything on your computer (no Stride installation required)
- You want to start very quickly
- You want to learn C# programming with a nice visual output instead of console
- You want to learn game programming gradually, in the simplest way, without using the game editor
- You find coding and coding tools very complex to understand and navigate around
- You want to start with game development basics before you even start exploring the game editor
- Easy and quick prototyping
- Easy to learn game development concepts and steps
- Performance and feature evaluation

References
- https://github.com/stride3d/stride/issues/1295
- https://github.com/stride3d/stride/discussions/1253
- Example games to do https://github.com/abagames/111-one-button-games-in-2021/blob/main/README.md
