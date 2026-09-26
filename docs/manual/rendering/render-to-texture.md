# Render to texture - a second camera as a picture

A rear-view mirror. A security monitor. A minimap. A picture-in-picture scope. All of them are the
same thing: a camera that draws into a texture instead of the window, and a texture that is then
just a texture - the emissive map of a monitor's material, the fill of a shape, a sprite. The
engine has every piece of this and no single call for it, so the toolkit adds the call:

```csharp
var feed = game.AddRenderTextureCamera(cameraEntity, width: 512, height: 512);

// feed.Texture on anything that takes a texture; feed.Entity is the camera, aim it like any other
```

`E09_3D_RenderToTexture` is the example: five cameras watching one scene, each on a monitor, and a
big screen showing whichever one is selected. The shape gallery's last station is the same call
with the texture filled into a glowing panel.

## What the call builds

Four pieces, all of them the engine's:

1. **A texture** that is both a render target and a shader resource.
2. **A camera slot** of its own in the compositor, with the entity's `CameraComponent` bound to
   it. Slot zero is the game's camera, and two cameras on one slot is a known trap. The camera's
   aspect ratio is set to the texture's, or the picture is squashed to the window's shape.
3. **A renderer branch** appended to the compositor's top-level collection: a
   `SceneCameraRenderer` for the slot, wrapping a `RenderTextureSceneRenderer` for the texture,
   wrapping a **second** `ForwardRenderer` over the compositor's existing opaque and transparent
   stages. This is the structure of the engine's own `TestSharedStageMultipleOutputs`. Sharing the
   stages is what makes the feed draw the same meshes, particles and shapes as the main view.
4. **The handle** you get back, `RenderTextureCamera`, holding the texture, the entity, the camera,
   the slot, the renderer and the forward renderer. Dispose it and the feed is gone from the
   compositor and the texture released.

The feed draws after the main view, so the texture holds the previous frame: one frame of lag
nobody can see, and the price of the main view being able to show it.

## Two things that are not obvious

**The texture is HDR.** `PixelFormat.R16G16B16A16_Float` is the default, and it should stay so
unless the feed tone-maps itself. The scene is lit in HDR and a feed has no tone map of its own, so
an 8-bit texture saturates to white wherever the light is strong - which in a lit scene is almost
everywhere. Left in HDR, the texture is tone-mapped by the main view along with everything else the
main view draws, so a monitor comes out at the same brightness as the scene beside it.

**It has to be a second forward renderer.** The obvious shortcut, hanging the compositor's own
`SingleView` under the feed as well, draws fine and then throws at shutdown with
`AddReference/Release pair must match`: the compositor reference-counts its renderer and cannot
have it in two places. A second `ForwardRenderer` over the same stages costs nothing worth
measuring.

## A look of the feed's own

Each feed has its own forward renderer, so each can carry its own post-effects chain, and Stride's
colour-transform group takes any shader that inherits `ColorTransformShader`. The call takes a
callback for it:

```csharp
var thermal = game.AddRenderTextureCamera(cameraEntity, 512, 512,
    postEffects: fx => fx.ColorTransforms.Transforms.Add(new Thermal()));
```

The callback receives a `PostProcessingEffects` with every effect switched off, the way the
toolkit's default compositor has it; enable what the feed should have, or add transforms. Adding a
transform enables the group.

`Stride.CommunityToolkit.Effects` ships two colour transforms in its `PostProcessing` namespace, and they are as usable on the main
camera as on a feed:

- **`NightVision`**: luminance amplified into one colour, with bright sources blooming to white,
  grain that moves, faint scanlines and a vignette. `Gain`, `Tint`, `NoiseAmount` and
  `VignetteRadius` tune it.
- **`Thermal`**: luminance painted onto a false-colour ramp, dark blue through violet and red to
  orange, yellow and white. `Exposure` decides how much of the scene reads as hot.

Both run on HDR colour before any tone map, so an emissive object really is the hottest and the
brightest thing in the picture. Each is a class of a few lines and a shader of a few more, in the
package's `Effects` folder; a third one is written the same way.

## What it is not

- **Not a wireframe or a depth view.** Those need meshes drawn with a different rasterizer state
  or a post effect that reads the depth buffer, which is a render stage of its own rather than a
  colour transform. Both are in the backlog as feeds for later.
- **Not free.** Every feed is another pass over the scene. Five 512-pixel feeds in the example are
  cheap; five full-resolution ones are five more frames a frame.
- **Not the same frame.** The previous one, as above. A feed that must show this frame would have
  to be inserted before the main view rather than appended, at the cost of a blank first frame.

A colour transform of your own is a small shader in a package; the recipe is [Shaders in a toolkit package](../../contributing/toolkit/shaders.md).