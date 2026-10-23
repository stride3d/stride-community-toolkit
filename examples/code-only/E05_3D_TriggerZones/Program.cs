using Stride.BepuPhysics;
using Stride.BepuPhysics.Definitions;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.BepuPhysics.Definitions.Contacts;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.DebugShapes;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Rendering.Overlay;
using Stride.CommunityToolkit.Rendering.Text;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Input;
using Stride.Rendering;

// Reacting to contact. E05_3D_CollisionGroup and E05_3D_CollisionLayer decide what collides; this is
// what to do when it does. A collidable's ContactEventHandler hears about its contacts, and the handler
// says whether the collidable is solid. Three stations:
//
//   1. Zone: a Trigger on a static with no model. NoContactResponse is true, so bodies pass through it
//      as if it were not there, and OnEnter and OnLeave say who came and went.
//   2. Plate: a solid static with a handler of its own. Bodies rest on it like on any floor, and
//      OnStartedTouching and OnStoppedTouching count what stands on it.
//   3. Crate: a dynamic body with a handler that measures each hit, from the contacts of the first touch.
//
// Space drops a ball over every station, and the grabber carries bodies from one to another. The
// callbacks run on the main thread, after the physics step, so they may touch entities and materials.

WindowsDpiManager.EnablePerMonitorV2();

const float StationSpacing = 5f;
const float BallRadius = 0.4f;
const int BallsKept = 30;

var zoneSize = new Vector3(3f, 2.2f, 3f);
var zoneCentre = new Vector3(-StationSpacing, zoneSize.Y * 0.5f, 0f);
var plateCentre = new Vector3(0f, 0.15f, 0f);
var crateStart = new Vector3(StationSpacing, 0.6f, 0f);

var idle = new Color(150, 150, 160);
var zoneColour = new Color(90, 200, 120);
var plateColour = new Color(255, 190, 60);
var crateColour = new Color(220, 90, 80);

var inside = new HashSet<CollidableComponent>();
var entries = 0;
var plate = new TouchCounter();
var impact = new ImpactMeter();
var balls = new List<Entity>();
var random = new Random(5);

Entity? pad = null;
Entity? plateEntity = null;
Entity? crate = null;
ImmediateDebugRenderSystem? debugDraw = null;
Material? idleMaterial = null;
Material? zoneMaterial = null;
Material? plateMaterial = null;
Material? hitMaterial = null;
Material? crateMaterial = null;
Material? ballMaterial = null;

using var game = new Game();

var sceneSetup = new SceneSetup3D
{
    Camera = CameraSetup.Perspective(position: new Vector3(1f, 8f, 13f), rotation: new Vector3(0f, -28f, 0f)).WithController(),
    Profiler = true
}.UseBepu().UseSkybox();

game.Run(sceneSetup, start: Start, update: Update);

void Start(Scene scene)
{
    game.Window.AllowUserResizing = true;
    game.Window.Title = "Trigger zones - Stride Community Toolkit";

    game.AddDebugShapes();
    // F11 shows the debug meshes with the contacts the solver is resolving, which is what this example is about
    game.AddPhysicsDebug(contactsVisible: true);
    game.AddGrabber();

    debugDraw = game.Services.GetService<ImmediateDebugRenderSystem>() ?? throw new InvalidOperationException("AddDebugShapes registers the renderer first");
    debugDraw.Visible = true;

    idleMaterial = game.CreateMaterial(idle, metalness: 0f, glossiness: 0.3f);
    zoneMaterial = game.CreateMaterial(zoneColour, metalness: 0f, glossiness: 0.3f);
    plateMaterial = game.CreateMaterial(plateColour, metalness: 0f, glossiness: 0.3f);
    hitMaterial = game.CreateMaterial(Color.White, metalness: 0f, glossiness: 0.3f);
    crateMaterial = game.CreateMaterial(crateColour, metalness: 0f, glossiness: 0.3f);
    ballMaterial = game.CreateMaterial(new Color(80, 170, 255), metalness: 0f, glossiness: 0.6f);

    BuildZone(scene);
    BuildPlate(scene);
    BuildCrate(scene);
    AddLabel(scene, "1. Zone", zoneCentre.X);
    AddLabel(scene, "2. Plate", plateCentre.X);
    AddLabel(scene, "3. Crate", crateStart.X);

    // A first batch, so the stations have something to report straight away
    DropBalls(scene);

    TextOverlay.GetOrCreate(game).AddSection("Trigger zones", OverlayLines);
}

