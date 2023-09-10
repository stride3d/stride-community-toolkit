# Get started

This article covers how to get started using the packages provided as part of the Stride Community Toolkit project.

## Prerequisites

You must install the following, otherwise you won't be able to build/run the project. If you are using Stride **4.1**+ already, these should be already installed.

<!---
- https://download.visualstudio.microsoft.com/download/pr/0c1cfec3-e028-4996-8bb7-0c751ba41e32/1abed1573f36075bfdfc538a2af00d37/vc_redist.x86.exe
- https://download.visualstudio.microsoft.com/download/pr/cc0046d4-e7b4-45a1-bd46-b1c079191224/9c4042a4c2e6d1f661f4c58cf4d129e9/vc_redist.x64.exe
-->

1. Install Microsoft Visual C++ 2013 Redistributable
   - [vcredist_x86.exe](https://download.microsoft.com/download/2/E/6/2E61CFA4-993B-4DD4-91DA-3737CD5CD6E3/vcredist_x86.exe)
   - [vcredist_x64.exe](https://download.microsoft.com/download/2/E/6/2E61CFA4-993B-4DD4-91DA-3737CD5CD6E3/vcredist_x64.exe)
2. Install Microsoft Visual C++ 2015-2019 Redistributable
   - [vcredist_x64.exe](https://download.visualstudio.microsoft.com/download/pr/cc0046d4-e7b4-45a1-bd46-b1c079191224/9c4042a4c2e6d1f661f4c58cf4d129e9/vc_redist.x64.exe)
2. New - Install Microsoft Visual C++ 2015-2022 Redistributable
   - [vcredist_x64.exe](https://aka.ms/vs/17/release/vc_redist.x64.exe) (25MB)
3. Install .NET 6 SDK x64 https://dotnet.microsoft.com/en-us/download (200MB)
4. Install IDE of your choice
   - Visual Studio 2022
      - [Visual Studio 2022 Community](https://visualstudio.microsoft.com/vs/) is free
      - Make sure that **.NET desktop development** workload is selected when installing Visual Studio
   - [Visual Studio Code](https://code.visualstudio.com/) (free, 81MB)
      -  Make sure you install also **C# for Visual Studio Code (powered by OmniSharp)** extension
      -  Restart Visual Studio Code otherwise ```dotnet``` command might not work
   - Rider (paid)

## Adding the NuGet package(s)

The toolkit is available as a set of NuGet packages that can be added to any existing or new project using Visual Studio.
