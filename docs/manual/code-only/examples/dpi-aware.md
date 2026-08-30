---
generated: true
slug: dpi-aware
---

# DPI-Aware Window

The capsule scene again, with one difference that is not in the C# at all: an app.manifest declaring
the process per-monitor DPI aware, referenced from the csproj. Without it Windows scales the window
itself on a high-DPI display and the result is a blurred, upscaled image. The example exists because
the fix is invisible in the source, so it is easy to conclude that Stride renders badly when the
real cause is a missing manifest.

The `Program.cs` file shows how to:

- Why a high-DPI display renders a blurred window without a manifest
- Declaring per-monitor DPI awareness in app.manifest
- Wiring the manifest in with <ApplicationManifest>
- Referencing Stride.CommunityToolkit.Windows for Windows-only concerns
- Using helpers: SetupBase3DScene, AddSkybox, Create3DPrimitive

![DPI-Aware Window](media/dpi-aware.webp)

View on [GitHub](https://github.com/stride3d/stride-community-toolkit/tree/main/examples/code-only/Example01_Basic3DScene_DPI_Aware).

[!code-csharp[](../../../../examples/code-only/Example01_Basic3DScene_DPI_Aware/Program.cs?start=1&end=23)]
