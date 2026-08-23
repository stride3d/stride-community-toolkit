using Stride.BepuPhysics;
using Stride.BepuPhysics.Definitions;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;

// This example demonstrates CollisionLayer feature, two players colliding with each other and not colliding with the enemy entity (red cube)

// Define collision layers to control which objects can collide with each other
// Objects within the same layer can collide with each other
var playerLayer = CollisionLayer.Layer1;
var enemyLayer = CollisionLayer.Layer2;
var groundLayer = CollisionLayer.Layer3;

// CollisionMatrix is used to define which objects can collide with each other
var collisionMatrix = new CollisionMatrix();
collisionMatrix.Set(playerLayer, playerLayer, shouldCollide: true);
collisionMatrix.Set(playerLayer, enemyLayer, shouldCollide: false);
collisionMatrix.Set(playerLayer, groundLayer, shouldCollide: true);
collisionMatrix.Set(enemyLayer, groundLayer, shouldCollide: true);

using var game = new Game();

game.Run(start: Start);

// Sets up the initial scene with players and enemies
void Start(Scene scene)
{
    game.SetupBase3DScene();
    game.AddSkybox();

    SetupCollisionMatrix(scene);
    SetupGroundCollisionLayer(scene);

    // Create player entities
    CreateEntityWithLayer("Player1", Color.MediumSeaGreen, new Vector3(0, 0.5f, 0), scene, playerLayer);
    CreateEntityWithLayer("Player2", Color.MediumPurple, new Vector3(0.5f, 4, 0.7f), scene, playerLayer);

    // Create enemy entity
    CreateEntityWithLayer("Enemy", Color.Red, new Vector3(-0.1f, 12, 0.5f), scene, enemyLayer);
}

void SetupCollisionMatrix(Scene scene)
{
    var camera = scene.GetCamera();

    var simulation = camera?.Entity.GetSimulation();

    if (simulation == null) return;

    simulation.CollisionMatrix = collisionMatrix;
}

void SetupGroundCollisionLayer(Scene scene)
{
    var groundEntity = scene.Entities.FirstOrDefault(e => e.Name == "Ground");

    if (groundEntity == null) return;

    var groundBody = groundEntity.GetComponent<StaticComponent>();

    groundBody!.CollisionLayer = groundLayer;
}

void CreateEntityWithLayer(string name, Color color, Vector3 position, Scene scene, CollisionLayer layer)
{
    var enemy = CreateEntity(name, color, position);
    var body = enemy.GetComponent<BodyComponent>();

    body!.CollisionLayer = layer;

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
/*
---example-metadata
slug: collision-layer
title:
  en: Collision Layer
level: Intermediate
category: Physics
complexity: 3
order: 90
description:
  en: |-
    The same players-and-enemy scene as the collision group example, solved the other way. Layers name
    the categories - players, enemies, ground - and a matrix says which pairs interact, so both players
    collide with each other and the ground while the enemy ignores the players but still lands on the
    floor. Layers are the readable choice when the rules are per-pair; groups win when they follow a
    formula.
concepts:
  - Defining named collision layers
  - Filling in the collision matrix pair by pair
  - Letting one entity phase through another while both keep colliding with the ground
  - When to prefer a layer matrix over an index rule
  - "Using helpers: SetupBase3DScene, AddSkybox, Create3DPrimitive, CreateFlatMaterial"
tags:
  - 3D
  - Bepu
  - Physics
  - Collision
  - Collision Layer
  - Filtering
related:
  - Example16_CollisionGroup
  - Example14_Raycast
media: stride-game-engine-example16-collision-layer.webp
enabled: true
created: 2025-03-09
---
*/