void Update(Scene scene, GameTime time)
{
    if (game.Input.IsKeyPressed(Keys.Space)) DropBalls(scene);
    if (game.Input.IsKeyPressed(Keys.R)) Reset();

    // The zone has no model: the box is drawn here, in the colour of its state
    debugDraw!.DrawCube(zoneCentre, zoneSize, color: inside.Count > 0 ? zoneColour : idle);

    pad!.Get<ModelComponent>().Materials[0] = inside.Count > 0 ? zoneMaterial! : idleMaterial!;
    plateEntity!.Get<ModelComponent>().Materials[0] = plate.Touching.Count > 0 ? plateMaterial! : idleMaterial!;

    // The crate flashes white for a moment after a hit
    impact.FlashLeft = MathF.Max(0f, impact.FlashLeft - (float)time.Elapsed.TotalSeconds);
    crate!.Get<ModelComponent>().Materials[0] = impact.FlashLeft > 0f ? hitMaterial! : crateMaterial!;
}

/// <summary>
/// A trigger is a handler that says NoContactResponse: Bepu still finds the contacts, so the events fire,
/// but the solver never pushes anything apart. The static needs no model; the pad under it is the visual.
/// </summary>
void BuildZone(Scene scene)
{
    var trigger = new Trigger();

    trigger.OnEnter += (_, other) =>
    {
        if (inside.Add(other)) entries++;
    };

    trigger.OnLeave += (_, other) => inside.Remove(other);

    var zone = new Entity("Zone")
    {
        new StaticComponent
        {
            Collider = new CompoundCollider { Colliders = { new BoxCollider { Size = zoneSize } } },
            ContactEventHandler = trigger,
        },
    };

    zone.Transform.Position = zoneCentre;
    zone.Scene = scene;

    pad = game.CreatePrimitive(Primitive3D.Box(new Vector3(zoneSize.X, 0.06f, zoneSize.Z)), new BepuPhysicsOptions { Material = idleMaterial, Body = BodyMode.None });
    pad.Name = "Zone pad";
    pad.Transform.Position = new Vector3(zoneCentre.X, 0.03f, zoneCentre.Z);
    pad.Scene = scene;
}

/// <summary>A solid static with a handler: a floor that also knows what stands on it.</summary>
void BuildPlate(Scene scene)
{
    plateEntity = game.CreatePrimitive(Primitive3D.Box(new Vector3(3f, 0.3f, 3f)), new BepuPhysicsOptions
    {
        Material = idleMaterial,
        Component = new StaticComponent { Collider = new CompoundCollider(), ContactEventHandler = plate },
    });

    plateEntity.Name = "Plate";
    plateEntity.Transform.Position = plateCentre;
    plateEntity.Scene = scene;
}

/// <summary>A dynamic body with a handler: it is hit, pushed about, and reports how hard.</summary>
void BuildCrate(Scene scene)
{
    crate = game.CreatePrimitive(Primitive3D.Cube(1.2f), new BepuPhysicsOptions
    {
        Material = crateMaterial,
        Component = new BodyComponent { Collider = new CompoundCollider(), InterpolationMode = InterpolationMode.Interpolated, ContactEventHandler = impact },
    });

    crate.Name = "Crate";
    crate.Transform.Position = crateStart;
    crate.Scene = scene;
}

/// <summary>One ball over each station, a little off centre so they do not all land the same way.</summary>
void DropBalls(Scene scene)
{
    foreach (var x in new[] { zoneCentre.X, plateCentre.X, crateStart.X })
    {
        var ball = game.CreatePrimitive(Primitive3D.Sphere(BallRadius), new BepuPhysicsOptions
        {
            Material = ballMaterial,
        });

        ball.Name = $"Ball {balls.Count + 1}";
        ball.Transform.Position = new Vector3(x + random.NextSingle() * 0.6f - 0.3f, 6f, random.NextSingle() * 0.6f - 0.3f);
        ball.Scene = scene;

        balls.Add(ball);
    }

    while (balls.Count > BallsKept)
    {
        balls[0].Scene = null;
        balls.RemoveAt(0);
    }
}

/// <summary>The balls go, the crate goes home, and the counters start again.</summary>
void Reset()
{
    foreach (var ball in balls) ball.Scene = null;

    balls.Clear();
    inside.Clear();
    entries = 0;
    plate.Reset();
    impact.Reset();

    var body = crate!.Get<BodyComponent>();

    body.Teleport(crateStart, Quaternion.Identity);
    body.Awake = true;
    body.LinearVelocity = Vector3.Zero;
    body.AngularVelocity = Vector3.Zero;
}

void AddLabel(Scene scene, string text, float x)
{
    var label = new Entity("Label")
    {
        new EntityTextComponent { Text = text, FontSize = 14, Anchor = TextAnchor.MiddleCenter, EnableShadow = true },
    };

    label.Transform.Position = new Vector3(x, 0f, 2.6f);
    label.Scene = scene;
}

