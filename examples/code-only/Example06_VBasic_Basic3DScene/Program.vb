Imports Stride.CommunityToolkit.Extensions
Imports Stride.CommunityToolkit.ProceduralModels
Imports Stride.Core.Mathematics
Imports Stride.Engine

Module Program
    Private game As New Game()

    Sub Main()
        GameExtensions.Run(game, Nothing, AddressOf StartGame)
    End Sub

    Private Sub StartGame(rootScene As Scene)
        game.SetupBase3DScene()
        game.AddProfiler()

        Dim entity = game.CreatePrimitive(PrimitiveModelType.Capsule)
        entity.Transform.Position = New Vector3(0, 8, 0)
        entity.Scene = rootScene
    End Sub
End Module