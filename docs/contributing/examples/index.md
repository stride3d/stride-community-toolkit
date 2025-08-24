# Contribute Examples

You can see all examples in the [examples](https://github.com/stride3d/stride-community-toolkit/tree/main/examples/code-only) folder.

If you would like your example be launchable from the console application, you can add it to the `Stride.CommunityToolkit.Examples` project which you can find here: [Stride.CommunityToolkit.Examples](https://github.com/stride3d/stride-community-toolkit/tree/main/src/Stride.CommunityToolkit.Examples).
  
1. Create a project in the folder `examples/code-only/ExampleXY_YourExampleNamespace` (replace `XY` with the next available number).
1. Add your example meta data to the `*.csproj`, these will be used in the console application menu:
    ```xml
    <ExampleTitle>Basic Example - Capsule with rigid body</ExampleTitle>
    <ExampleOrder>100</ExampleOrder>
    <ExampleEnabled>true</ExampleEnabled>
    <ExampleCategory>1 - Basic Example</ExampleCategory>
    ```
1. If you enabled your example `<ExampleEnabled>true</ExampleEnabled>`, it will automatically show up in the console application menu.
1. Run `Stride.CommunityToolkit.Examples`
1. You should see your example listed in the console application.
   ```
    Stride Community Toolkit Examples

     [1] Basic Example - Capsule with rigid body
     [2] Basic Example - Capsule with rigid body - Bullet Physics
     [3] Basic Example - Capsule with rigid body - F#
     [4] Basic Example - Capsule with rigid body - Visual Basic
     [5] Basic Example - Mesh Line
     [6] Basic Example - Material
     [7] Basic Example - Give me a cube
     [8] Basic Example - Stride UI - Canvas - Capsule with rigid body and Window
     [9] Basic Example - Grid - Save and load game state
    [10] Basic Example - Procedural Geometry
    [11] Basic Example - Cylinder Mesh
    [12] Basic Example - Partial Torus
    [13] Basic Example - Partial Torus in F#
    [14] Basic Example - Particles
    [15] Basic Example - Raycast
    [16] Basic Example - CollisionGroup
    [17] Basic Example - CollisionLayer
    [18] Advance Example - Simple Constraint
    [19] Advance Example - Various Constraints
    [20] Advance Example - Myra UI - Draggable Window, GetService()
    [21] Advance Example - Stride UI - Draggable Window
    [22] Advance Example - Stride UI - Draggable Window - Bullet Physics
    [23] Advance Example - ImGui UI
    [24] Advance Example - Image Processing
    [25] Advance Example - Root Renderer Shader
    [26] Advance Example - Box2D.NET Physics
    [27] Advance Example - Mesh Outline
    [28] Other - Debug Shapes
    [29] Other - Debug Shapes Usage
    [30] Other - Renderer
    [31] Other - Blazor + SignalR - Run this from IIS
    [32] Other - Stride + SignalR - Website two way communication
    [33] Other - 2D Playground [WIP]
    [34] Other - Bepu Playground [WIP]
    [35] Other - Game - Cubicle Calamity [WIP]
    [36] Other - Game - Cubicle Calamity [WIP] - Bullet Physics
    [37] Basic Example - Basic 2D Scene [WIP]
     [Q] Quit

    Enter example id and press ENTER to run it.
    (Debug output may appear; you can ignore it and type another id at any time.)
    Choice:
   ```
1. Update the `Stride.CommunityToolkit.Docs/includes/manual/examples/basic-examples-outro.md` file with the new example.
1. Update the `Stride.CommunityToolkit.Docs/includes/manual/basic-examples.md` or `advance-examples.md` or `other-examples.md` file with the new example.