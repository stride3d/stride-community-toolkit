# 🚀 Get Started

This article guides you through the initial steps to utilize the packages within the Stride Community Toolkit project.

## 🛠️ Prerequisites

Ensure the following are installed to build/run the project. If you're on Stride **4.1**+ already, these should be pre-installed.

1. Microsoft Visual C++ 2015-2022 Redistributable
   - [vcredist_x64.exe](https://aka.ms/vs/17/release/vc_redist.x64.exe) (25MB)
1. .NET 8 SDK x64: [Download](https://dotnet.microsoft.com/en-us/download) (200MB)
   - Verify installation with:
        ```
        dotnet --info
        ```
1. IDE of your choice
   - Visual Studio 2022
      - [Visual Studio 2022 Community](https://visualstudio.microsoft.com/vs/) (Free)
   - [Visual Studio Code](https://code.visualstudio.com/) (Free, 95MB)
      - Install **C# Dev Kit** extension 
      - Restart Visual Studio Code to ensure `dotnet` command functions properly
   - Rider (Paid)

## 📦 Adding the NuGet package

The toolkit is encapsulated in a single package named `Stride.CommunityToolkit`. This package embodies all the toolkit's functionalities. You can use your preferred IDE or the command line to add this package to your project.


To add the NuGet package using the command line, execute the following command:

```
dotnet add package Stride.CommunityToolkit --prerelease
```

Use the left navigation to check our extension or dive in code-only section for simple examples.