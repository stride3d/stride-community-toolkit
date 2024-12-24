Imports Stride.CommunityToolkit.Bullet
Imports Stride.CommunityToolkit.Engine
Imports Stride.CommunityToolkit.Rendering.ProceduralModels
Imports Stride.CommunityToolkit.Skyboxes
Imports Stride.Core.Mathematics
Imports Stride.Engine
Imports GameExtensions = Stride.CommunityToolkit.Engine.GameExtensions

Module Program
    Private ReadOnly game As New Game()

    Sub Main()
        GameExtensions.Run(game, Nothing, AddressOf StartGame)
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