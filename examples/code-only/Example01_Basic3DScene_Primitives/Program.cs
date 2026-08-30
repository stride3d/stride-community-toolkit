using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Rendering.Text;
using Stride.CommunityToolkit.Scripts.Utilities;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Input;

const string EntityName = "PrimitiveModelGroup";

var size1 = new Vector3(0.5f);
var size2 = new Vector3(0.25f, 0.5f, 0.25f);
DebugOverlaySection? instructions = null;

using var game = new Game();

game.Run(start: Start, update: Update);

void Start(Scene scene)
{
    game.SetupBase3DScene();
    game.AddSkybox();
    game.AddProfiler();

    InitializeDebugOverlay();
    Add3DPrimitives(scene);
}

void Update(Scene scene, GameTime time)
{
    if (game.Input.IsKeyPressed(Keys.R))
    {
        ResetTheScene(scene);
        Add3DPrimitives(scene);
    }

}

void Add3DPrimitives(Scene scene)
{
    var primitives = new[]
    {
        (Type: PrimitiveModelType.Cube, Position: new Vector3(-4f, 0.5f, 0), Size: size1, Rotation: null, AddDebug: true),
        (Type: PrimitiveModelType.Cube, Position: new Vector3(-2.2f, 0.5f, -4f), Size: size2, Rotation: null, AddDebug: false),
        (Type: PrimitiveModelType.Cone, Position: new Vector3(0, 2, 0), Size: new (0.5f, 3, 0), Rotation: null, AddDebug: false),
        (Type: PrimitiveModelType.Capsule, Position: new Vector3(0.01f, 6, 0), Size: size2, Rotation: null, AddDebug: false),
        (Type: PrimitiveModelType.Sphere, Position: new Vector3(0, 8, 0), Size: size2, Rotation: null, AddDebug: false),
        (Type: PrimitiveModelType.Cylinder, Position: new Vector3(0.5f, 10, 0), Size: size2, Rotation: null, AddDebug: false),
        (Type: PrimitiveModelType.Teapot, Position: new Vector3(3, 4f, 0), Size: null, Rotation: null, AddDebug: false)!,
        (Type: PrimitiveModelType.Torus, Position: new Vector3(0, 12, 0), Size: null, Rotation: null, AddDebug: false)!,
        (Type: PrimitiveModelType.TriangularPrism, Position: new Vector3(-8.0f, 2, -3.0f), Size: (Vector3?)null, Rotation: (Quaternion?)Quaternion.RotationY(75), AddDebug: false),
        (Type: PrimitiveModelType.TriangularPrism, Position: new Vector3(-3.5f, 0.5f, 1), Size: null, Rotation: null, AddDebug: false)!,
    };

    foreach (var primitive in primitives)
    {
        var entity = CreatePrimitive(primitive.Type, primitive.Position, primitive.Size, primitive.Rotation);

        if (primitive.AddDebug)
            AddDebugComponents(entity);

        entity.Scene = scene;
    }

    Entity CreatePrimitive(PrimitiveModelType type, Vector3 position, Vector3? size, Quaternion? rotation)
    {
        var entity = game.Create3DPrimitive(type, size.HasValue ? new() { Size = size.Value } : null);
        entity.Transform.Position = position;

        if (rotation.HasValue)
            entity.Transform.Rotation = rotation.Value;

        entity.Name = EntityName;
        return entity;
    }

    void AddDebugComponents(Entity entity)
    {
        entity.Add(new DebugRenderComponentScript());
        entity.Add(new CollidableGizmoScript()
        {
            Color = new Color4(0.4f, 0.843f, 0, 0.9f),
            Visible = false
        });
    }
}

static void ResetTheScene(Scene scene)
{
    var entities = scene.Entities.Where(e => e.Name == EntityName).ToList();

    foreach (var entity in entities)
        entity.Scene = null;
}

void InitializeDebugOverlay()
{
    // The overlay draws itself, and is shared with the camera controller's own help, so nothing here
    // needs to be called every frame
    var overlay = DebugOverlay.GetOrCreate(game);

    overlay.Position = DisplayPosition.BottomLeft;

    instructions = overlay.AddSection("Game", static () =>
    [
        new("INSTRUCTIONS"),
        new("Press P to see collidables"),
        new("Press F11 to see debug meshes"),
        new("Press R to reset the scene", Color.Yellow),
    ]);
}
/*
---example-metadata
slug: primitives-3d
title:
  en: Basic3D Scene (Every Primitive)
level: Beginner
category: Shapes
complexity: 2
order: 10
description:
  en: |-
    Every 3D primitive the toolkit can build - cube, cone, capsule, sphere, cylinder, teapot, torus and
    triangular prism - dropped into one scene so the shapes, their default sizes and their generated
    colliders can be compared side by side. Naming each entity is what makes the scene resettable: R
    removes everything with that name and rebuilds, which is the simplest safe teardown pattern there
    is. P and F11 turn on the collider and debug-mesh overlays.
concepts:
  - Creating each PrimitiveModelType and comparing their defaults
  - Sizing a primitive with Primitive3DCreationOptions
  - Rotating an entity with Transform.Rotation
  - Tagging entities with a name so the scene can be torn down and rebuilt
  - Inspecting generated colliders with CollidableGizmoScript
  - Adding keyboard instructions as a DebugOverlay section
  - "Using helpers: SetupBase3DScene, AddSkybox, AddProfiler, Create3DPrimitive"
tags:
  - 3D
  - Bepu
  - Shapes
  - Primitives
  - Debug Text
  - Gizmo
  - Input
related:
  - Example01_Basic3DScene
  - Example01_Basic2DScene_Primitives
  - Example08_CollidableGizmo
enabled: true
created: 2025-10-06
---
*/