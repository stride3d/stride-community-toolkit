# Adding a Texture at Runtime

In this tutorial, we will explore how to dynamically apply a texture to a 3D object in your Stride project. By using the `Texture` class, you can load an image file at runtime and assign it to a 3D entity. We will demonstrate how to do this by applying a texture to a cube.

## What You'll Learn
- How to load a texture from an image file at runtime.
- How to create and configure a `MaterialDescriptor` to apply the texture to a 3D object.
- How to create a 3D primitive (cube) and assign the texture as its material.

## Code Walkthrough

[!code-csharp[Adding a Texture at Runtime](../../../examples/snippets/TextureMapping_Example01/Program.cs)]

## Running the Code

When you run this code, the game will display a 3D cube with the specified texture applied to its surface. The texture will be visible, and the cube will be placed 8 units above the ground.

## Summary

This example demonstrates how to dynamically load and apply a texture to a 3D object at runtime in Stride. The process involves:

- Loading a texture from an image file.
- Creating a material to define how the texture should be rendered.
- Applying the material to a 3D primitive and adding it to the scene.

This approach can be extended to various other 3D models and textures, allowing you to dynamically change the appearance of objects in your game.