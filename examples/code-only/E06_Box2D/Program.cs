using E06_Box2D.Helpers;
using Stride.CommunityToolkit.Box2D;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Shapes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;

// Box2D.NET in Stride: a 2D simulation with shapes, collisions and interactive controls

SceneManager sceneManager = null!;

using var game = new Game();
using var simulation = new Box2DSimulation();

var sceneSetup = new SceneSetup2D
{
    ClearColor = new Color(0.2f),
    Ground = null,
    Profiler = true,
    WindowTitle = "Box2D Physics Example - Stride Community Toolkit",
    Resizable = true,
    DpiAware = true,
};

game.Run(sceneSetup, start: Start, update: Update);

void Start(Scene rootScene)
{
    // Shapes render through the toolkit's Box2D debug draw - testbed-style fill and pixel-constant
    // borders from an SDF shader, replacing the old per-mesh outline render features
    var shapeBatch = game.AddShapeBatch();
    shapeBatch.BorderWidth = 1.5f;
    shapeBatch.Fill.Alpha = 0.4f;
    // Neon tubes: a bloom outside every border in the border's own colour, adding light rather than
    // covering. A third of the strength at the edge is what reads as light around the stroke; full
    // strength would just look like a fatter border
    shapeBatch.Glow.Set(10f);
    shapeBatch.Glow.Strength = 0.35f;
    shapeBatch.Glow.Additive = true;

    ConfigurePhysicsWorld();

    // Left mouse picks any shape up and throws it; a click on empty space still spawns one
    game.AddGrabber2D(simulation);

    sceneManager = new SceneManager(game, rootScene, simulation);
    sceneManager.Initialize();
}

void Update(Scene rootScene, GameTime time)
{
    simulation.Update(time.Elapsed);
    sceneManager.Update(time);
}

void ConfigurePhysicsWorld()
{
    // Negative Y is down
    simulation.Gravity = new Vector2(0f, GameConfig.Gravity);

    simulation.EnableContactEvents = true;
    simulation.EnableSensorEvents = true;

    simulation.TimeScale = 1.0f;
    simulation.MaxStepsPerFrame = 3;
}
/*
---example-metadata
slug: box2d-physics
title:
  en: Box2D.NET Physics
level: Advanced
category: Physics
complexity: 4
order: 90
description:
  en: |-
    A 2D simulation run by Box2D.NET rather than by Stride's own physics, with Stride reduced to drawing
    the result. That split is the whole lesson: the physics world steps on a fixed timestep of its own,
    and entity transforms are copied from body poses afterwards, so nothing in the scene graph is
    allowed to move a body directly. The same pattern applies to any external simulation you want to
    drive a Stride scene with.
concepts:
  - Hosting an external physics world alongside Stride
  - "Stepping a simulation on a fixed timestep, independent of frame rate"
  - Creating dynamic and static bodies in Box2D
  - Copying body poses onto entity transforms each frame
  - Why the scene graph must not write back to the simulation
  - "Requires the Box2D.NET NuGet package"
tags:
  - 2D
  - Physics
  - Box2D
  - Integration
  - Fixed Timestep
  - Third Party
related:
  - E06_Jitter2
  - E01_2D_BasicScene
enabled: true
created: 2025-08-11
---
*/