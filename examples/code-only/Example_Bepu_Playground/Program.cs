using Stride.BepuPhysics.Components.Containers;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Input;
using Stride.Rendering;

using var game = new Game();
var shift = new Vector3(30, 0, 0);
Model? model = null;
int cubes = 0;
int debugX = 5;
int debugY = 30;

game.Run(start: (Action<Scene>?)((Scene rootScene) =>
{
    game.SetupBase3DScene();
    game.AddProfiler();

    var entity = game.CreatePrimitive(PrimitiveModelType.Capsule);
    entity.Transform.Position = new Vector3(0, 20, 0);
    entity.Scene = rootScene;


    var groundProceduralModel = Procedural3DModelBuilder.Build(PrimitiveModelType.Cube, new(20, 1, 20));
    var groundModel = groundProceduralModel.Generate(game.Services);

    var ground = new Entity("BepuCube")
    {
        new ModelComponent(groundModel) { RenderGroup = RenderGroup.Group0 },
        new StaticContainerComponent() { Colliders = { new BoxCollider() { Size = new(20, 1, 20) } } }
    };

    ground.Transform.Position = new Vector3(0, 2, 0) + shift;
    ground.Scene = rootScene;

    var cubeProceduralModel = Procedural3DModelBuilder.Build(PrimitiveModelType.Cube, Vector3.One);
    model = cubeProceduralModel.Generate(game.Services);

    GenerateCubes(rootScene, shift, model);

}), update: Update);

void Update(Scene scene, GameTime time)
{
    if (game.Input.IsKeyPressed(Keys.Space))
    {
        GenerateCubes(scene, shift, model);

        SetCubeCount(scene);
    }
    else if (game.Input.IsKeyReleased(Keys.X))
    {
        foreach (var entity in scene.Entities.Where(w => w.Name == "BepuCube").ToList())
        {
            entity.Remove();
        }

        SetCubeCount(scene);
    }

    RenderNavigation();
}

static void GenerateCubes(Scene rootScene, Vector3 shift, Model model)
{
    for (int i = 0; i < 100; i++)
    {
        var entity2 = new Entity("BepuCube") {
            new ModelComponent(model) { RenderGroup = RenderGroup.Group0 }
        };

        var component = new BodyContainerComponent();

        component.Colliders.Add(new BoxCollider());

        entity2.Add(component);
        entity2.Transform.Position = new Vector3(Random.Shared.Next(-5, 5), 10, Random.Shared.Next(-5, 5)) + shift;
        entity2.Scene = rootScene;
    }
}

void SetCubeCount(Scene scene) => cubes = scene.Entities.Where(w => w.Name == "BepuCube").Count();

void RenderNavigation()
{
    game.DebugTextSystem.Print($"Cubes: {cubes}", new Int2(x: debugX, y: debugY));
    game.DebugTextSystem.Print($"X - Delete all cubes and shapes", new Int2(x: debugX, y: debugY + 30), Color.Red);
    game.DebugTextSystem.Print($"Space - generate 3D cubes", new Int2(x: debugX, y: debugY + 60));
}