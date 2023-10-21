using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Physics;
using Stride.Rendering;

using var game = new Game();

//game.Run(start: (Scene rootScene) =>
//{
//    game.SetupBase3DScene();

//    var entity = game.CreatePrimitive(PrimitiveModelType.Capsule);

//    entity.Transform.Position = new Vector3(0, 8, 0);

//    entity.Scene = rootScene;
//});

//game.Run(start: (Scene rootScene) =>
//{
//    game.SetupBase3DScene();

//    for (int i = 0; i < 10; i++)
//    {
//        var entity = game.CreatePrimitive(PrimitiveModelType.Capsule);

//        entity.Transform.Position = new Vector3(0, 8, 0);

//        entity.Scene = rootScene;
//    }
//});

const float Interval = 2.0f;
const int MaxLayers = 5;
const int Rows = 5;

var sideLength = 1;
var cubeSize = new Vector3(sideLength);
var colours = new[] { Color.Red, Color.Green, Color.Blue };
var materials = new Material[3];
var random = new Random();
double elapsedTime = 0f;
var layer = 1;

game.Run(start: Start, update: Update);

void Start(Scene scene)
{
    game.SetupBase3DScene();
    game.AddProfiler();

    CreateMaterials(game, colours, materials);

    CreateFirstRow(game, scene);
}

static void CreateMaterials(Game game, Color[] colours, Material[] materials)
{
    for (var i = 0; i < materials.Length; i++)
    {
        materials[i] = game.CreateMaterial(colours[i]);
    }
}

void CreateFirstRow(Game game, Scene scene)
{
    var entities = CreateModelRow(game, scene, 0.5f);

    AddColliders(entities);
}

void AddColliders(List<Entity> entities)
{
    foreach (var entity in entities)
    {
        var collider = new RigidbodyComponent()
        {
            Mass = 1000f,
        };

        collider.ColliderShapes.Add(new BoxColliderShapeDesc
        {
            Size = cubeSize,
        });

        entity.Add(collider);
    }
}

void Update(Scene scene, GameTime time)
{
    elapsedTime += time.Elapsed.TotalSeconds;

    if (elapsedTime >= Interval && layer <= MaxLayers)
    {
        elapsedTime = 0;

        var entities = CreateModelRow(game, scene, layer + 0.5f);

        AddColliders(entities);

        layer++;
    }
}

List<Entity> CreateModelRow(Game game, Scene scene, float y)
{
    var entities = new List<Entity>();

    for (var x = 0; x < Rows; x++)
    {
        for (var z = 0; z < Rows; z++)
        {
            var entity = GiveMeCube(game, cubeSize);

            entity.Transform.Position = new Vector3(x, y, z);

            entity.Scene = scene;

            entities.Add(entity);
        }
    }

    return entities;
}

Material GetRandomMaterial() => materials[random.Next(0, 3)];

Entity GiveMeCube(Game game, Vector3 size)
{
    return game.CreatePrimitive(PrimitiveModelType.Cube, "Cube", size: size, material: GetRandomMaterial(), includeCollider: false);
}