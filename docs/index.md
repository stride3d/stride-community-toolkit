---
_disableToc: false
---
# Stride Community Toolkit Documentation

[![Join the chat at https://discord.gg/f6aerfE](https://img.shields.io/discord/500285081265635328.svg?style=flat&logo=discord&label=discord&logoColor=f2f2f2)](https://discord.gg/f6aerfE)
[![License](https://img.shields.io/badge/license-MIT-blue)](https://github.com/stride3d/stride/blob/master/LICENSE.md)

[!INCLUDE [global-note](includes/global-note.md)]

## 👋 Introduction

The [Stride Community Toolkit](https://github.com/stride3d/stride-community-toolkit) is a set of C# helpers and [extensions](manual/animation-extensions/index.md) designed to enhance your experience with the [Stride Game Engine](https://www.stride3d.net/). It simplifies and streamlines routine development tasks 🛠️, making it easier to build applications for Stride using **.NET 10**.

> [!TIP]
> Experienced developers can dive straight into the source, these helpers are thin wrappers over Stride APIs, meant to accelerate iteration (not hide the engine).

## 📦 Libraries  

Each library focuses on a specific concern (core helpers, physics integration, UI/debug aids, etc.):

[!INCLUDE [global-note](includes/libraries.md)]

## 🚀 Fast Iteration (Preview Status)

This toolkit favors momentum and rapid 🏃 feature delivery. Expect:

- Breaking changes between releases (namespaces / APIs may shift).
- Early access to experimental helpers.
- Occasional rough edges in less‑traveled code paths.

Stability will improve as patterns settle. Pin a version if you need consistency.

This approach allows us to quickly iterate and integrate new features and improvements. We believe this pace serves the needs of developers who are looking for cutting-edge tools and are comfortable with a more dynamic environment.

## 🔧 Installation

The toolkit, available as a 📦 [NuGet package](https://www.nuget.org/profiles/StrideCommunity), can be integrated into new or existing Stride Game C# projects. For more information on how to get started, please refer to the [Getting Started](manual/getting-started.md) page.

## 🛠️ Toolkit Repository

The Stride Community Toolkit is an open-source, MIT-licensed project hosted on GitHub and supported by the community. Access the source code or contribute 🤝 to the toolkit on its [GitHub Repository](https://github.com/stride3d/stride-community-toolkit).

Found a gap? Open an [issue](https://github.com/stride3d/stride-community-toolkit/issues) or [PR](https://github.com/stride3d/stride-community-toolkit/pulls). Even small doc clarifications help. See [Contributing](contributing/index.md) section for guidelines.

## 🎮 Stride Game Engine Repository

Access the source code or contribute 🤝 to the Stride Game Engine on its [GitHub Repository](https://github.com/stride3d/stride). Explore a comprehensive guide on the [Stride Docs](https://doc.stride3d.net/) website.

## 📃 Documentation & Resources

Explore a range of resources to help you get the most out of the toolkit:

- [Manual](manual/index.md): Detailed guidance and best practices
- [Tutorials](tutorials/index.md): Step-by-step tutorials to help you learn various features
- [Release Notes](release-notes/index.md): Stay updated with the latest changes and improvements
- [API Reference](api/index.md): In-depth API documentation for a deep dive into the toolkit's capabilities
- [Contributing](contributing/index.md): Guidelines for contributing to the toolkit

These resources provide comprehensive information and support for developers at all levels, from beginners to advanced users.

## 👥 Contributors

Thanks to everyone pushing this forward:

- [dfkeenan](https://github.com/dfkeenan): Previous toolkit implementation
- [DockFrankenstein](https://github.com/DockFrankenstein): Script System Extensions
- [Doprez](https://github.com/Doprez): Extensions, docs
- [DotLogix](https://github.com/dotlogix): Utility @Stride.CommunityToolkit.Rendering.Utilities.MeshBuilder, @Stride.CommunityToolkit.Rendering.Utilities.TextureCanvas and docs
- [Idomeneas1970](https://github.com/Idomeneas1970): Heightmap extensions
- [IXLLEGACYIXL](https://github.com/IXLLEGACYIXL): Extensions
- [Johan Gustafsson](https://github.com/johang88): Extensions
- [juho](https://github.com/juhe1): Example Scripts
- [Jurgen Coenegrachts](https://github.com/w200338): Fixes
- [Luca Domenichini](https://github.com/luca-domenichini): Fixes
- [Martin Blackman](https://github.com/mpblackman): Examples
- [Vaclav Elias](https://github.com/VaclavElias): Code-only approach implementation, toolkit docs
