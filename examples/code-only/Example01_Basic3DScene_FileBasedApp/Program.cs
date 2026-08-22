// Basic3D Scene as a .NET 10 file-based app - the same scene as Example01_Basic3DScene, but with
// no .csproj at all. Everything the build needs is declared by the #: directives below.
//
//   Run:     dotnet run Program.cs
//   Build:   dotnet build Program.cs
//   Convert: dotnet project convert Program.cs   (emits a regular .csproj next to this file)
//
// Requires the .NET 10 SDK. Because there is no project file, this example is not part of
// Stride.CommunityToolkit.slnx and Visual Studio will not build it with the rest of the solution -
// run it from the command line.

#:package Stride.BepuPhysics.Debug@$(StrideVersion)
#:project ../../../src/Stride.CommunityToolkit.Bepu/Stride.CommunityToolkit.Bepu.csproj
#:project ../../../src/Stride.CommunityToolkit.Skyboxes/Stride.CommunityToolkit.Skyboxes.csproj
#:project ../../../src/Stride.CommunityToolkit.Windows/Stride.CommunityToolkit.Windows.csproj
#:property PublishAot=false

// Stride's asset compiler builds its cache path by concatenating $(ProjectDir) with
// $(IntermediateOutputPath). That assumes the latter is relative, which holds for a normal project
// but not for a file-based app, whose obj/bin live under the SDK's temp cache and are therefore
// absolute - producing a malformed path and an IOException during build.
//
// Uncomment the two directives below to work around it on a Stride build that lacks the fix. They
// force obj/ and bin/ next to this file, which costs the SDK's shared temp build cache, so prefer
// leaving them commented once the fix is in.
//
// #:property BaseIntermediateOutputPath=obj\
// #:property OutputPath=bin\

using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;

using var game = new Game();

game.Run(start: Start);

void Start(Scene rootScene)
{
    game.SetupBase3DScene();
    game.AddSkybox();

    var entity = game.Create3DPrimitive(PrimitiveModelType.Capsule);
    entity.Transform.Position = new Vector3(0, 8, 0);
    entity.Scene = rootScene;
}

/*
---example-metadata
slug: file-based-app
title:
  en: Basic3D Scene (Capsule) - File-Based App
  cs: Základní 3D scéna (Kapsle) - aplikace bez projektu
level: Getting Started
category: Shapes
complexity: 1
order: 20
description:
  en: |-
    The same minimal 3D scene as Example01_Basic3DScene, written as a .NET 10 file-based app: a single
    C# file with no .csproj. NuGet packages, project references and MSBuild properties are declared
    inline with #:package, #:project and #:property directives, so the example runs with
    "dotnet run Program.cs". Use "dotnet project convert" to turn it back into a regular project.
  cs: |-
    Stejná minimální 3D scéna jako Example01_Basic3DScene, ale napsaná jako aplikace bez projektu
    (.NET 10 file-based app): jediný soubor C# bez .csproj. Balíčky NuGet, odkazy na projekty
    a vlastnosti MSBuild se deklarují přímo pomocí direktiv #:package, #:project a #:property,
    takže příklad se spustí příkazem "dotnet run Program.cs". Příkazem "dotnet project convert"
    jej lze převést zpět na běžný projekt.
concepts:
  - Running a Stride game as a .NET 10 file-based app (no .csproj)
  - "Declaring NuGet packages inline with #:package"
  - "Referencing projects inline with #:project"
  - "Setting MSBuild properties inline with #:property"
  - Converting a file-based app to a project with dotnet project convert
  - "Using helpers: SetupBase3DScene"
  - "Using helpers: AddSkybox"
tags:
  - 3D
  - Bepu
  - Shapes
  - Primitive
  - Capsule
  - Scene Setup
  - Skybox
  - File-Based App
  - dotnet run
related:
  - Example01_Basic3DScene
  - Example02_GiveMeACube
  - Example01_Basic3DScene_Primitives
enabled: true
created: 2026-08-08
---
*/