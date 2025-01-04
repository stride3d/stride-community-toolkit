---
_disableToc: false
---
# Stride Community Toolkit Documentation

[![Join the chat at https://discord.gg/f6aerfE](https://img.shields.io/discord/500285081265635328.svg?style=flat&logo=discord&label=discord&logoColor=f2f2f2)](https://discord.gg/f6aerfE)
[![License](https://img.shields.io/badge/license-MIT-blue)](https://github.com/stride3d/stride/blob/master/LICENSE.md)

[!INCLUDE [global-note](includes/global-note.md)]

## 👋 Introduction

The [Stride Community Toolkit](https://github.com/stride3d/stride-community-toolkit) is a set of C# helpers and [extensions](manual/animation-extensions/index.md) designed to enhance your experience with the [Stride Game Engine](https://www.stride3d.net/). It simplifies and streamlines routine development tasks 🛠️, making it easier to build applications for Stride using .NET 8 🎉.

> [!TIP]
> Experienced developers might prefer exploring the toolkit's source code directly. These extensions are convenience wrappers, offering a foundation for advanced users to build custom solutions.

## 📦 Libraries

The toolkit includes the following libraries:

- [`Stride.CommunityToolkit`](https://github.com/stride3d/stride-community-toolkit/tree/main/src/Stride.CommunityToolkit): This is the core library. Use it for general-purpose extensions in a regular Stride project or for a code-only approach.
- [`Stride.CommunityToolkit.Bepu`](https://github.com/stride3d/stride-community-toolkit/tree/main/src/Stride.CommunityToolkit.Bepu): This library contains Bepu Physics extensions.
- [`Stride.CommunityToolkit.Bullet`](https://github.com/stride3d/stride-community-toolkit/tree/main/src/Stride.CommunityToolkit.Bullet): This library contains Bullet Physics extensions.
- [`Stride.CommunityToolkit.ImGui`](https://github.com/stride3d/stride-community-toolkit/tree/main/src/Stride.CommunityToolkit.ImGui): This library contains ImgGui extensions.
- [`Stride.CommunityToolkit.Skyboxes`](https://github.com/stride3d/stride-community-toolkit/tree/main/src/Stride.CommunityToolkit.Skyboxes): Enhances code-only projects by adding skybox functionality.
- [`Stride.CommunityToolkit.Windows`](https://github.com/stride3d/stride-community-toolkit/tree/main/src/Stride.CommunityToolkit.Windows): This library contains Windows-specific dependencies required for code-only approach.

## 🔧 Installation

The toolkit, available as a 📦 [NuGet package](https://www.nuget.org/profiles/StrideCommunity), can be integrated into new or existing Stride Game C# projects. For more information on how to get started, please refer to the [Getting Started](manual/getting-started.md) page.

## 🚀 Fast-Paced Development

This toolkit serves as our preferred solution for rapid 🏃 prototyping and accelerated game development. Unlike the more stable Stride Game Engine, the Stride Community Toolkit aims for faster development momentum. As such, you should expect that **breaking changes** are likely to occur. This approach allows us to quickly iterate and integrate new features and improvements. We believe this pace serves the needs of developers who are looking for cutting-edge tools and are comfortable with a more dynamic environment.

## 🛠️ Toolkit Repository

The Stride Community Toolkit is an open-source, MIT-licensed project hosted on GitHub and supported by the community. Access the source code or contribute 🤝 to the toolkit on its [GitHub Repository](https://github.com/stride3d/stride-community-toolkit).

## 🎮 Stride Game Engine Repository

Access the source code or contribute 🤝 to the Stride Game Engine on its [GitHub Repository](https://github.com/stride3d/stride). Explore a comprehensive guide on the [Stride Docs](https://doc.stride3d.net/) website.

## 📃 Documentation & Resources

Explore a range of resources to help you get the most out of the toolkit:

- [Manual](manual/index.md): Detailed guidance and best practices for using the toolkit
- [Tutorials](tutorials/index.md): Step-by-step tutorials to help you learn various features of the toolkit
- [Release Notes](release-notes/index.md): Stay updated with the latest changes and improvements
- [API Reference](api/index.md): In-depth API documentation for a deep dive into the toolkit's capabilities

These resources provide comprehensive information and support for developers at all levels, from beginners to advanced users.

## 👥 Contributors

We would like to thank our contributors for expanding the toolkit's capabilities:

- [DotLogix](https://github.com/dotlogix): Utility @Stride.CommunityToolkit.Rendering.Utilities.MeshBuilder, @Stride.CommunityToolkit.Rendering.Utilities.TextureCanvas and docs
- [Doprez](https://github.com/Doprez): Extensions
- [IXLLEGACYIXL](https://github.com/IXLLEGACYIXL): Extensions
- [Vaclav Elias](https://github.com/VaclavElias): Code-only approach implementation, toolkit docs
- [dfkeenan](https://github.com/dfkeenan): Previous toolkit implementation
- [Idomeneas1970](https://github.com/Idomeneas1970): Heightmap extensions
- [DockFrankenstein](https://github.com/DockFrankenstein): Script System Extensions
