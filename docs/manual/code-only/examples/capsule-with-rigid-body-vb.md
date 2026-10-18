---
generated: true
slug: capsule-with-rigid-body-vb
---

# Capsule with rigid body in Visual Basic

The first code-only scene written in Visual Basic. VB has no top-level statements, so the app is a
Module with a Main, and it cannot call C# extension methods with extension syntax - Run is invoked
as the plain static method GameExtensions.Run, with the callback passed using AddressOf.

The `Program.vb` file shows how to:

- Writing a code-only Stride app in Visual Basic
- Hosting the app in a Module with a Sub Main
- Calling an extension method as a static: GameExtensions.Run(game, ...)
- Passing a callback with AddressOf
- Using helpers: SceneSetup3D, UseBepu, UseSkybox, AddProfiler, CreatePrimitive

![Capsule with rigid body in Visual Basic](media/capsule-with-rigid-body-vb.webp)

View on [GitHub](https://github.com/stride3d/stride-community-toolkit/tree/main/examples/code-only/E01_3D_BasicScene_VisualBasic).

[!code-vb[](../../../../examples/code-only/E01_3D_BasicScene_VisualBasic/Program.vb?start=1&end=25)]