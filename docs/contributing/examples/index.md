# Contribute Examples

You can see all examples in the [examples](https://github.com/stride3d/stride-community-toolkit/tree/main/examples/code-only) folder.

If you would like your example be launchable from the console application, you can add it to the `Stride.CommunityToolkit.Examples` project which you can find here: [Stride.CommunityToolkit.Examples](https://github.com/stride3d/stride-community-toolkit/tree/main/src/Stride.CommunityToolkit.Examples).
  
1. Make sure you create an anchor class named `NamespaceAnchor.cs` if your example contains only `Programs.cs`, so the namespace can be accessible from `Stride.CommunityToolkit.Examples`.
    ```csharp
    namespace ExampleXY_YourExampleNamespace;
    
    /// <summary>
    /// This empty class is here to make the namespace available in the nameof() operator in the main examples project.
    /// </summary>
    internal static class NamespaceAnchor;
    ```
1. Add your example project to `Stride.CommunityToolkit.Examples` project.
1. Update `Providers/ExampleProvider.cs`
1. Run `Stride.CommunityToolkit.Examples`
1. You should see your example listed
```
Stride Community Toolkit Examples

[1] Basic Example - Capsule with rigid body
[2] Basic Example - Capsule with rigid body in F#
[3] Basic Example - Capsule with rigid body in Visual Basic
[4] Basic Example - Give me a cube
[5] Basic Example - Stride UI - Canvas - Capsule with rigid body and Window
[6] Basic Example - Stride UI - Grid - Save and load game state
[7] Basic Example - Procedural Geometry
[8] Basic Example - Particles
[9] Advance Example - Myra UI - Draggable Window, GetService()
[10] Advance Example - Image Processing
[11] Advance Example - Root Renderer Shader
[12] Other - CubeClicker
[13] Other - Debug Shapes
[14] Other - Renderer
[Q] Quit

Enter choice and press ENTER to continue
```
