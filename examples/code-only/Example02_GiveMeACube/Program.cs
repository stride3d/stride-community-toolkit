using Example02_GiveMeACube;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Games;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;

// Two cubes, and the difference between them is the lesson:
//   - the grounded cube is a physics body, so Bepu owns its transform
//   - the orbiting cube has no physics at all, so a script owns its transform
// The orbit itself is driven by OrbitScript, a SyncScript attached to the entity, rather than by
// the update callback of game.Run(...).

// Half the cube's height, so a 1x1x1 cube sits exactly on the ground
const float CubeRestingHeight = 0.5f;

using var game = new Game();

game.Run(start: Start);

void Start(Scene rootScene)
{
    // Camera, directional light and a ground plane
    game.SetupBase3DScene();
    game.AddSkybox();

    CreateGroundedCube(rootScene);
    CreateOrbitingCube(rootScene);
}

/// <summary>
/// Creates a cube with the toolkit's default Bepu physics: a dynamic body with a box collider.
/// It drops onto the ground and comes to rest there, and from then on the simulation writes its
/// transform every frame.
/// </summary>
void CreateGroundedCube(Scene rootScene)
{
    var cube = game.Create3DPrimitive(PrimitiveModelType.Cube, new Bepu3DPhysicsOptions());

    cube.Transform.Position = new Vector3(0, CubeRestingHeight, 0);
    cube.Scene = rootScene;
}

/// <summary>
/// Creates a cube with no physics component at all, so nothing competes with the script for its
/// transform.
/// </summary>
/// <remarks>
/// Passing <see cref="Primitive3DEntityOptions"/> selects the plain overload of Create3DPrimitive.
/// The Bepu overload would attach a <c>BodyComponent</c>, and even with <c>IncludeCollider = false</c>
/// that component is dead weight: a collider with no shapes never attaches to the simulation, so you
/// would be carrying a body that can never move or collide.
/// </remarks>
void CreateOrbitingCube(Scene rootScene)
{
    var cube = game.Create3DPrimitive(PrimitiveModelType.Cube, new Primitive3DEntityOptions());

    cube.Transform.Position = new Vector3(0, CubeRestingHeight, 0);

    // Adding the script is what gives the entity its behaviour
    cube.Add(new OrbitScript
    {
        Radius = 3f,
        AngularSpeed = 1f
    });

    cube.Scene = rootScene;
}

/*
---example-metadata
title:
  en: Give Me a Cube
  cs: Dej mi kostku
level: Getting Started
category: Scripts
complexity: 2
description:
  en: |
    Add behaviour to an entity with a SyncScript component instead of the update callback of game.Run.
    The script's Start captures the entity's position as an orbit centre and Update moves it around a
    circle each frame. Two cubes make the ownership rule visible: the grounded cube is a Bepu body whose
    transform the simulation writes, while the orbiting cube has no physics component, so the script is
    free to write its transform directly.
  cs: |
    Přidání chování k entitě pomocí komponenty SyncScript namísto callbacku update v game.Run.
    Metoda Start si uloží pozici entity jako střed kružnice a Update s ní každý snímek pohybuje po kruhu.
    Dvě kostky ukazují, kdo vlastní transformaci: kostka na zemi je těleso Bepu, jehož transformaci
    zapisuje simulace, zatímco obíhající kostka nemá žádnou fyzikální komponentu, takže její transformaci
    může přímo zapisovat skript.
concepts:
  - Adding behaviour with a SyncScript component
  - The Start and Update lifecycle methods
  - Configuring a script through public properties
  - Creating a primitive with physics versus without
  - Why writing Transform.Position does not move a physics body
  - "Using helpers: SetupBase3DScene"
  - "Using helpers: AddSkybox"
  - "Using helpers: Game.DeltaTime"
related:
  - Example01_Basic3DScene
  - Example01_Basic3DScene_SyncScript
  - Example11_ImGuiNet
tags:
  - 3D
  - Script
  - SyncScript
  - Component
  - Update Loop
  - Transform
  - Bepu
  - Physics
  - Cube
  - Getting Started
order: 2
enabled: true
created: 2023-09-11
---
*/
