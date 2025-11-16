# 🚀 Get Started

This article walks you through the initial steps to use the packages in the Stride Community Toolkit.

## 🛠️ Prerequisites

Ensure the following are installed. If you're on Stride **4.3+**, these should already be present.

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

The toolkit is available via several packages named `Stride.CommunityToolkit` and `Stride.CommunityToolkit.*`. The main package includes all functionality. Add it via your IDE or CLI. It works for both regular Stride game projects and code-only projects.

To add the package via the command line:

```
dotnet add package Stride.CommunityToolkit --prerelease
```

> [!NOTE]
> When using `Stride.CommunityToolkit` in code-only projects, you may need to add some dependencies manually to your project file. Using `Stride.CommunityToolkit.Windows` handles these automatically.

### Additional toolkit packages

[!INCLUDE [global-note](../includes/libraries.md)]

Explore the extensions in the left navigation or dive into the code-only section for simple examples.