IReadOnlyList<TextElement> OverlayLines() =>
[
    new("Space", "Drop a ball over each station", Color.Gold),
    new("Left mouse", "Grab a ball and carry it", Color.Gold),
    new("R", "Reset", Color.Gold),
    new("P", "Draw the collider gizmos", Color.Gold),
    new("F11", "Draw the debug meshes and the contacts", Color.Gold),
    new(""),
    new($"Zone: {inside.Count} inside, entered {entries} times", zoneColour),
    new($"Plate: {plate.Touching.Count} on it, {plate.Landings} landings", plateColour),
    new($"Crate: last hit {impact.Last:0.0} by {impact.LastBy}, hardest {impact.Hardest:0.0}", crateColour),
];

/// <summary>
/// A handler for a solid: NoContactResponse is false, so the bodies rest on the plate as on any floor,
/// and the two callbacks keep the set of what touches it. The same body can touch through more than one
/// contact, so the set, not a counter, is the truth.
/// </summary>
sealed class TouchCounter : IContactHandler
{
    public bool NoContactResponse => false;

    internal HashSet<CollidableComponent> Touching { get; } = [];

    internal int Landings { get; private set; }

    internal void Reset()
    {
        Touching.Clear();
        Landings = 0;
    }

    void IContactHandler.OnStartedTouching<TManifold>(Contacts<TManifold> contacts)
    {
        if (Touching.Add(contacts.Other)) Landings++;
    }

    void IContactHandler.OnStoppedTouching<TManifold>(Contacts<TManifold> contacts) => Touching.Remove(contacts.Other);
}

/// <summary>
/// A handler that measures hits: on the first touch of a pair, the impact force of every contact in it is
/// added up. The ground and the plate are statics and do not count; only bodies hit the crate.
/// </summary>
sealed class ImpactMeter : IContactHandler
{
    public bool NoContactResponse => false;

    internal float Last { get; private set; }

    internal float Hardest { get; private set; }

    internal string LastBy { get; private set; } = "nothing yet";

    internal float FlashLeft { get; set; }

    internal void Reset()
    {
        Last = 0f;
        Hardest = 0f;
        LastBy = "nothing yet";
    }

    void IContactHandler.OnStartedTouching<TManifold>(Contacts<TManifold> contacts)
    {
        if (contacts.Other is not BodyComponent) return;

        var force = 0f;

        foreach (var contact in contacts) force += contacts.ComputeImpactForce(contact).Length();

        Last = force;
        LastBy = contacts.Other.Entity.Name;
        Hardest = MathF.Max(Hardest, force);
        FlashLeft = 0.4f;
    }
}

/*
---example-metadata
slug: trigger-zones
title:
  en: Trigger Zones
  cs: Spouštěcí zóny
level: Beginner
category: Physics
complexity: 5
order: 82
description:
  en: |-
    Reacting to contact, three ways. A trigger zone is a static with no model whose handler says
    NoContactResponse: bodies pass through it and OnEnter and OnLeave say who came and went. A plate is a
    solid static with a handler of its own, counting what stands on it through OnStartedTouching and
    OnStoppedTouching. A crate is a dynamic body whose handler measures each hit from the contacts of
    the first touch. Space drops a ball over every station; the grabber carries bodies between them.
  cs: |-
    Reakce na dotyk, třemi způsoby. Spouštěcí zóna je statické těleso bez modelu, jehož handler hlásí
    NoContactResponse: tělesa jí procházejí a OnEnter a OnLeave říkají, kdo přišel a odešel. Deska je
    pevné statické těleso s vlastním handlerem, který přes OnStartedTouching a OnStoppedTouching počítá,
    co na ní stojí. Bedna je dynamické těleso, jehož handler změří každý zásah z kontaktů prvního dotyku.
    Mezerník upustí míč nad každou stanici; grabber tělesa mezi stanicemi přenáší.
concepts:
  - ContactEventHandler on a collidable, and NoContactResponse deciding whether it is solid
  - Trigger with OnEnter and OnLeave on a static with no model, drawn as a debug box
  - IContactHandler on a solid static, OnStartedTouching and OnStoppedTouching keeping a set of touchers
  - IContactHandler on a dynamic body, the impact force of a first touch from its contacts
  - Callbacks run on the main thread after the physics step, so they may change materials and entities
  - F11 draws the solver's contact points and normals, Stride 4.4.1's debug view, through AddPhysicsDebug
  - "Using helpers: SceneSetup3D, CreatePrimitive, AddGrabber, AddDebugShapes, AddPhysicsDebug, TextOverlay"
tags:
  - 3D
  - Physics
  - Bepu
  - Trigger
  - Collision
  - Contact Events
related:
  - E05_3D_CollisionGroup
  - E05_3D_CollisionLayer
  - E05_3D_ShapeQueries
  - E13_SignalR
screenshotFrame: 150
enabled: true
created: 2026-10-18
---
*/