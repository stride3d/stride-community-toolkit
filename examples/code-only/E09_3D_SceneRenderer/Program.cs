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

BodyComponent body = null!;
var impulseApplied = false;

using var game = new Game();

// Three ways to draw over the scene: a scene-wide renderer (MyCustomSceneRenderer), the toolkit's
// EntityTextRenderer for EntityTextComponent labels, and a renderer that belongs to one entity
// (SpriteBatchRendererScript)
game.Run(new SceneSetup3D { Profiler = true }.UseBepu().UseSkybox(), start: Start, update: Update);

void Start(Scene scene)
{
    game.AddGrabber();

    // 1. A scene renderer: a label for every entity, and the static "Example 1"
    game.AddSceneRenderer(new MyCustomSceneRenderer());

    // 2. The toolkit's renderer for EntityTextComponent labels
    game.AddSceneRenderer(new EntityTextRenderer());

    var cube = game.CreatePrimitive(Primitive3D.Cube(), new PrimitiveEntityOptions { Position = new Vector3(0, 0.5f, 0) });

    // 2. The cube's label
    cube.Add(new EntityTextComponent
    {
        Text = "Example 2: Me, Cube",
        FontSize = 12,
        TextColor = Color.Purple,
    });

    cube.Scene = scene;

    var capsule = game.CreatePrimitive(Primitive3D.Capsule(), new PrimitiveEntityOptions { Position = new Vector3(0, 8, 0) });

    // Tilted, so it rolls when it lands
    capsule.Transform.Rotation = Quaternion.RotationZ(MathUtil.DegreesToRadians(2));

    body = capsule.Get<BodyComponent>();

    // 3. A startup script that draws for this one entity
    capsule.Add(new SpriteBatchRendererScript());

    capsule.Add(new EntityTextComponent
    {
        Text = "Example 2: Hello, Stride!",
        FontSize = 13,
        TextColor = Color.Blue,
        Offset = new(0, -100),
        EnableBackground = true,
    });

    capsule.Scene = scene;
}

void Update(Scene scene, GameTime time)
{
    if (impulseApplied) return;

    // A push, so it rolls after it lands
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
    covers the whole scene and is the right shape for HUDs and text overlays; a DelegateSceneRenderer
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
  - "Using helpers: SceneSetup3D, UseSkybox, CreatePrimitive"
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