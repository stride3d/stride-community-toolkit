# Code Only Examples

You can either copy and paste the code snippets from the examples below into your own project or run them directly using our console app with an interactive menu.

The console application providing this interactive menu is part of the `Stride.CommunityToolkit.Examples` project.


```plaintext
Stride Community Toolkit Examples

[1] Basic Example - Capsule with rigid body
[2] Basic Example - Give me a cube
[3] Basic Example - Capsule with rigid body and window
[Q] Quit
```

## Basic Example - Capsule with rigid body

This example shows the basic steps to create a 3D capsule entity in Stride. The capsule entity comes automatically equipped with a rigid body and a collider, thanks to the `CreatePrimitive()` method.

[!code-csharp[](../../../examples/code-only/Example01_Basic3DScene/Program.cs)]

## Basic Example - Give me a cube

This example demonstrates the essential steps to create a 3D cube in Stride. Just like the previous example, the cube entity comes automatically equipped with a rigid body and a collider, thanks to the `CreatePrimitive()` method. The cube is positioned at `(1f, 0.5f, 3f)` in the 3D world space. This example is perfect for those who are new to 3D game development with Stride.

[!code-csharp[](../../../examples/code-only/Example02_GiveMeACube/Program.cs)]

## Basic Example - Capsule with rigid body and window

This example expands upon the first one by adding a simple window interface. The window is crafted using a `UIComponent`, `UIPage`, and a `Canvas` containing a `TextBlock`.

In this example, we extend upon the previous demonstrations by introducing user interface elements into the scene. Specifically, we show you how to create a 3D capsule with a rigid body and a collider and display a simple "Hello, World" message in a UI window.

The AddCapsule function places a 3D capsule into the scene at a specified position, similar to previous examples. This time, we separate the logic into its own method for better code organization.

Then, we use the AddWindow function to introduce a basic UI window into the scene. This is done using a UIComponent attached to an Entity, along with a Canvas and TextBlock to display the text.

We also introduce SpriteFont, which is loaded into the _font variable to specify the text's font.

The entire example showcases the versatility of Stride, demonstrating how one can mix 3D elements and UI components in the same scene effortlessly.

[!code-csharp[](../../../examples/code-only/Example03_CapsuleAndWindow/Program.cs)]