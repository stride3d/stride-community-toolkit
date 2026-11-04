using Stride.BepuPhysics;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.Overlay;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Rendering.Text;
using Stride.CommunityToolkit.Shapes;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Input;
using Stride.Rendering;

// One body, several shapes. Every Bepu body in Stride carries a CompoundCollider, and the toolkit's
// CreatePrimitive puts one collider in it; a body that is a hammer, a dumbbell or an L-piece wants
// two or three, each at its own local position and rotation with its own mass. The compound is then
// one rigid thing: it lands as a whole, its mass is the sum, and it turns about a centre of mass that
// the masses decide. A ring marks that centre on each body.
//
// The right-hand lane drops the same pieces as separate bodies, placed just as the compounds' children
// are. Nothing joins them, so they come apart on landing. That is the difference a compound makes: not
// what the pieces look like, which is the same on both sides, but that they are one body.
//
// The pieces' models are child entities of the body's entity, so they ride on its transform; only the
// colliders know about the physics. Space drops everything again; T drops it with a spin.

const float LaneOffset = 3.2f;
const float DropHeight = 3.5f;
const float RigSpacing = 3f;

const float HeadMass = 2f;
const float HandleMass = 0.5f;
const float WeightMass = 1.5f;
const float BarMass = 0.4f;
const float ArmMass = 1f;

var rigs = new List<Rig>();
var looseBodies = new List<(BodyComponent Body, Vector3 Start, Quaternion Rotation)>();
var random = new Random(3);

ShapeBatch? shapes = null;

using var game = new Game();

var sceneSetup = new SceneSetup3D
{
    Camera = CameraSetup.Perspective(position: new Vector3(0f, 6f, 12f), rotation: new Vector3(0f, -25f, 0f)).WithController(),
    WindowTitle = "Compound colliders - Stride Community Toolkit",
    Resizable = true,
    DpiAware = true,
}.UseBepu().UseSkybox();

game.Run(sceneSetup, start: Start, update: Update);

void Start(Scene scene)
{
    game.AddGrabber();

    shapes = game.AddShapeBatch(depthTest: false);

    var steel = game.CreateMaterial(new Color(160, 165, 175), metalness: 0.6f, glossiness: 0.7f);
    var wood = game.CreateMaterial(new Color(170, 120, 70), metalness: 0f, glossiness: 0.3f);
    var iron = game.CreateMaterial(new Color(70, 70, 80), metalness: 0.4f, glossiness: 0.4f);
    var brick = game.CreateMaterial(new Color(200, 90, 70), metalness: 0f, glossiness: 0.3f);

    // A hammer: a heavy head across the top of a light handle
    Piece[] hammer =
    [
        new(Primitive3D.Box(new Vector3(0.9f, 0.4f, 0.4f)), new Vector3(0f, 0.9f, 0f), Quaternion.Identity, HeadMass, steel),
        new(Primitive3D.Cylinder(0.08f, 1.6f), Vector3.Zero, Quaternion.Identity, HandleMass, wood),
    ];

    // A dumbbell: two weights on a bar, symmetric, so its centre of mass is the middle of the bar
    Piece[] dumbbell =
    [
        new(Primitive3D.Sphere(0.32f), new Vector3(-0.75f, 0f, 0f), Quaternion.Identity, WeightMass, iron),
        new(Primitive3D.Sphere(0.32f), new Vector3(0.75f, 0f, 0f), Quaternion.Identity, WeightMass, iron),
        new(Primitive3D.Cylinder(0.07f, 1.5f), Vector3.Zero, Quaternion.RotationZ(MathUtil.PiOverTwo), BarMass, steel),
    ];

    // An L-piece: two equal arms, so its centre of mass lies in the air between them
    Piece[] corner =
    [
        new(Primitive3D.Box(new Vector3(1.4f, 0.35f, 0.35f)), new Vector3(0.525f, 0f, 0f), Quaternion.Identity, ArmMass, brick),
        new(Primitive3D.Box(new Vector3(0.35f, 1.4f, 0.35f)), new Vector3(-0.175f, 0.525f, 0f), Quaternion.Identity, ArmMass, brick),
    ];

    Piece[][] designs = [hammer, dumbbell, corner];
    string[] names = ["Hammer", "Dumbbell", "L-piece"];

    for (var i = 0; i < designs.Length; i++)
    {
        var z = (i - 1) * RigSpacing;

        rigs.Add(AddCompound(scene, names[i], designs[i], new Vector3(-LaneOffset, DropHeight, z)));
        AddLoosePieces(scene, designs[i], new Vector3(LaneOffset, DropHeight, z));
    }

    TextOverlay.GetOrCreate(game).AddSection("Compound colliders", BuildOverlayLines);
}

void Update(Scene scene, GameTime time)
{
    if (game.Input.IsKeyPressed(Keys.Space)) Drop(spin: false);
    if (game.Input.IsKeyPressed(Keys.T)) Drop(spin: true);

    if (shapes is null) return;

    // The centre of mass is a local point: it rides with the body, so it is drawn through the body's world matrix
    foreach (var rig in rigs)
    {
        var centre = Vector3.TransformCoordinate(rig.Body.CenterOfMass, rig.Entity.Transform.WorldMatrix);

        shapes.StrokeWidth = 2f;
        shapes.DrawPixelRing(centre, 7f, Color.Gold);
        shapes.DrawPixelDisc(centre, 2.5f, Color.Gold);
    }
}

