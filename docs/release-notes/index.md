# Release Notes

Welcome to the Release Notes for the **Stride Community Toolkit**. This section aims to provide you with an organized, high-level summary of changes, enhancements, and fixes made in each version release. If you're looking to understand what has changed from one version to the next, you're in the right place.

## What to Expect

The Stride Community Toolkit is developed with rapid iteration in mind. It moves at a faster development pace compared to the Stride Game Engine. As a result, you should expect frequent updates that may introduce breaking changes. This fast-paced approach allows us to incorporate community feedback quickly and continue improving the toolkit.

## 1.0.0.0-preview.66

<!-- If needed add more categories -->

## What's Changed

### 💥 Breaking Changes

### 🎉 New Features

- `TextureLoader.FromPixels(device, pixels, width, height, options)`: makes a texture from pixels computed in code, prepared by role and with mipmaps, like a loaded file.

### 🐞 Bug Fixes

### 🎨 Rendering

### ✨ Enhancement

### 📄 Docs

- ShapeBatch manual: new section on using a shader class as a fill.

### 🎓 Examples

- `E02_3D_Material_Gallery`: the hair and subsurface scattering stations are always on and require a Stride build newer than 4.4.0-beta8. The `--engine-fix` switch is removed.
- `E11_3D_ShapeBatch_Gallery`: new station **Shader fill**, a fill computed by a shader class instead of a texture. The gallery picture has mipmaps.
- `E09_3D_Particles_Gallery`: new station **Shader node**, particles drawn by a shader class instead of a texture. `--station N --variation M` starts at a variation.

### 🔧 Engineering

- Removed the repository's `Directory.Build.targets`, which replaced Stride's `StrideSortItems` task to silence CS0162. Stride builds newer than 4.4.0-beta8 do not raise the warning.
- `Directory.Build.local.props`: an optional, git-ignored file that overrides build properties locally, such as `StrideVersion` for a Stride build from source. See the contributing build page.

### 💪 Other Changes