# TextureCanvas

@Stride.CommunityToolkit.Rendering.TextureCanvas builds and edits a texture at runtime: scaling,
image effects, and composing several textures into one.

```csharp
using var canvas = game.CreateTextureCanvas(new Size2(1024, 1024));
```

`CreateTextureCanvas` also extends `RenderContext` and `IServiceRegistry`, and the constructor
`new TextureCanvas(renderContext, size)` does the same. A `null` size is 1024 x 1024, and the
`pixelFormat` parameter defaults to `PixelFormat.R8G8B8A8_UNorm`.

> [!NOTE]
> Dispose the canvas. It holds up to two GPU textures.

## Load a source texture

Most operations start from a texture. `TextureLoader` loads a colour image as sRGB and premultiplied,
as the content pipeline would; `Texture.Load` with its defaults reads it as linear, and mid-tones
come out pale.

```csharp
var texture = new TextureLoader(game.GraphicsDevice).Color("input.png");
```

## Load into the canvas

`Load` reads a file path, a `Stream` or a `byte[]` straight into the canvas, and takes the image's
size and pixel format. `SetData` does the same from raw pixels.

```csharp
canvas.Load(Path.Combine(AppContext.BaseDirectory, "input.png"));
```

## Store

`Store` writes the canvas to a file path or a `Stream`; `GetData` returns the bytes.

```csharp
canvas.Store(Path.Combine(AppContext.BaseDirectory, "output.png"));
```

With a path, the file type comes from the extension (`.png`, `.jpg`, `.bmp`, `.gif`, `.tiff`, `.tga`,
`.dds` or `.stride`) unless you pass `fileType`. A `Stream` needs the file type.

`ToTexture` returns a copy of the canvas as a texture for a material or the UI.

```csharp
var output = canvas.ToTexture();
```

> [!NOTE]
> `Store`, `GetData` and `ToTexture` copy from the GPU to the CPU and stall the game. Do not call them
> every frame.

## Manipulation

### Resample

`Resample` changes the size or the pixel format and keeps the content, resized if necessary. Setting
`Size` or `PixelFormat` does the same for one of the two. The optional `samplingPattern` defaults to
`SamplingPattern.Linear`.

```csharp
canvas.Resample(size: new Size2(512, 512), pixelFormat: PixelFormat.R8G8B8A8_UNorm);
```

### BrightFilter

Applies a @Stride.Rendering.Images.BrightFilter. The values shown are the defaults; the optional
`colorMultiplier` tints the bright areas.

```csharp
canvas.BrightFilter(threshold: 0.2f, steepness: 1.0f);
```

### GaussianBlur

Applies a @Stride.Rendering.Images.GaussianBlur. The values shown are the defaults.

```csharp
canvas.GaussianBlur(radius: 4, sigmaRatio: 2.0f);
```

### CoCMapBlur

Applies a @Stride.Rendering.Images.CoCMapBlur. The default radius is 4.

```csharp
canvas.CoCMapBlur(radius: 4);
```

### Transform

Applies a @Stride.Rendering.Images.ColorTransformGroup. `preTransforms` and `postTransforms` are
optional.

```csharp
canvas.Transform(transforms: [new LuminanceToChannelTransform { ColorChannel = ColorChannel.R }]);
```

### Combine

Applies a @Stride.Rendering.Images.ColorCombiner. A `null` entry in `textures` is the canvas content.
The optional `colorMultipliers` tints each texture.

```csharp
canvas.Combine(
    textures: [sourceTexture1, sourceTexture2, null /* the canvas content */],
    factors: [0.2f, 0.2f, 0.6f]);
```

### Colorize

Multiplies the canvas by a colour.

```csharp
canvas.Colorize(colorMultiplier: new Color4(1f, 0, 0));
```

### Recolorize

Copies the red channel to all three channels, then multiplies by a colour. Without a colour the
result is greyscale.

```csharp
canvas.Recolorize(colorMultiplier: new Color4(1f, 0, 0));
```

### Apply

Applies any @Stride.Rendering.Images.ImageEffect.

```csharp
using var effect = new GaussianBlur();
canvas.Apply(effect);
```

## Drawing

`DrawTexture` copies part of a source texture to part of the canvas, which composes several textures
into one.

### Stretch

How the source rectangle is resampled into the target rectangle.