/// <summary>One body whose compound holds a collider per piece, each piece drawn by a child entity at the same local pose.</summary>
Rig AddCompound(Scene scene, string name, Piece[] pieces, Vector3 position)
{
    var compound = new CompoundCollider();

    foreach (var piece in pieces)
    {
        var collider = Stride.CommunityToolkit.Bepu.EntityExtensions.ColliderFor(piece.Shape);

        collider.PositionLocal = piece.Position;
        collider.RotationLocal = piece.Rotation;
        collider.Mass = piece.Mass;
        compound.Colliders.Add(collider);
    }

    var body = new BodyComponent { Collider = compound };
    var entity = new Entity(name) { body };

    entity.Transform.Position = position;

    foreach (var piece in pieces)
    {
        // Model only: the body's collider already covers this piece
        var model = game.CreatePrimitive(piece.Shape, new BepuPhysicsOptions { Body = BodyMode.None, Material = piece.Material, EntityName = $"{name} piece" });

        model.Transform.Position = piece.Position;
        model.Transform.Rotation = piece.Rotation;
        entity.AddChild(model);
    }

    entity.Scene = scene;

    return new Rig(name, entity, body, position);
}

/// <summary>The same pieces as bodies of their own, each starting where the compound's child sits.</summary>
void AddLoosePieces(Scene scene, Piece[] pieces, Vector3 position)
{
    foreach (var piece in pieces)
    {
        var entity = game.CreatePrimitive(piece.Shape, new BepuPhysicsOptions
        {
            EntityName = "Loose piece",
            Material = piece.Material,
            Mass = piece.Mass,
            Position = position + piece.Position,
        });

        entity.Transform.Rotation = piece.Rotation;
        entity.Scene = scene;

        looseBodies.Add((entity.Get<BodyComponent>(), position + piece.Position, piece.Rotation));
    }
}

void Drop(bool spin)
{
    foreach (var rig in rigs)
    {
        rig.Body.TeleportAtRest(rig.Start);

        if (spin) rig.Body.AngularVelocity = Spin();
    }

    foreach (var (body, start, rotation) in looseBodies)
    {
        body.TeleportAtRest(start, rotation);

        if (spin) body.AngularVelocity = Spin();
    }
}

Vector3 Spin() => new Vector3(random.NextSingle() - 0.5f, random.NextSingle() - 0.5f, random.NextSingle() - 0.5f) * 6f;

IReadOnlyList<TextElement> BuildOverlayLines()
{
    var lines = new List<TextElement>
    {
        new("Space", "Drop everything again", Color.Gold),
        new("T", "Drop it with a spin", Color.Gold),
        new(""),
        new("Left: one body per rig, a collider per piece", Color.LightGreen),
        new("Right: the same pieces as separate bodies", Color.Orange),
        new(""),
    };

    foreach (var rig in rigs)
    {
        // The compound's inertia is the sum of its pieces' and its centre of mass sits where the masses say
        var centre = rig.Body.CenterOfMass;

        lines.Add(new($"{rig.Name}: mass {1f / rig.Body.BodyInertia.InverseMass:0.0}, centre of mass at {centre.X:0.00}, {centre.Y:0.00}, {centre.Z:0.00}", Color.White));
    }

    return lines;
}

/// <summary>One piece of a rig: its shape, its pose in the rig's own space, its mass and its material.</summary>
sealed record Piece(Primitive3D Shape, Vector3 Position, Quaternion Rotation, float Mass, Material Material);

/// <summary>A compound body and where it started.</summary>
sealed record Rig(string Name, Entity Entity, BodyComponent Body, Vector3 Start);

/*
---example-metadata
slug: compound-colliders
title:
  en: Compound Colliders
level: Beginner
category: Physics
complexity: 2
order: 48
description:
  en: |-
    A hammer, a dumbbell and an L-piece, each one body whose compound collider holds a collider per
    piece at its own local pose and mass, beside the same pieces dropped as separate bodies. The
    compounds land whole and turn about the centre of mass their masses decide, marked with a ring;
    the loose pieces come apart. The pieces' models are child entities riding on the body's transform.
concepts:
  - A CompoundCollider with several child colliders, each with PositionLocal, RotationLocal and Mass
  - The body's mass as the sum, and CenterOfMass where the masses put it
  - Models as child entities of the body, so one transform moves the lot
  - The same pieces as separate bodies for the contrast
  - "Using helpers: SceneSetup3D, UseBepu, UseSkybox, CreatePrimitive, ColliderFor, CreateMaterial, TeleportAtRest, ShapeBatch, TextOverlay"
tags:
  - 3D
  - Physics
  - Bepu
  - Colliders
related:
  - E05_3D_ColliderShapes
  - E05_3D_MeshColliders
screenshotFrame: 200
enabled: true
created: 2026-11-01
---
*/