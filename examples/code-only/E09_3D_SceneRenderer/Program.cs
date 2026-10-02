using E09_3D_SceneRenderer;
using Stride.BepuPhysics;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.Compositing;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Rendering.Text;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;

// This example demonstrates three different ways of adding custom rendering logic to a Stride game.
// 1. Using a custom SceneRenderer via MyCustomSceneRenderer, which renders text for all entities and static text.
// 2. Using a custom EntityTextComponent, which is a component that can be added to any entity to render text.
// 3. Using a StartupScript via SpriteBatchRendererScript, which draws specific text for a single entity.
// All approaches integrate into the Stride rendering pipeline, demonstrating how to extend the default rendering behaviour.

BodyComponent? body = null;
bool impulseApplied = false;

using var game = new Game();

// Three ways to draw over the scene: a scene-wide renderer (MyCustomSceneRenderer), the toolkit's
// EntityTextRenderer for EntityTextComponent labels, and a renderer that belongs to one entity
// (SpriteBatchRendererScript)
game.Run(start: Start, update: Update);

void Start(Scene scene)
{
    // Sets up the base 3D scene, including lighting, camera, and default settings
    game.SetupBase3DScene();

    // Adds the built-in profiler, which provides real-time performance metrics
    game.AddProfiler();

    // Adds a skybox (a background environment) to the scene
    game.AddSkybox();

    // Example 1: Adds a custom scene renderer to render text for all entities in the scene and also the static "Example 1" text
    game.AddSceneRenderer(new MyCustomSceneRenderer());

    // Example 2: Adds a custom scene renderer to render text for all entities with a component of type EntityTextComponent
    game.AddSceneRenderer(new EntityTextRenderer());

    // Creates a 3D primitive (a cube) and sets its position in the scene
    var cube = game.Create3DPrimitive(PrimitiveModelType.Cube);
    cube.Transform.Position = new Vector3(0, 0.5f, 0);

    // Example 2: Adds an EntityTextComponent to the cube entity, which renders text
    cube.Add(new EntityTextComponent()
    {
        Text = "Example 2: Me, Cube",
        FontSize = 12,
        TextColor = Color.Purple,
    });

    cube.Scene = scene;

    // Creates a 3D primitive (a capsule) and sets its position in the scene
    var capsule = game.Create3DPrimitive(PrimitiveModelType.Capsule);
    capsule.Transform.Position = new Vector3(0, 8, 0);

    // Let's tilt the capsule a bit which should cause it to move after it falls
    capsule.Transform.Rotation = Quaternion.RotationZ(MathUtil.DegreesToRadians(2));

    // Get the body component
    body = capsule.Get<BodyComponent>();

    // Example 3: Adds a custom startup script to the entity, which draws specific text using SpriteBatch when the game is running.
    capsule.Add(new SpriteBatchRendererScript());

    var textComponent = new EntityTextComponent()
    {
        Text = "Example 2: Hello, Stride!",
        FontSize = 13,
        TextColor = Color.Blue,
        Offset = new(0, -100),
        EnableBackground = true,
    };
    capsule.Add(textComponent);

    // Assigns the entity to the root scene to ensure it is rendered and updated.
    capsule.Scene = scene;
}

void Update(Scene scene, GameTime time)
{
    if (body is null) return;

    if (impulseApplied) return;

    // Let's add some momentum so it rolls after it falls, the rigid body is already added by Create3DPrimitive

    body.ApplyAngularImpulse(new(0, 10, 0));

    impulseApplied = true;
}
/*
---example-metadata
slug: renderer
title:
  en: Custom Scene Renderers
level: Advanced
category: Rendering
complexity: 4
order: 10
description:
  en: |-
    Three ways to draw 2D content over a 3D scene, side by side. A SceneRendererBase subclass
    covers the whole scene and is the right shape for HUDs and debug overlays; a DelegateSceneRenderer
    created from a StartupScript belongs to one entity and is the right shape for labels attached to a
    thing; the toolkit's EntityTextComponent does the same with no renderer of your own. The first
    two project world positions into screen space and draw through a SpriteBatch, so the example
    also covers where those resources are created and when they must be released.
concepts:
  - "Hooking the pipeline by subclassing SceneRendererBase"
  - "Attaching a renderer to one entity with DelegateSceneRenderer"
  - Converting a world position into screen coordinates
  - Drawing text and backgrounds with SpriteBatch
  - Deciding between a scene-wide and an entity-scoped renderer
  - "Labelling entities with the toolkit's EntityTextComponent and EntityTextRenderer"
  - Initialising and releasing renderer resources at the right time
  - "Using helpers: SetupBase3DScene, AddSkybox, Create3DPrimitive"
tags:
  - 3D
  - Rendering
  - Scene Renderer
  - SpriteBatch
  - HUD
  - Text
  - Screen Space
related:
  - E03_3D_EntityText
  - E09_3D_RootRendererShader
  - E07_3D_MeshLine
enabled: true
created: 2024-09-24
---
*/