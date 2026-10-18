# What's new in Stride Community Toolkit 1.0.0.0-preview.39

This article lists the most significant changes in Stride Community Toolkit preview `1.0.0.0-preview.39`.

This update mainly focused on this issue https://github.com/stride3d/stride-community-toolkit/issues/133.

I was able to debug the error but I wasn't able to find the issue itself because it happens in the NuGet library itself. I moved on and these are the suggestions for the toolkit for now:

Anywhere we reference `Stride.Core.Assets.CompilerApp` we have to add also `<RuntimeIdentifier>win-x64</RuntimeIdentifier>` which makes it Windows only. 

- `Stride.CommunityToolkit` library will have no `Stride.Core.Assets.CompilerApp` references, so it can be built correctly and be platform agnostic. Should work in code-only and regular Stride Projects.
- The new `Stride.CommunityToolkit.Windows` library will have `Stride.Core.Assets.CompilerApp` reference and also `<RuntimeIdentifier>win-x64</RuntimeIdentifier>`, at the moment it is an empty library referencing `Stride.CommunityToolkit` project mainly for code-only purpose, just to remove the above boilerplate. That means, code only examples would reference `Stride.CommunityToolkit.Windows` instead of `Stride.CommunityToolkit`. This could be done also for Linux later.
- `Stride.CommunityToolkit.Skyboxes` library mainly for code-only purpose, will have `Stride.Core.Assets.CompilerApp` reference removed so the NuGet package can be created. Because it is used with `Stride.CommunityToolkit.Windows`, it should build correctly

<!-- Release notes generated using configuration in .github/release.yml at main -->

## What's Changed
### 🔁 Build & Deploy
* Update 15 - Cross platform improvements by @VaclavElias in https://github.com/stride3d/stride-community-toolkit/pull/134


**Full Changelog**: https://github.com/stride3d/stride-community-toolkit/compare/1.0.0.0-preview.38...1.0.0.0-preview.39