# Adding a Texture at Runtime

This tutorial shows how to apply a texture to a 3D object at runtime. Using the toolkit's `TextureLoader`, you'll load an image file as a colour texture and assign it to a 3D entity (a cube).

## What you'll learn
- Load a texture from an image file at runtime.
- Create and configure a `MaterialDescriptor` to apply the texture.
- Create a 3D primitive (cube) and assign the material.

## Code walkthrough

[!code-csharp[Adding a Texture at Runtime](../../../examples/snippets/TextureMapping_Example01/Program.cs)]

## Running the code

When you run this code, you'll see a 3D cube with the specified texture applied. The cube starts 8 units above the ground and falls onto it.

## Summary

- Load a texture from an image file.
- Create a material that uses the texture.
- Apply the material to a 3D primitive and add it to the scene.

You can apply the same approach to other models and textures to change object appearance at runtime.