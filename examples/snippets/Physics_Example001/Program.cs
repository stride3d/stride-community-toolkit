using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;

Entity? target = null;
CameraComponent? camera = null;

using var game = new Game();

game.Run(new SceneSetup3D().UseBepu(), start: Start, update: Update);

void Start(Scene scene)
{
    // Add a gizmo to help visualize the ground plane and axis directions
    game.AddGroundGizmo(showAxisName: true);

    // Create a cube entity with a violet material and position it in the scene
    var cube = game.CreatePrimitive(Primitive3D.Cube(), new()
    {
        Material = game.CreateMaterial(Color.Violet),
    });

    cube.Transform.Position = new Vector3(0, 8, -3);

    // Add cube to the scene
    cube.Scene = scene;

    // Create a sphere entity with a wheat-colored material
    var entity = game.CreatePrimitive(Primitive3D.Sphere(), new()
    {
        Material = game.CreateMaterial(Color.Wheat),
    });

    entity.Transform.Position = new Vector3(-4, 8, 0);

    // Add sphere to the scene
    entity.Scene = scene;

    camera = scene.GetCamera();
}

// Update method called every frame to handle game logic
void Update(Scene scene, GameTime gameTime)
{
    game.DebugTextSystem.Print("Click on a sphere, cube or ground to turn the camera", new Int2(5, 10));

    if (camera is null) return;

    if (game.Input.IsMouseButtonPressed(Stride.Input.MouseButton.Left))
    {
        var hit = camera.Raycast(game.Input.MousePosition, 100, out var hitInfo);

        if (hit)
        {
            target = hitInfo.Collidable.Entity;
        }
        else
        {
            Console.WriteLine("No hit");
        }
    }

    // If a target entity is set, make the camera smoothly look at it
    if (target != null)
    {
        camera.Entity.Transform.LookAt(target.Transform, game.DeltaTime() * 3.0f);
    }
}