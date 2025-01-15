using Stride.CommunityToolkit.Bullet;
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

    // Add a gizmo to help visualize the ground plane and axis directions
    game.AddGroundGizmo(showAxisName: true);

    // Create a cube entity with a violet material and position it in the scene
    var cube = game.Create3DPrimitive(PrimitiveModelType.Cube, new()
    {
        Material = game.CreateMaterial(Color.Violet),
    });

    // Set the position of the cube
    cube.Transform.Position = new Vector3(0, 8, -3);

    // Add cube to the scene
    cube.Scene = scene;

    // Create a sphere entity with a wheat-colored material
    var entity = game.Create3DPrimitive(PrimitiveModelType.Sphere, new()
    {
        Material = game.CreateMaterial(Color.Wheat),
    });

    // Set the position of the sphere
    entity.Transform.Position = new Vector3(-4, 8, 0);

    // Add sphere to the scene
    entity.Scene = scene;

    // Retrieve the camera and the physics simulation from the scene
    camera = scene.GetCamera();
    simulation = game.SceneSystem.SceneInstance.GetProcessor<PhysicsProcessor>()?.Simulation;
}

// Update method called every frame to handle game logic
void Update(Scene scene, GameTime gameTime)
{
    // Ensure that the camera and simulation are initialized
    if (simulation == null || camera is null) return;

    // Check if the left mouse button is pressed
    if (game.Input.IsMouseButtonPressed(Stride.Input.MouseButton.Left))
    {
        // Cast a ray from the mouse position into the world
        var ray = camera.ScreenToWorldRaySegment(game.Input.MousePosition);

        // Perform raycasting in the simulation to detect any entities
        var hitResult = simulation.Raycast(ray);

        if (hitResult.Succeeded)
        {
            // If the ray hits an entity, set it as the camera's target
            target = hitResult.Collider.Entity;
        }
    }

    // If a target entity is set, make the camera smoothly look at it
    if (target != null)
    {
        camera.Entity.Transform.LookAt(target.Transform, game.DeltaTime() * 3.0f);
    }
}