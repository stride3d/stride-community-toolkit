Imports Stride.CommunityToolkit.Bepu
Imports Stride.CommunityToolkit.Engine
Imports Stride.CommunityToolkit.Rendering.ProceduralModels
Imports Stride.CommunityToolkit.Skyboxes
Imports Stride.Core.Mathematics
Imports Stride.Engine
Imports GameExtensions = Stride.CommunityToolkit.Engine.GameExtensions

Module Program
    Private ReadOnly game As New Game()

    Sub Main()
        GameExtensions.Run(game, AddressOf StartGame)
    End Sub

    Private Sub StartGame(rootScene As Scene)
        game.SetupBase3DScene()
        game.AddSkybox()
        game.AddProfiler()

        Dim entity = game.Create3DPrimitive(PrimitiveModelType.Capsule)
        entity.Transform.Position = New Vector3(0, 8, 0)
        entity.Scene = rootScene
    End Sub
End Module
' ---example-metadata
' slug: capsule-with-rigid-body-vb
' title:
'   en: Capsule with rigid body in Visual Basic
' level: Getting Started
' category: Shapes
' complexity: 1
' order: 10
' description:
'   en: |-
'     The first code-only scene written in Visual Basic. VB has no top-level statements, so the app is a
'     Module with a Main, and it cannot call C# extension methods with extension syntax - Run is invoked
'     as the plain static method GameExtensions.Run, with the callback passed using AddressOf.
' concepts:
'   - Writing a code-only Stride app in Visual Basic
'   - "Hosting the app in a Module with a Sub Main"
'   - "Calling an extension method as a static: GameExtensions.Run(game, ...)"
'   - "Passing a callback with AddressOf"
'   - "Using helpers: SetupBase3DScene, AddSkybox, AddProfiler, Create3DPrimitive"
' tags:
'   - 3D
'   - Visual Basic
'   - Bepu
'   - Shapes
'   - Primitive
'   - Capsule
'   - Scene Setup
' related:
'   - Example01_Basic3DScene
'   - Example01_Basic3DScene_FSharp
' tocName: Capsule with rigid body
' enabled: true
' created: 2023-09-30
' ---