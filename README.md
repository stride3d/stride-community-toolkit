![Stride](https://media.githubusercontent.com/media/stride3d/stride/master/sources/data/images/Logo/stride-logo-readme.png)

This repo contains some C# helpers and extensions to run [Stride](https://github.com/stride3d/stride) easily without the editor/Game Studio.

**Prerequisites**

1. Install Visual C++ Redistributable: https://download.visualstudio.microsoft.com/download/pr/cc0046d4-e7b4-45a1-bd46-b1c079191224/9c4042a4c2e6d1f661f4c58cf4d129e9/vc_redist.x64.exe
2. http://download.microsoft.com/download/2/E/6/2E61CFA4-993B-4DD4-91DA-3737CD5CD6E3/vcredist_x86.exe

**Steps**
1. Create a console application (.NET 6) in your editor of choice
2. Reference this project or NuGet package CodeCapital.Stride.GameDefaults (prerelease)
    - Note that Stride .NET 6 is not yet released, you need to build [Stride](https://github.com/stride3d/stride) master branch.
4. Paste the code below
5. Run

```c#
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.GameDefaults;
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
