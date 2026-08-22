using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Mathematics;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Rendering.Lights;

var random = new Random(1);
var count = 10;
List<Primitive2DModelType> primitives = [
    Primitive2DModelType.Circle,
    Primitive2DModelType.Capsule,
    Primitive2DModelType.Rectangle,
    Primitive2DModelType.Square,
    Primitive2DModelType.Triangle,
    Primitive2DModelType.Circle,
    Primitive2DModelType.Capsule,
];

using var game = new Game();

game.Run(start: Start);

void Start(Scene rootScene)
{
    game.Window.AllowUserResizing = true;
    game.Window.Title = "Bepu 2D Physics Primitives";

    game.SetupBase2D();
    game.Add2DCameraController();
    game.AddProfiler();

    var ground = game.Add2DGround();
    ground.Transform.Position = new Vector3(0, -4, 0);

    // Lighting example for 2D scene
    AddSpotLight(rootScene);

    for (int i = -count / 2; i < count / 2; i++)
    {
        foreach (var (index, primitive2) in primitives.Index())
        {
            var entity = game.Create2DPrimitive(primitive2, new()
            {
                Material = game.CreateFlatMaterial(random.NextColor()),
            });

            entity.Transform.Position = new Vector3(i, 10 + index * 1.5f, 0);
            entity.Scene = rootScene;
        }
    }

    // Activate debug rendering by pressing P for colliders and F11 for mesh
    AddPhysicsDebugGizmo(rootScene);
}

static void AddSpotLight(Scene rootScene)
{
    var spotLight = new Entity("SpotLight")
    {
        new LightComponent
        {
            Type = new LightSpot
            {
                Range = 20f,
                AngleInner = 20f,
                AngleOuter = 35f
            },
            Intensity = 1000f,
        }
    };

    spotLight.Transform.Position = new Vector3(0, -4, 2);
    spotLight.Scene = rootScene;
}

static void AddPhysicsDebugGizmo(Scene rootScene)
{
    var debugGizmoEntity = new Entity("DebugGizmo")
    {
        new DebugRenderComponentScript(),
        new CollidableGizmoScript()
        {
            Color = new Color4(0.4f, 0.843f, 0, 0.9f),
            Visible = false
        }
    };

    debugGizmoEntity.Scene = rootScene;
}
/*
---example-metadata
slug: debug-render-2d
title:
  en: Basic2D Scene (Debug Rendering)
level: Beginner
category: Debug
complexity: 2
order: 100
description:
  en: |-
    A pile of falling 2D shapes with the physics debug overlays turned on, so what the simulation is
    actually solving can be seen rather than inferred. P draws the colliders and F11 draws the debug
    meshes. Both come from components on a single entity that has nothing else to do, which is the
    cheapest way to add them to any scene. A spot light is included because 2D scenes are lit like any
    other and look flat without one.
concepts:
  - Drawing physics colliders with CollidableGizmoScript
  - Toggling debug meshes with DebugRenderComponentScript
  - Hanging both off one otherwise empty entity
  - Lighting a 2D scene with a LightSpot
  - Giving each shape its own colour with a flat material
  - "Using helpers: SetupBase2D, Add2DCameraController, Add2DGround, Create2DPrimitive"
tags:
  - 2D
  - Bepu
  - Debug
  - Gizmo
  - Physics
  - Lighting
  - Primitives
related:
  - Example01_Basic2DScene_FallingShapes
  - Example08_CollidableGizmo
  - Example08_DebugRenderComponent
enabled: true
created: 2025-11-30
---
*/