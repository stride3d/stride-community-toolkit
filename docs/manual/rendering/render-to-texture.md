# Render to texture

`AddRenderTextureCamera` makes an entity's camera draw into a texture instead of the window: a rear-view
mirror, a security monitor, a minimap. Use the texture anywhere a texture fits: the emissive map of a
monitor's material, the fill of a shape, a sprite. The method is in the
`Stride.CommunityToolkit.Rendering.Compositing` namespace.

```csharp
var feed = game.AddRenderTextureCamera(cameraEntity, width: 512, height: 512);

// feed.Texture on anything that takes a texture; feed.Entity is the camera, aim it like any other
```

`E09_3D_RenderToTexture` draws five feeds, each on a monitor, and a big screen that shows the selected
one. <kbd>1</kbd> to <kbd>5</kbd> select a feed and <kbd>Space</kbd> steps through them. The ShapeBatch
gallery (`E11_3D_ShapeBatch_Gallery`, station "Second camera in a panel") fills a shape with a feed's
texture.

## What the call builds

1. **A texture** that is both a render target and a shader resource.
2. **A camera slot** of its own in the compositor, with the entity's `CameraComponent` bound to it.
   The entity gets a `CameraComponent` if it has none. The camera's aspect ratio is set to the
   texture's.
3. **A renderer branch** appended to the compositor's top-level collection: a `SceneCameraRenderer`
   for the slot, wrapping a `RenderTextureSceneRenderer` for the texture, wrapping a second
   `ForwardRenderer` over the compositor's existing opaque and transparent stages. Sharing the stages
   makes the feed draw the same meshes, particles and shapes as the main view.
4. **The handle**, `RenderTextureCamera`: `Texture`, `Entity`, `Camera`, `Slot`, `Renderer` and
   `Forward`. Dispose it to remove the feed from the compositor and release the texture.

The feed draws after the main view, so the texture holds the previous frame.

| Parameter | Default | Description |
|---|---|---|
| `width`, `height` | | The texture size in pixels |
| `format` | `PixelFormat.R16G16B16A16_Float` | The texture format. Keep it HDR unless the feed tone-maps itself: the scene is lit in HDR, so an 8-bit texture saturates to white. An HDR texture is tone-mapped by the main view with everything else it draws |
| `postEffects` | `null` | A callback that configures the feed's post effects. See [Post effects on a feed](#post-effects-on-a-feed) |

> [!NOTE]
> The feed needs a second `ForwardRenderer`. The compositor's own renderer is reference-counted and
> throws `AddReference/Release pair must match` at shutdown if it is placed under the feed as well.

## Post effects on a feed

Each feed has its own forward renderer, so it can have its own post-effects chain.

```csharp
var thermal = game.AddRenderTextureCamera(cameraEntity, 512, 512,
    postEffects: fx => fx.ColorTransforms.Transforms.Add(new Thermal()));
```

The callback receives a `PostProcessingEffects` with every effect switched off. Enable what the feed
needs, or add colour transforms. Adding a transform enables the colour-transform group.

`Stride.CommunityToolkit.Effects` ships two colour transforms in the
`Stride.CommunityToolkit.Effects.PostProcessing` namespace. They also work on the main camera.

| Transform | Effect | Properties |
|---|---|---|
| `NightVision` | Amplified luminance in one colour, bright sources bloom to white, moving grain, scanlines and a vignette | `Gain` (4), `Tint` (0.2, 1, 0.35), `NoiseAmount` (0.08), `VignetteRadius` (0.55) |
| `Thermal` | Luminance on a false-colour ramp from dark blue through violet, red, orange and yellow to white | `Exposure` (2.5): how much of the scene reads as hot |

Both run on HDR colour before the tone map, so an emissive object is the brightest thing in the
picture. To write a colour transform of your own, see
[Shaders in a toolkit package](../../contributing/toolkit/shaders.md).

## Limitations

- The compositor must exist and have render stages named `Opaque` and `Transparent`; otherwise the
  call throws `InvalidOperationException`.
- A colour transform cannot produce a wireframe or a depth view. Those need a rasterizer state or a
  post effect that reads the depth buffer.
- Every feed is another pass over the scene. Five 512-pixel feeds are cheap; five full-resolution
  feeds are five extra frames per frame.
- The texture shows the previous frame. A feed that must show the current frame has to be inserted
  before the main view rather than appended.
- The GPU picker reads the main camera only. See [GPU picking](gpu-picking.md).

## See also

- [E09_3D_RenderToTexture](../code-only/examples/render-to-texture.md), the example.
- [ShapeBatch](shape-batch.md) for filling a shape with a texture.
- [Shaders in a toolkit package](../../contributing/toolkit/shaders.md) for writing a colour transform.