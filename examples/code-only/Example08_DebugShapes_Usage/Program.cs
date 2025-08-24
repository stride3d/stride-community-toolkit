using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.DebugShapes.Code;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;

const string SphereEntityName = "Sphere";
ImmediateDebugRenderSystem? debugDraw = null;

// Cache sphere entities to avoid per-frame scene iteration and string comparisons
List<Entity> sphereEntities = new(capacity: 8);

using var game = new Game();

game.Run(start: (Scene rootScene) =>
{
    game.SetupBase3DScene();
    game.AddSkybox();
    game.AddProfiler();
    game.AddDebugShapes();

    CreateSpheres(rootScene, 6);

    debugDraw = game.Services.GetService<ImmediateDebugRenderSystem>();

    if (debugDraw != null)
    {
        debugDraw.Visible = true; // force visible in non-debug builds
        debugDraw.PrimitiveColor = Color.Lime;
    }
}, update: Update);


void CreateSpheres(Scene rootScene, int count)
{
    // Precompute half to avoid recalculating inside loop
    int half = count / 2;

    for (int i = -half; i < half; i++)
    {
        var entity = game.Create3DPrimitive(PrimitiveModelType.Sphere, new() { EntityName = SphereEntityName });
        entity.Transform.Position = new Vector3(i * 0.99f, 8, 0);
        entity.Scene = rootScene;
        sphereEntities.Add(entity);
    }
}

void Update(Scene scene, GameTime gameTime)
{
    if (debugDraw is null) return;

    // Iterate cached list – no allocations, no name checks
    foreach (var entity in sphereEntities)
    {
        var position = entity.Transform.Position; // cache struct access

        debugDraw.DrawSphere(position, 0.5f, Color.Red, solid: false);
        debugDraw.DrawCircle(position, 0.55f, rotation: entity.Transform.Rotation, color: Color.Orange, solid: false);
    }
}