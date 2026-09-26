# Shaders in a toolkit package

What to know before writing an `.sdsl` or `.sdfx` file for the toolkit: where it goes, what the
build does with it, what the engine already gives you, and the three habits that keep a shader
honest on every display and in every golden image. The compiler facts - the SPIR-V toolchain,
reserved words, where the generated HLSL lands - are in the
[repository instructions](https://github.com/stride3d/stride-community-toolkit/blob/main/.github/copilot-instructions.md#shaders-sdsl); this page is
the recipe.

## Where a shader lives

The core package has no shaders, by decision, and stays that way. A shader makes a package carry
an `Effects` pack item, an `.sdpkg` manifest and a build-time dependency on the asset compiler,
and none of that belongs in the package everything else depends on. So a shader goes into the
package that owns the feature - `Shapes` for the shape batch, `DebugShapes`, the two ImGui
packages - or into `Stride.CommunityToolkit.Effects`, the home for shader-backed pieces that have
no package of their own: the post-processing colour transforms and GPU picking today, more
later, until the maintainers settle a longer-term layout.

Inside the package the layout is fixed:

```text
Stride.CommunityToolkit.Effects/
  Effects/                     <- every .sdsl and .sdfx, flat; the folder name is the contract
    NightVisionShader.sdsl
    GpuPickingEffect.sdfx
  PostProcessing/NightVision.cs
  Picking/GpuPicker.cs
  Stride.CommunityToolkit.Effects.csproj
```

The folder must be `Effects/` at the project root: the asset compiler's targets stage exactly
that folder into the package as `stride/Assets/Effects` and write the `.sdpkg` manifest for it.
Do not hand-write either. The project file needs one line, and a comment saying why it is there:

```xml
<PackageReference Include="Stride.AssetCompiler" Version="$(StrideVersion)" IncludeAssets="build;buildTransitive" />
```

Key classes (`NightVisionShaderKeys`, `GpuPickingShaderKeys`) are generated at build time by a
Roslyn source generator for every shader in the project. There is no file on disk and nothing to
regenerate. Declare the shader `internal shader X : ...` and the keys class is internal too,
which is what you want unless a caller outside the package sets its parameters; a public shader
with an undocumented keys class fails the XML-comment warning on every build.

## What the engine already gives you

Stride ships function libraries as shaders you call *qualified*, without inheriting them, so a
mixin stays small and its parameter block stays yours:

| Library | What is in it | Call it as |
|---|---|---|
| `Math` | `PI`, `Luminance`, `Hermine` and `Quintic` smoothsteps, `FastRandom`, ray-plane and ray-sphere tests, `ExpDecay` | `Math.Luminance(rgb)` |
| `ColorUtility` | `ToLinear`, `ToSRgb` (the cubic fits the engine uses for sprites and UI), colour packing | `ColorUtility.ToLinear(c)` |
| `HSVUtils` | `GetHue`, `GetSaturation`, `GetValue`, RGB to HSV and back | `HSVUtils.GetValue(rgb)` |
| `BlendUtils` | The Photoshop blend modes: multiply, screen, overlay, soft light and the rest | `BlendUtils.Overlay(a, b)` |
| `NormalPack` | Normal packing and unpacking for G-buffers | `NormalPack.Unpack(n)` |
| `Texturing` | The full-screen quad's `TexCoord` stream and the default samplers | inherit it |
| `ColorTransformShader` | The base of a post-processing colour transform: override `Compute(float4 color)` | inherit it |

`ShapeDistance : Math` in the Shapes package and `NightVisionShader : ColorTransformShader, Texturing`
in Effects are the two patterns. Inherit for streams and overrides; qualify for functions.

## Three habits

**Decode colours for the target you draw on.** The back buffer is sRGB, so a colour that arrives
as bytes must be decoded to linear before blending and the hardware encodes it on the way out.
Skip the decode and the picture is washed out; decode with `pow(c, 2.2)` instead of
`ColorUtility.ToLinear` and the darks crush. Every toolkit shader that takes vertex colours does
this the same way; the ShapeBatch page has the story of getting it wrong twice.

**Dither any wide, soft gradient.** Eight bits band. Half a code of ordered noise, fixed to the
pixel grid so it never crawls, breaks the bands into something the eye does not see. The
ShapeBatch colour mixin carries the version below; copy it rather than sharing it, because a
shared mixin would drag a package dependency in for twenty lines.

```hlsl
// One 4x4 Bayer cell per four pixels either way, returned in -0.5..0.5
float Bayer(float2 pixel)
{
    int2 cell = int2(pixel) & 3;
    int index = cell.x + cell.y * 4;

    float threshold =
        index == 0 ? 0.0 : index == 1 ? 8.0 : index == 2 ? 2.0 : index == 3 ? 10.0 :
        index == 4 ? 12.0 : index == 5 ? 4.0 : index == 6 ? 14.0 : index == 7 ? 6.0 :
        index == 8 ? 3.0 : index == 9 ? 11.0 : index == 10 ? 1.0 : index == 11 ? 9.0 :
        index == 12 ? 15.0 : index == 13 ? 7.0 : index == 14 ? 13.0 : 5.0;

    return (threshold + 0.5) / 16.0 - 0.5;
}

// Half a code in the units of the target: an sRGB target's code is wider in linear terms the
// brighter the value (the curve's slope, about 2.2 times the value), never narrower than the toe
float3 Dither(float3 rgb, float noise, bool linearTarget)
{
    float3 code = linearTarget ? max(rgb * 2.2, 1.0 / 12.92) / 255.0 : 1.0 / 255.0;

    return rgb + noise * code;
}

// In PSMain: streams.ColorTarget.rgb = Dither(rgb, Bayer(streams.ShadingPosition.xy), true);
```

**Wrap the draw in a timing scope.** Two lines put the feature's GPU time under its own name in
the profiler overlay's GPU section and as a marker in a frame capture. Without them the cost is
invisible, folded into whatever stage the engine attributes it to. Every custom renderer in the
toolkit does this; the key is public so a game can find the block by name.

```csharp
public static readonly ProfilingKey ProfilingKey = new("ShapeBatch");
private static readonly Color4 ProfileColor = new(1f, 0.6f, 0.1f, 1f);

// in Draw or DrawCore
using var _ = context.QueryManager.BeginProfile(ProfileColor, ProfilingKey);
```

## Effects and the generated mixins class

A `.sdsl` is a shader; a `.sdfx` is an *effect*, the recipe that picks and composes shaders from
parameters at run time. The Shapes package uses one to swap in a textured fill only when a fill
source is set; the Effects package uses one to give the engine's forward shading effect a child
that replaces the pixel stage for picking:

```hlsl
effect GpuPickingEffect
{
    mixin StrideForwardShadingEffect;
    mixin child GpuPickingIds;
};

effect GpuPickingIds
{
    mixin GpuPickingShader;
};
```

The source generator turns each `.sdfx` into a registered mixin builder inside a static class
named `ShaderMixins`, one per file namespace. Two packages with effect files therefore have two
types of that name, which NDepend's same-name rule flags; the suppression in
`GlobalSuppression.cs` names the generated class and says it is the generator's. Add one for your
package if it ships an effect file.

## Render features and the phase model

A root render feature uploads in `Prepare`, draws once *per view* in `Draw`, and releases in
`Flush`. A feature that empties its data in `Draw` is drained by the first view and draws nothing
in the second - which is why the shape batch's mirror panel and render-texture feeds work: the
batch is emptied in `Flush`, after the last view. DebugShapes follows the same shape. A scene
renderer (`SceneRendererBase`) draws once and the model does not apply; the text renderers and
the picking renderer are those.

## Proving it

A shader change that should change nothing is proven by the golden images: run
`dotnet run --file build/gold-images.cs` before and after and every scene stays bit-identical. A
deliberate visual change is reviewed on the contact sheet and then re-baselined with `--update`,
and the release notes say which goldens moved and why. A new shader gets a gold scene of its own
in `tests/Stride.CommunityToolkit.GoldScenes`, small and named for what it pins.

When a picture is wrong and the reason is not obvious, do not reason from the C#: read a pixel
back with `GetData`, or dump the whole target to an image, and look. The picking pass's instancing
bug, where every instance was drawn through its inverse matrix, was found in one dump after an
hour of theories; the story is on the [GPU picking](../../manual/rendering/gpu-picking.md) page.