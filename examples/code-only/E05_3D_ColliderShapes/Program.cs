using Stride.BepuPhysics;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering;
using Stride.CommunityToolkit.Rendering.Overlay;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Rendering.Text;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;
using Stride.Input;
using Stride.Rendering;

// One of every primitive shape, dropped side by side, each with the collider the toolkit fits to it.
// CreatePrimitive builds the model from the shape and the Bepu setup builds the collider from the same
// shape, so the two agree by construction: a box gets a box collider, a sphere a sphere, a capsule and a
// cylinder their own kinds, and the shapes Bepu has no primitive for - cone, prism, torus, teapot - get
// one convex hull around every vertex, which is why a torus becomes a solid puck. A second torus carries
// a mesh collider instead, which follows the model's triangles and so keeps the hole: a cone stands under
// each torus, and the hull rests on its tip while the mesh slides down round it. P draws the colliders
// over the models.
//
// The numbers a shape takes mean what the shape's own factory says: a box's Size is its full extent on
// each axis, a sphere's Radius is half its width, a capsule's Length is the distance between its two cap
// centres, and a torus's Radius runs to the middle of its tube. The label over each shape spells that out.
//
// Space drops them again; T gives each a spin first, so the hulls show how they tumble.

const float Spacing = 2.3f;
const float DropHeight = 3f;

// The last flag asks for a mesh collider in place of the fitted one
(Primitive3D Shape, string Label, Color Colour, bool Mesh)[] shapes =
[
    (Primitive3D.Box(new Vector3(1.4f, 0.7f, 1f)), "Box\n1.4 x 0.7 x 1.0, full extents\nbox collider", new Color(220, 120, 70), false),
    (Primitive3D.Sphere(0.5f), "Sphere\nradius 0.5, so 1.0 across\nsphere collider", new Color(80, 170, 255), false),
    (Primitive3D.Capsule(0.3f, 0.6f), "Capsule\nradius 0.3, length 0.6 cap to cap\ncapsule collider", new Color(120, 210, 120), false),
    (Primitive3D.Cylinder(0.5f, 1f), "Cylinder\nradius 0.5, height 1.0\ncylinder collider", new Color(230, 200, 80), false),
    (Primitive3D.Cone(0.5f, 1f), "Cone\nradius 0.5, height 1.0\nconvex hull", new Color(200, 110, 200), false),
    (Primitive3D.TriangularPrism(new Vector3(1f, 0.8f, 1f)), "Prism\n1.0 x 0.8 x 1.0, full extents\nconvex hull", new Color(110, 200, 210), false),
    (Primitive3D.Torus(0.5f, 0.15f), "Torus\nradius 0.5 to the tube, 0.15 thick\nconvex hull: the hole is filled", new Color(250, 150, 90), false),
    (Primitive3D.Torus(0.5f, 0.15f), "The same torus\nwith a mesh collider\nthe hole stays open", new Color(250, 150, 90), true),
    (Primitive3D.Teapot(1.2f), "Teapot\nsize 1.2\nconvex hull round the lot", new Color(170, 170, 190), false),
];

var bodies = new BodyComponent[shapes.Length];
var random = new Random(7);

using var game = new Game();

var sceneSetup = new SceneSetup3D
{
    Camera = CameraSetup.Perspective(position: new Vector3(0f, 4.5f, 16f), rotation: new Vector3(0f, -14f, 0f)).WithController(),
    WindowTitle = "Collider shapes - Stride Community Toolkit",
    Resizable = true,
    DpiAware = true
}.UseBepu().UseSkybox();

game.Run(sceneSetup, start: Start, update: Update);

void Start(Scene scene)
{
    game.AddGrabber();

    var stand = game.CreateMaterial(new Color(95, 95, 105), metalness: 0f, glossiness: 0.3f);

    for (var i = 0; i < shapes.Length; i++)
    {
        var (shape, label, colour, mesh) = shapes[i];
        var material = game.CreateMaterial(colour, metalness: 0f, glossiness: 0.5f);

        var options = new BepuPhysicsOptions
        {
            EntityName = shape.Name,
            Material = material,
            Position = StartPosition(i),
        };

        if (mesh)
        {
            // A mesh collider is built from a model's triangles, so it follows the hole; the model is the one the
            // entity draws, shared through the game so it is generated once
            options.ShareModel = true;
            options.Component = new BodyComponent { Collider = new MeshCollider { Model = game.GetOrCreateModel(shape, material), Closed = true } };
        }

        var entity = game.CreatePrimitive(shape, options);

        entity.Scene = scene;
        bodies[i] = entity.Get<BodyComponent>();

        // A cone under each torus, tip up, so the two colliders show their difference as they land
        var torus = shape is Primitive3D.TorusShape;

        if (torus) AddStand(scene, stand, StartPosition(i).X);

        AddLabel(scene, label, new Vector3(StartPosition(i).X, torus ? 3.2f : 1.5f, 0f));
    }

    // The collider gizmos, over the models, on P
    game.AddPhysicsDebug();

    TextOverlay.GetOrCreate(game).AddSection("Collider shapes", BuildOverlayLines);
}

