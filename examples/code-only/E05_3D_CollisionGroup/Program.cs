using Stride.BepuPhysics;
using Stride.BepuPhysics.Definitions;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;

// Collision groups: two players that collide with each other and pass through the enemy, the red box.
// Bodies in one group collide with each other only when their IndexA values differ by 2 or more, so
// the players at 0 and 2 collide, and the enemy at 1 passes through both.
var playerGroup1 = new CollisionGroup { Id = 1, IndexA = 0 };
var playerGroup2 = new CollisionGroup { Id = 1, IndexA = 2 };
var enemyGroup = new CollisionGroup { Id = 1, IndexA = 1 };

using var game = new Game();

game.Run(new SceneSetup3D().UseBepu().UseSkybox(), start: Start);

void Start(Scene scene)
{
    CreateCube(scene, "Player1", Color.MediumSeaGreen, new Vector3(0, 0.5f, 0), playerGroup1);
    CreateCube(scene, "Player2", Color.MediumPurple, new Vector3(0.5f, 4, 0.7f), playerGroup2);
    CreateCube(scene, "Enemy", Color.Red, new Vector3(-0.1f, 12, 0.5f), enemyGroup);
}

void CreateCube(Scene scene, string name, Color color, Vector3 position, CollisionGroup group)
{
    var entity = game.CreatePrimitive(Primitive3D.Cube(), new()
    {
        EntityName = name,
        Material = game.CreateMaterial(color),
        Position = position,
    });

    entity.Get<BodyComponent>().CollisionGroup = group;
    entity.Scene = scene;
}
/*
---example-metadata
slug: collision-group
title:
  en: Collision Group
level: Intermediate
category: Physics
complexity: 3
order: 80
description:
  en: |-
    Two players and an enemy, where the players collide with each other but the enemy passes through
    both. Bepu collision groups decide this arithmetically rather than by a table: bodies in the same
    group collide only when their index values differ by more than a set amount. Here the players sit at
    0 and 2 and the enemy at 1, so the players interact and the enemy is transparent to them.
concepts:
  - Assigning bodies to a collision group
  - "How the IndexA difference rule decides whether two bodies collide"
  - Choosing index values so one entity phases through others
  - When an arithmetic rule beats a full collision matrix
  - "Using helpers: SceneSetup3D, UseSkybox, CreatePrimitive, CreateMaterial"
tags:
  - 3D
  - Bepu
  - Physics
  - Collision
  - Collision Group
  - Filtering
related:
  - E05_3D_CollisionLayer
  - E05_3D_Raycast
enabled: true
created: 2025-03-08
---
*/