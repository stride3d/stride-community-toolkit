using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.Overlay;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Input;

const string GroupName = "PrimitiveModelGroup";

// The five colours of the first examples, one shape after another
Color[] colours = [new(30, 70, 170), new(240, 150, 60), new(235, 100, 80), new(110, 200, 110), new(190, 130, 230)];

using var game = new Game();

game.Run(new SceneSetup3D { Profiler = true }.UseBepu().UseSkybox(), start: Start, update: Update);

void Start(Scene scene)
{
    // The one line. Left mouse grabs, the wheel changes the carry distance, T + mouse turns the body.
    game.AddGrabber();
    game.AddGroundGizmo(showAxisName: true);

    // One entity for both debug views, since each acts on the whole scene: P draws every collider,
    // F11 the physics debug meshes
    game.AddPhysicsDebug(gizmoColor: new Color4(0.4f, 0.843f, 0, 0.9f));

    AddInstructions();
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
    (Primitive3D Type, Vector3 Position, Quaternion? Rotation)[] primitives =
    [
        (Primitive3D.Cube(0.5f), new Vector3(-4f, 0.5f, 0), null),
        (Primitive3D.Box(new Vector3(0.25f, 0.5f, 0.25f)), new Vector3(-2.2f, 0.5f, -4f), null),
        (Primitive3D.Cone(0.5f, 3), new Vector3(0, 2, 0), null),
        (Primitive3D.Capsule(0.25f, 0.5f), new Vector3(0.01f, 6, 0), null),
        (Primitive3D.Sphere(0.25f), new Vector3(0, 8, 0), null),
        (Primitive3D.Cylinder(0.25f, 0.5f), new Vector3(0.5f, 10, 0), null),
        (Primitive3D.Teapot(), new Vector3(3, 4f, 0), null),
        (Primitive3D.Torus(), new Vector3(0, 12, 0), null),
        (Primitive3D.TriangularPrism(new(1, 1, 3)), new Vector3(-6.0f, 2, -8.0f), Quaternion.RotationY(MathUtil.DegreesToRadians(125))),
        (Primitive3D.TriangularPrism(Vector3.One), new Vector3(-3.5f, 0.5f, 1), null),
    ];

    foreach (var (index, (type, position, rotation)) in primitives.Index())
    {
        var entity = game.CreatePrimitive(type, new() { EntityName = GroupName, Material = game.CreateMaterial(colours[index % colours.Length]) });

        entity.Transform.Position = position;
        entity.Transform.Rotation = rotation ?? Quaternion.Identity;
        entity.Scene = scene;
    }
}

static void ResetTheScene(Scene scene)
{
    foreach (var entity in scene.Entities.Where(e => e.Name == GroupName).ToList())
    {
        entity.Scene = null;
    }
}

void AddInstructions()
{
    // The overlay draws itself, and is shared with the camera controller's own help, so nothing here
    // needs to be called every frame
    TextOverlay.GetOrCreate(game).AddSection("Game", static () =>
    [
        new("P", "Show or hide the collidable gizmos", Color.Gold),
        new("F11", "Show or hide the debug meshes", Color.Gold),
        new("R", "Reset the scene", Color.Gold),
    ]);
}

/*
---example-metadata
slug: primitives-3d
title:
  en: 3D Primitives
level: Beginner
category: Shapes
complexity: 2
order: 10
description:
  en: |-
    Nine of the toolkit's 3D primitives - cube, box, cone, capsule, sphere, cylinder, teapot, torus and
    triangular prism - dropped into one scene so the shapes, their default sizes and their generated
    colliders can be compared side by side. Naming each entity is what makes the scene resettable: R
    removes everything with that name and rebuilds, which is the simplest safe teardown pattern there
    is. P and F11 turn on the collider and debug-mesh overlays.
concepts:
  - Creating nine Primitive3D shapes and comparing their defaults
  - Sizing a primitive through its factory arguments, such as Primitive3D.Cone(0.5f, 3)
  - Rotating an entity with Transform.Rotation
  - Tagging entities with a name so the scene can be torn down and rebuilt
  - Inspecting the generated colliders and debug meshes with AddPhysicsDebug, on P and F11
  - Adding keyboard instructions as a TextOverlay section
  - "Using helpers: SceneSetup3D, AddGrabber, AddPhysicsDebug, CreatePrimitive"
tags:
  - 3D
  - Bepu
  - Shapes
  - Primitives
  - Debug Text
  - Gizmo
  - Input
related:
  - E01_3D_BasicScene
  - E02_2D_Primitives
  - E08_3D_CollidableGizmo
enabled: true
created: 2025-10-06
---
*/