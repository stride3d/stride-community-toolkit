using Stride.BepuPhysics;
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
    game.AddGroundGizmo(new(-5, 0, -5), showAxisName: true);

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
        var hit = camera.Raycast(game.Input.MousePosition, 100, out var hitInfo);

        if (hit)
        {
            Console.WriteLine(hitInfo.Collidable.Entity);

            var body = hitInfo.Collidable.Entity.Get<BodyComponent>();

            if (body is null) return;

            var direction = new Vector3(1, 1, 1);

            body.ApplyImpulse(direction * 1, new());
            body.Awake = true;
        }
        else
        {
            Console.WriteLine("No hit");
        }
    }
}