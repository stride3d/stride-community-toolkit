using Stride.BepuPhysics;
using Stride.BepuPhysics.Definitions;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;

// Collision layers: two players that collide with each other and pass through the enemy, the red cube.
// Whether two layers collide, a layer with itself included, is what the CollisionMatrix below says.
var playerLayer = CollisionLayer.Layer1;
var enemyLayer = CollisionLayer.Layer2;
var groundLayer = CollisionLayer.Layer3;

var collisionMatrix = new CollisionMatrix();
collisionMatrix.Set(playerLayer, playerLayer, shouldCollide: true);
collisionMatrix.Set(playerLayer, enemyLayer, shouldCollide: false);
collisionMatrix.Set(playerLayer, groundLayer, shouldCollide: true);
collisionMatrix.Set(enemyLayer, groundLayer, shouldCollide: true);

using var game = new Game();

game.Run(new SceneSetup3D().UseBepu().UseSkybox(), start: Start);

void Start(Scene scene)
{
    game.GetCameraEntity().GetSimulation().CollisionMatrix = collisionMatrix;

    // The setup's ground gets the ground layer, so the matrix applies to it too
    scene.Entities.First(e => e.Name == "Ground").Get<StaticComponent>().CollisionLayer = groundLayer;

    CreateCube(scene, "Player1", Color.MediumSeaGreen, new Vector3(0, 0.5f, 0), playerLayer);
    CreateCube(scene, "Player2", Color.MediumPurple, new Vector3(0.5f, 4, 0.7f), playerLayer);
    CreateCube(scene, "Enemy", Color.Red, new Vector3(-0.1f, 12, 0.5f), enemyLayer);
}

void CreateCube(Scene scene, string name, Color color, Vector3 position, CollisionLayer layer)
{
    var entity = game.CreatePrimitive(Primitive3D.Cube(), new()
    {
        EntityName = name,
        Material = game.CreateMaterial(color),
        Position = position,
    });

    entity.Get<BodyComponent>().CollisionLayer = layer;
    entity.Scene = scene;
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
  - "Using helpers: SceneSetup3D, UseSkybox, CreatePrimitive, CreateMaterial"
tags:
  - 3D
  - Bepu
  - Physics
  - Collision
  - Collision Layer
  - Filtering
related:
  - E05_3D_CollisionGroup
  - E05_3D_Raycast
enabled: true
created: 2025-03-09
---
*/