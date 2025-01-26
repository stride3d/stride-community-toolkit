using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Input;

CameraComponent? camera = null;

using var game = new Game();

game.Run(start: Start, update: Update);

void Start(Scene scene)
{
    game.SetupBase3DScene();
    game.AddSkybox();

    var entity = game.Create3DPrimitive(PrimitiveModelType.Capsule);

    entity.Transform.Position = new Vector3(0, 8, 0);

    entity.Scene = scene;

    camera = scene.GetCamera();
}

void Update(Scene scene, GameTime time)
{
    if (camera == null) return;

    if (game.Input.IsMouseButtonPressed(MouseButton.Left))
    {
        var hit = camera.Raycast(game.Input.MousePosition);

        if (hit == null)
        {
            Console.WriteLine("No hit");
        }
        else
        {
            Console.WriteLine(hit.Value.Collidable.Entity);
        }
    }
}