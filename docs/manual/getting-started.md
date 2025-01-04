# 🚀 Get Started

This article guides you through the initial steps to utilize the packages within the Stride Community Toolkit project.

## 🛠️ Prerequisites

Ensure the following are installed to build/run the project. If you're on Stride **4.2**+ already, these should be pre-installed.

1. Microsoft Visual C++ 2015-2022 Redistributable
   - [vcredist_x64.exe](https://aka.ms/vs/17/release/vc_redist.x64.exe) (25MB)
   - **Note:** You might be asked to restart your PC after the installation.
1. .NET 8 SDK x64: [Download](https://dotnet.microsoft.com/en-us/download) (200MB)
   - Verify installation with:
        ```
        dotnet --info
        ```
1. IDE of your choice
   - Visual Studio 2022
      - [Visual Studio 2022 Community](https://visualstudio.microsoft.com/vs/) (Free)
   - [Visual Studio Code](https://code.visualstudio.com/) (Free, 95MB)
      - Install the **C# Dev Kit** extension 
      - Restart Visual Studio Code to ensure `dotnet` command functions properly
   - [Rider](https://www.jetbrains.com/rider/) (Free for non-commercial use)

## 📦 Adding the NuGet package

The toolkit is available through several packages named `Stride.CommunityToolkit` and `Stride.CommunityToolkit.*`. The main package includes all functionalities of the toolkit. You can add this package to your project using your preferred IDE or via the command line. It is designed to be compatible with both regular Stride game projects and code-only game projects.

To add the NuGet package using the command line, execute the following command:

```
dotnet add package Stride.CommunityToolkit --prerelease
```

> [!NOTE]
> When using `Stride.CommunityToolkit` in a code-only project, you will need to manually add certain dependencies to your project file. However, if you are using the `Stride.CommunityToolkit.Windows` package, it automatically handles these dependencies for you.

### Additional toolkit packages

[!INCLUDE [global-note](../includes/libraries.md)]

Explore the extensions available in the left navigation or dive into the code-only section for simple examples.