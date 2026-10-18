---
generated: true
slug: root-renderer-shader
---

# Root Renderer Shader

An animated ribbon background drawn by a custom RootRenderFeature, which is the deepest extension
point Stride offers short of writing your own compositor. Three pieces make it work and each has a
distinct job: a component holding the settings an author edits, a render object carrying just what
the shader needs, and the render feature that ties them to a stage. Intensity, frequency, amplitude,
speed and width are all live properties.

The `Program.cs` file shows how to:

- Writing a custom RootRenderFeature
- Splitting state across component, render object and render feature
- Registering a render feature with the graphics compositor
- Choosing the render stage and group a feature draws in
- Feeding component properties through to shader parameters

![Root Renderer Shader](media/root-renderer-shader.webp)

View on [GitHub](https://github.com/stride3d/stride-community-toolkit/tree/main/examples/code-only/E09_3D_RootRendererShader).

[!code-csharp[](../../../../examples/code-only/E09_3D_RootRendererShader/Program.cs?start=1&end=57)]