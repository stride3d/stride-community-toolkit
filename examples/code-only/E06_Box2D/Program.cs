using E06_Box2D.Helpers;
using Stride.CommunityToolkit.Box2D;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Shapes;
using Stride.CommunityToolkit.Windows;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;

// Example 18: Box2D Physics Integration
// This example demonstrates how to integrate Box2D.NET with Stride game engine
// for 2D physics simulations with shapes, collisions, and interactive controls

// Per-monitor DPI awareness, so a 150% display gets a sharp window rather than a stretched one;
// it has to happen before the window exists
WindowsDpiManager.EnablePerMonitorV2();

// Global variables for the demo
Box2DSimulation? simulation = null;
SceneManager? sceneManager = null;

using var game = new Game();

game.Run(start: Start, update: Update);

void Start(Scene rootScene)
{
    // Configure the game window
    game.Window.AllowUserResizing = true;
    game.Window.Title = "Box2D Physics Example - Stride Community Toolkit";

    // Set up a 2D scene with camera and controls
    game.SetupBase2D(clearColor: new Color(0.2f));
    game.Add2DCameraController();
    //game.AddGraphicsCompositor();
    //game.AddGraphicsCompositor2();
    //game.Add2DGraphicsCompositor(clearColor);
    //game.Add3DCamera().Add3DCameraController();
    //game.AddSkybox();
    game.AddProfiler();
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

    // Initialize the Box2D physics simulation
    simulation = new Box2DSimulation();
    ConfigurePhysicsWorld();

    // Left mouse picks any shape up and throws it; a click on empty space still spawns one
    game.GetCameraEntity().Add(new Grabber2DScript { Simulation = simulation });

    // Initialize the demo manager to handle all demo logic
    sceneManager = new SceneManager(game, rootScene, simulation);
    sceneManager.Initialize();
}

void Update(Scene rootScene, GameTime time)
{
    // Update physics simulation
    simulation?.Update(time.Elapsed);

    // Update demo manager (handles input and UI)
    sceneManager?.Update(time);
}

void ConfigurePhysicsWorld()
{
    // Configure gravity (negative Y is down)
    simulation.Gravity = new Vector2(0f, GameConfig.Gravity);

    // Enable contact events for collision detection
    simulation.EnableContactEvents = true;
    simulation.EnableSensorEvents = true;

    // Set physics timestep properties
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
  - Creating dynamic, kinematic and static bodies in Box2D
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