using Stride.BepuPhysics;
using Stride.BepuPhysics.Constraints;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;

// Initialize the game instance
using var game = new Game();

// Run the game loop with the Start method
game.Run(start: Start);

void Start(Scene scene)
{
    // Set up a basic 3D scene with skybox, profiler, and a ground gizmo
    game.SetupBase3DScene();
    game.AddSkybox();
    game.AddProfiler();
    game.AddGroundGizmo(new(-5, 0, -5), showAxisName: true);

    // Create an additional capsule for visual reference
    var entity = game.Create3DPrimitive(PrimitiveModelType.Capsule);
    entity.Transform.Position = new Vector3(0, 3, 0);
    entity.Scene = scene;

    // Create a sphere with a golden material
    var sphere = game.Create3DPrimitive(PrimitiveModelType.Sphere, new()
    {
        Material = game.CreateMaterial(Color.Gold)
    });
    sphere.Transform.Position = new Vector3(0.1f, 5, -0.3f);

    // Create a second sphere to demonstrate a connected constraint
    var connectedSphere = game.Create3DPrimitive(PrimitiveModelType.Sphere);
    connectedSphere.Transform.Position = new Vector3(-2f, 1, -2f);

    // Set up a distance servo constraint between the sphere and connected sphere
    // The distance servo constraint will try to keep the distance between the two spheres the same
    // Observe the speheres pulling towards each other and the distance between them being maintained
    // Zoom out to see the effect better
    var distanceServo = new DistanceServoConstraintComponent
    {
        A = sphere.Get<BodyComponent>(),
        B = connectedSphere.Get<BodyComponent>(),
        TargetDistance = 3.0f,
    };

    sphere.Add(distanceServo);

    // Add both entities to the scene
    sphere.Scene = scene;
    connectedSphere.Scene = scene;
}