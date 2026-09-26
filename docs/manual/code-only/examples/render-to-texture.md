---
generated: true
slug: render-to-texture
---

# Render to Texture

Five cameras watch one scene and each draws into a texture shown on a monitor: an overhead
map, a chase camera following an orbiting ball, a fixed CCTV corner, and two cameras with a
look of their own - night vision and a thermal camera, colour transforms from the toolkit's
Effects package. A big screen shows the selected feed. Each feed is one call,
AddRenderTextureCamera, which returns a texture that is just a texture: here the emissive map
of a monitor's material.

The `Program.cs` file shows how to:

- A second camera drawing into a texture with AddRenderTextureCamera, and what the call builds in the compositor
- Why the texture is HDR and where the tone map happens
- A texture on a plane through an emissive material, so a monitor is not lit again
- An orthographic map camera, a chase camera re-aimed every frame, and fixed cameras looking at a point
- Colour transforms of your own on a camera's post-effects chain - night vision and thermal
- Swapping a model's material at runtime to change what a screen shows

View on [GitHub](https://github.com/stride3d/stride-community-toolkit/tree/main/examples/code-only/E09_3D_RenderToTexture).

[!code-csharp[](../../../../examples/code-only/E09_3D_RenderToTexture/Program.cs?start=1&end=352)]