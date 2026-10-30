# 🚀 Get Started

## 🔗 Which version for which Stride

Each toolkit release is built against one Stride release and will not work with another, because the
engine's own packages change between them. Match the two:

| Stride      | Toolkit packages   |
| ----------- | ------------------ |
| 4.4.1       | `1.0.0-preview.68` |
| 4.4.0       | `1.0.0-preview.67` |
| 4.4.0       | `1.0.0-preview.66` |
| 4.4.0-beta8 | `1.0.0-preview.65` |
| 4.4.0-beta5 | `1.0.0-preview.63` |
| 4.3         | `1.0.0-preview.62` |
| 4.2         | `1.0.0-preview.61` |

`dotnet add package ... --prerelease` always takes the newest preview, so on an older Stride pin the
version explicitly, for example `dotnet add package Stride.CommunityToolkit --version 1.0.0-preview.62`.

## 🛠️ Prerequisites

Ensure the following are installed. If you already have Stride **4.4** installed, they are present.

1. Microsoft Visual C++ v14 Redistributable (x64)
   - [vc_redist.x64.exe](https://aka.ms/vc14/vc_redist.x64.exe) (~19 MB)
   - You may be asked to restart your PC after installation.
2. .NET 10 SDK x64: [Download](https://dotnet.microsoft.com/en-us/download) (~216 MB)
   - Verify installation:
        ```
        dotnet --info
        ```
3. IDE of your choice
   - Visual Studio 2026
      - [Visual Studio 2026 Community](https://visualstudio.microsoft.com/vs/) (Free)
   - [Visual Studio Code](https://code.visualstudio.com/) (Free, ~95 MB)
      - Install the C# Dev Kit extension
      - Restart VS Code to ensure the `dotnet` command works
   - [Rider](https://www.jetbrains.com/rider/) (Free for non-commercial use)

## 📦 Adding the NuGet package

The toolkit is a set of packages named `Stride.CommunityToolkit` and `Stride.CommunityToolkit.*`. The core package holds the game, entity, camera and rendering extensions; physics, shapes, charts, effects and the UI integrations are separate packages. Add them through your IDE or the command line. They work in regular Stride game projects and in code-only projects.

To add the package via the command line:

```
dotnet add package Stride.CommunityToolkit --prerelease
```

> [!NOTE]
> `Stride.CommunityToolkit` brings in the Stride packages a code-only project needs, including the asset compiler build step. On Linux with a distribution's own `dotnet` package, add `Stride.CommunityToolkit.Linux` as well; see the Linux tab of [Create Project](code-only/create-project.md).

### Additional toolkit packages

[!INCLUDE [global-note](../includes/libraries.md)]

See [Code-Only](code-only/index.md) for a first project, or the extension pages in the left navigation.