void Update(Scene scene, GameTime time)
{
    if (game.Input.IsKeyPressed(Keys.Space)) Drop(spin: false);
    if (game.Input.IsKeyPressed(Keys.T)) Drop(spin: true);
}

// Every shape back above its spot, still, and with a spin when asked for one
void Drop(bool spin)
{
    for (var i = 0; i < bodies.Length; i++)
    {
        bodies[i].TeleportAtRest(StartPosition(i));

        if (spin)
        {
            bodies[i].AngularVelocity = new Vector3(random.NextSingle() - 0.5f, random.NextSingle() - 0.5f, random.NextSingle() - 0.5f) * 8f;
        }
    }
}

Vector3 StartPosition(int index) => new((index - (shapes.Length - 1) / 2f) * Spacing, DropHeight, 0f);

// A static cone standing on the ground; the torus with the open hole slides down it, the filled one rests on its tip
void AddStand(Scene scene, Material material, float x)
{
    var cone = game.CreatePrimitive(Primitive3D.Cone(0.7f, 1.6f), new BepuPhysicsOptions
    {
        EntityName = "Stand",
        Body = BodyMode.Static,
        Material = material,
        Position = new Vector3(x, 0.8f, 0f),
    });

    cone.Scene = scene;
}

// A label that always faces the camera, on its own entity so the shape under it tumbles without it
void AddLabel(Scene scene, string text, Vector3 position)
{
    var label = new Entity("Label")
    {
        new WorldTextComponent
        {
            Text = text,
            TextColor = Color.White,
            Anchor = Anchor.BottomCenter,
            Height = 0.48f,
            Billboard = true,
            Alignment = TextAlignment.Center,
            GlowColor = new Color(0, 0, 0, 200),
            GlowSize = 2f,
        },
    };

    label.Transform.Position = position;
    label.Scene = scene;
}

IReadOnlyList<TextElement> BuildOverlayLines() =>
[
    new("Space", "Drop every shape again", Color.Gold),
    new("T", "Drop them with a spin", Color.Gold),
    new("P", "Draw the colliders over the models", Color.Gold),
];

/*
---example-metadata
slug: collider-shapes
title:
  en: Collider Shapes
level: Beginner
category: Physics
complexity: 1
order: 42
description:
  en: |-
    One of every primitive shape dropped side by side, each with the collider the toolkit fits to it:
    a box, a sphere, a capsule and a cylinder get their own kinds, and the cone, prism, torus and
    teapot get one convex hull each, which fills the torus. A second torus carries a mesh collider
    that keeps the hole, and a cone under each torus shows the difference: the hull rests on the
    tip, the mesh slides down round it. The label over each shape says what its numbers mean, a full
    extent for the box-like shapes and a radius for the round ones, and P draws the colliders over
    the models.
concepts:
  - One shape builds both the model and the collider, so the two agree by construction
  - Which shapes have a Bepu primitive and which become one convex hull around every vertex
  - What Size, Radius, Length and Height mean on each shape
  - A MeshCollider from the shared model where a hull would fill a hole
  - The collider gizmos from AddPhysicsDebug, toggled with P
  - "Using helpers: SceneSetup3D, UseBepu, UseSkybox, CreatePrimitive, GetOrCreateModel, CreateMaterial, AddPhysicsDebug, TeleportAtRest, TextOverlay"
tags:
  - 3D
  - Physics
  - Bepu
  - Colliders
related:
  - E05_3D_MeshColliders
  - E05_3D_CompoundColliders
  - E05_3D_PhysicsMaterials
screenshotFrame: 180
enabled: true
created: 2026-11-01
---
*/