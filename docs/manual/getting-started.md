# 🚀 Get Started

This article guides you through the initial steps to utilize the packages within the Stride Community Toolkit project.

## 🛠️ Prerequisites

Ensure the following are installed to build/run the project. If you're on Stride **4.1**+ already, these should be pre-installed.

1. Microsoft Visual C++ 2015-2022 Redistributable
   - [vcredist_x64.exe](https://aka.ms/vs/17/release/vc_redist.x64.exe) (25MB)
1. .NET 6 SDK x64: [Download](https://dotnet.microsoft.com/en-us/download) (200MB)
   - Verify installation with:
        ```bash
        dotnet --info
        ```
1. IDE of your choice
   - Visual Studio 2022
      - [Visual Studio 2022 Community](https://visualstudio.microsoft.com/vs/) (Free)
      - Ensure **.NET desktop development** workload is selected during installation
   - [Visual Studio Code](https://code.visualstudio.com/) (Free, 81MB)
      - Install **C# for Visual Studio Code (powered by OmniSharp)** extension
      - Restart Visual Studio Code to ensure `dotnet` command functions properly
   - Rider (Paid)

## 📦 Adding the NuGet package(s)

The toolkit comprises a set of NuGet packages, easily integrable into existing or new projects via Visual Studio.