| `Stretch` | Result |
|---|---|
| `None` | The source keeps its size. Overflowing content is cropped |
| `Stretch` | The source fills the target. The aspect ratio is not preserved. The default |
| `Contain` | The source fits inside the target and keeps its aspect ratio; the rest of the target is left as it was |
| `Cover` | The source fills the target and keeps its aspect ratio; the overflow is cropped |

### Anchor

How the source is aligned inside the target when it is cropped or padded. The value names the point
of the source and the target that line up: `TopLeft` (the default), `TopCenter`, `TopRight`,
`MiddleLeft`, `MiddleCenter`, `MiddleRight`, `BottomLeft`, `BottomCenter` or `BottomRight`. With
`Stretch.Stretch` the anchor has no effect.

### Draw

The full overload takes a source and a destination rectangle: both `Rectangle` in pixels, or both
`RectangleF` as fractions of the texture and the canvas. Omit the source rectangle to draw the whole
texture, and both to fill the whole canvas. The values shown are the defaults.

```csharp
canvas.DrawTexture(
    texture,
    sourceRect,
    destinationRect,
    colorMultiplier: null,                     // Color4, multiplies the source; null is white
    stretch: Stretch.Stretch,
    anchor: Anchor.TopLeft,
    samplingPattern: SamplingPattern.Linear);
```

#### Examples

| Stretch | Anchor      | Source Rect           | Target Rect             | Result                       |
|---------|-------------|-----------------------|-------------------------|-----------------------------------|
| None    | TopLeft     | ![](media/source.png) | ![](media/target-v.png) | ![](media/none-top-left.png)      |
| None    | TopCenter   | ![](media/source.png) | ![](media/target-v.png) | ![](media/none-top-center.png)    |
| None    | TopRight    | ![](media/source.png) | ![](media/target-v.png) | ![](media/none-top-right.png)     |
| None    | MiddleLeft  | ![](media/source.png) | ![](media/target-v.png) | ![](media/none-center-left.png)   |
| None    | MiddleCenter | ![](media/source.png) | ![](media/target-v.png) | ![](media/none-center-center.png) |
| None    | MiddleRight | ![](media/source.png) | ![](media/target-v.png) | ![](media/none-center-right.png)  |
| None    | BottomLeft  | ![](media/source.png) | ![](media/target-v.png) | ![](media/none-bottom-left.png)   |
| None    | BottomCenter | ![](media/source.png) | ![](media/target-v.png) | ![](media/none-bottom-center.png) |
| None    | BottomRight | ![](media/source.png) | ![](media/target-v.png) | ![](media/none-bottom-right.png)  |
| Stretch | Any         | ![](media/source.png) | ![](media/target-v.png) | ![](media/stretch-any-v.png)      |
| Stretch | Any         | ![](media/source.png) | ![](media/target-h.png) | ![](media/stretch-any-h.png)      |
| Contain | Any left    | ![](media/source.png) | ![](media/target-h.png) | ![](media/contain-left.png)       |
| Contain | Any right   | ![](media/source.png) | ![](media/target-h.png) | ![](media/contain-right.png)      |
| Contain | Any top     | ![](media/source.png) | ![](media/target-v.png) | ![](media/contain-top.png)        |
| Contain | Any bottom  | ![](media/source.png) | ![](media/target-v.png) | ![](media/contain-bottom.png)     |
| Contain | Any centre  | ![](media/source.png) | ![](media/target.png)   | ![](media/contain-center.png)     |
| Cover   | Any left    | ![](media/source.png) | ![](media/target-v.png) | ![](media/cover-left.png)         |
| Cover   | Any right   | ![](media/source.png) | ![](media/target-v.png) | ![](media/cover-right.png)        |
| Cover   | Any top     | ![](media/source.png) | ![](media/target-h.png) | ![](media/cover-top.png)          |
| Cover   | Any bottom  | ![](media/source.png) | ![](media/target-h.png) | ![](media/cover-bottom.png)       |
| Cover   | Any centre  | ![](media/source.png) | ![](media/target.png)   | ![](media/cover-center.png)       |

View on [GitHub](https://github.com/stride3d/stride-community-toolkit/tree/main/examples/code-only/E09_2D_ImageProcessing).

[!code-csharp[](../../../examples/code-only/E09_2D_ImageProcessing/Program.cs)]

## See also

- [Image Processing (TextureCanvas)](../code-only/examples/image-processing.md), the example.
- [Render to texture](render-to-texture.md) for a camera that draws into a texture.
- [Materials](materials.md) for putting a texture on a material.