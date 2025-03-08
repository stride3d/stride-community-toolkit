using Stride.BepuPhysics;
using Stride.BepuPhysics.Definitions;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;

// This example demonstrates how to create a simple scene with two players and an enemy entity
// and set up collision groups to control which objects can collide with each other.

// Define collision groups to control which objects can collide with each other
// Objects within the same group can't collide with each other, however, if IndexA is used, the objects collide with each other if the difference is more than 2

// In this example, the players will collide with each other because the difference between their IndexA values is 2
// The enemy entity (red box) won't collide with the players because the difference between their IndexA values is 1
var playerCollisionGroup1 = new CollisionGroup { Id = 1, IndexA = 0 };
var playerCollisionGroup2 = new CollisionGroup { Id = 1, IndexA = 2 };
var enemyCollisionGroup = new CollisionGroup { Id = 1, IndexA = 1 };

using var game = new Game();

game.Run(start: Start);

// Sets up the initial scene with players and enemies
void Start(Scene scene)
{
    game.SetupBase3DScene();
    game.AddSkybox();

    // Create player entities
    CreateEntityWithGroup("Player1", Color.MediumSeaGreen, new Vector3(0, 0.5f, 0), scene, playerCollisionGroup1);
    CreateEntityWithGroup("Player2", Color.MediumPurple, new Vector3(0.5f, 4, 0.7f), scene, playerCollisionGroup2);

    // Create enemy entity
    CreateEntityWithGroup("Enemy", Color.Red, new Vector3(-0.1f, 12, 0.5f), scene, enemyCollisionGroup);
}

void CreateEntityWithGroup(string name, Color color, Vector3 position, Scene scene, CollisionGroup collisionGroup)
{
    var enemy = CreateEntity(name, color, position);
    var body = enemy.GetComponent<BodyComponent>();

    body!.CollisionGroup = collisionGroup;

    enemy.Scene = scene;
}

Entity CreateEntity(string name, Color color, Vector3 position)
{
    var entity = game.Create3DPrimitive(PrimitiveModelType.Cube, new()
    {
        EntityName = name,
        Material = game.CreateMaterial(color),
    });

    entity.Transform.Position = position;

    return entity;
}