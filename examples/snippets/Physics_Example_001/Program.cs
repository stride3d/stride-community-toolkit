using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Games;
using Stride.CommunityToolkit.Physics;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Physics;

Entity? target = null;
CameraComponent? camera = null;
Simulation? simulation = null;

using var game = new Game();

game.Run(start: Start, update: Update);

void Start(Scene scene)
{
    // Set up a base 3D scene with default lighting and camera
    game.SetupBase3DScene();
    game.AddGroundGizmo(showAxisName: true);

    // Create a 3D cube primitive and assign the material to it
    var cube = game.Create3DPrimitive(PrimitiveModelType.Cube, new()
    {
        Material = game.CreateMaterial(Color.Violet),
    });

    cube.Transform.Position = new Vector3(0, 8, -3);

    // Add the cube to the root scene
    cube.Scene = scene;

    // Create a 3D cube primitive and assign the material to it
    var entity = game.Create3DPrimitive(PrimitiveModelType.Sphere, new()
    {
        Material = game.CreateMaterial(Color.Wheat),
    });

    entity.Transform.Position = new Vector3(-4, 8, 0);

    // Add the cube to the root scene
    entity.Scene = scene;

    camera = scene.GetCamera();
    simulation = game.SceneSystem.SceneInstance.GetProcessor<PhysicsProcessor>()?.Simulation;
}

void Update(Scene scene, GameTime gameTime)
{
    if (simulation == null || camera is null) return;

    if (game.Input.IsMouseButtonPressed(Stride.Input.MouseButton.Left))
    {
        var ray = camera.ScreenToWorldRaySegment(game.Input.MousePosition);

        var hitResult = simulation.Raycast(ray);
        if (hitResult.Succeeded)
        {
            target = hitResult.Collider.Entity;
        }
    }

    if (target != null)
    {
        camera.Entity.Transform.LookAt(target.Transform, game.DeltaTime() * 3.0f);
    }
}