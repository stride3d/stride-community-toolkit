using Stride.BepuPhysics;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Helpers;
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
    game.AddGroundGizmo(new(-5, 0, -5), showAxisName: true);

    var entity = game.Create3DPrimitive(PrimitiveModelType.Sphere);

    entity.Transform.Position = new Vector3(0, 8, 0);

    entity.Scene = scene;

    camera = scene.GetCamera();
}

void Update(Scene scene, GameTime time)
{
    if (camera == null) return;

    game.DebugTextSystem.Print("Click the sphere to apply a random impulse", new(5, 10));

    if (game.Input.IsMouseButtonPressed(MouseButton.Left))
    {
        var hit = camera.Raycast(game.Input.MousePosition, 100, out var hitInfo);

        if (hit)
        {
            var body = hitInfo.Collidable.Entity.Get<BodyComponent>();

            if (body is null) return;

            var randomDirection = VectorHelper.RandomVector3([-1, 1], [0, 1], [-1, 1]);

            Console.WriteLine($"{hitInfo.Collidable.Entity}, direction: {randomDirection}");

            body.ApplyImpulse(randomDirection * 2, new());

            body.Awake = true;
        }
        else
        {
            Console.WriteLine("No hit");
        }
    }
}