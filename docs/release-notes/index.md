# Release Notes

Welcome to the Release Notes for the **Stride Community Toolkit**. This section aims to provide you with an organized, high-level summary of changes, enhancements, and fixes made in each version release. If you're looking to understand what has changed from one version to the next, you're in the right place.

## What to Expect

The Stride Community Toolkit is developed with rapid iteration in mind. It moves at a faster development pace compared to the Stride Game Engine. As a result, you should expect frequent updates that may introduce breaking changes. This fast-paced approach allows us to incorporate community feedback quickly and continue improving the toolkit.

## 1.0.0.0-preview.67

## What's Changed

<!-- If needed add more categories -->

### 💥 Breaking Changes

- `Stride.CommunityToolkit` brings in `Stride.AssetCompiler` (build-only) and holds `WindowsDpiManager`, so a code-only project needs the core package, or any package that depends on it, and nothing else. `WindowsDpiManager` is in the `Stride.CommunityToolkit.Engine` namespace (was `Stride.CommunityToolkit.Windows`). `Stride.CommunityToolkit.Windows` is empty and only depends on the core; it ships once more so that a project still naming it keeps building, and will be unlisted. Reference `Stride.CommunityToolkit` instead. `Stride.CommunityToolkit.Linux` is unchanged.
### 🎉 New Features

### 🐞 Bug Fixes

### 🎨 Rendering

### ✨ Enhancement

### 📄 Docs

### 🎓 Examples

### 🔧 Engineering

### 💪 Other Changes