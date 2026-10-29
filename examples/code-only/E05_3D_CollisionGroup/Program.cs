using Stride.BepuPhysics;
using Stride.BepuPhysics.Constraints;
using Stride.BepuPhysics.Definitions;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.BepuPhysics.Definitions.Contacts;
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
using Stride.Input;

// Collision groups. A group is a number rule inside a layer: bodies with the same group Id ignore each other
// when their indexes are the same or next to each other, and collide when the indexes are two or more apart.
// Bodies with different Ids, or Id 0, are not filtered at all. Two stations show the two uses the
// CollisionGroup documentation gives:
//
//   1. Teams: three lanes, two spheres a lane running at each other, each wearing its name and its group Id.
//      Team A has Id 1 and team B has Id 2, so A meets A and B meets B pass through each other, and A meets B
//      bounces. G sets every Id to 0, and all three lanes bounce.
//   2. Chain: four links hanging end to end from a swinging hook, all with Id 1 and indexes 0 to 3.
//      Neighbours are one apart and ignored, so the chain bends freely at its sockets; links two or more
//      apart still collide when it folds right over. C switches the filtering off, and the touching ends
//      collide at every bend: the chain goes stiff and jittery, and the link contact counter climbs.
//
// Groups are for rules that follow a formula: a team, a chain, debris from one explosion. For rules that are
// a table of named pairs, E05_3D_CollisionLayer and its matrix are the readable choice.

const float SphereRadius = 0.45f;
const float RunSpeed = 5f;
const float RunDistance = 3.4f;
const float LaneSpacing = 4f;
const float LinkLength = 1f;
const float HookDrop = 0.4f;
const float HookSwing = 1.3f;
const float HookRate = 1.2f;
const float RunEvery = 4f;
const float CameraPitch = -21f;

var teamsCentre = new Vector3(-4.8f, SphereRadius, 0f);
var tagOffset = new Vector3(0f, SphereRadius + 0.15f, 0f);

// Free-standing labels lean back by the camera's pitch, so they face the view instead of keystoning into italics
var labelTilt = Quaternion.RotationX(MathUtil.DegreesToRadians(CameraPitch));
var hookPosition = new Vector3(5f, 6.4f, 0f);

var teamA = new Color(90, 200, 120);
var teamB = new Color(255, 160, 60);
var linkColour = new Color(120, 170, 255);

// Two spheres a lane, left then right: A meets A, A meets B, B meets B
char[] teams = ['A', 'A', 'A', 'B', 'B', 'B'];
Color[] laneColours = [teamA, Color.White, teamB];

// A name each, drawn from the pool in a fresh order every run
string[] names = ["Ada", "Bea", "Cal", "Dev", "Eli", "Fay", "Gus", "Ivy", "Kai", "Lou", "Max", "Zoe"];

Random.Shared.Shuffle(names);

var spheres = new BodyComponent[teams.Length];
var sphereLabels = new WorldTextComponent[teams.Length];
var laneLabels = new WorldTextComponent[laneColours.Length];
var stationLabels = new WorldTextComponent[2];
var links = new BodyComponent[4];
var bounces = new TouchCounter();
var linkTouches = new TouchCounter();
BodyComponent? hook = null;
var teamGroups = true;
var chainFiltered = true;
var relaunchIn = RunEvery;

using var game = new Game();

var sceneSetup = new SceneSetup3D
{
    Camera = CameraSetup.Perspective(position: new Vector3(-1f, 7.5f, 14f), rotation: new Vector3(0f, CameraPitch, 0f)).WithController(),
    WindowTitle = "Collision groups - Stride Community Toolkit",
    Resizable = true,
    DpiAware = true,
}.UseBepu().UseSkybox();

game.Run(sceneSetup, start: Start, update: Update);

void Start(Scene scene)
{
    game.AddGrabber();

    for (var i = 0; i < spheres.Length; i++) spheres[i] = BuildSphere(scene, i);

    for (var lane = 0; lane < laneLabels.Length; lane++)
    {
        laneLabels[lane] = AddLabel(scene, LaneText(lane), laneColours[lane], teamsCentre + new Vector3(RunDistance + 1.2f, 0f, (lane - 1) * LaneSpacing), Anchor.MiddleLeft, 0.32f);
    }

    BuildChain(scene);

    stationLabels[0] = AddLabel(scene, "", Color.White, new Vector3(teamsCentre.X, 0f, LaneSpacing + 1.4f), Anchor.BottomCenter, 0.3f);
    stationLabels[1] = AddLabel(scene, "", Color.White, new Vector3(hookPosition.X - 2f, 0f, LaneSpacing + 1.4f), Anchor.BottomCenter, 0.3f);

    ApplyGroups();
    Launch();

    var overlay = TextOverlay.GetOrCreate(game);

    overlay.Position = Anchor.TopLeft;
    overlay.AddSection("Collision groups", BuildOverlayLines);
}

void Update(Scene scene, GameTime time)
{
    // The lanes run again on their own, so the meeting is never more than a few seconds away
    relaunchIn -= (float)time.Elapsed.TotalSeconds;

    if (game.Input.IsKeyPressed(Keys.Space) || relaunchIn <= 0f) Launch();

    // The name tags follow their spheres, and a sphere stops at the far end of its lane, so the lanes stay readable between runs
    for (var i = 0; i < spheres.Length; i++)
    {
        var position = spheres[i].Entity.Transform.Position;

        sphereLabels[i].Entity.Transform.Position = position + tagOffset;

        if (MathF.Abs(position.X - teamsCentre.X) > RunDistance + 0.3f)
        {
            spheres[i].LinearVelocity = Vector3.Zero;
            spheres[i].AngularVelocity = Vector3.Zero;
        }
    }

    // The hook follows a slow sine, so the chain is always bending
    if (hook is not null)
    {
        var target = hookPosition.X + MathF.Sin((float)time.Total.TotalSeconds * HookRate) * HookSwing;

        hook.LinearVelocity = new Vector3((target - hook.Entity.Transform.Position.X) * 8f, 0f, 0f);
        hook.Awake = true;
    }

    // Not T: that is the grabber's rotate key
    if (game.Input.IsKeyPressed(Keys.G))
    {
        teamGroups = !teamGroups;
        ApplyGroups();
        Launch();
    }

    if (game.Input.IsKeyPressed(Keys.C))
    {
        chainFiltered = !chainFiltered;
        linkTouches.Reset();
        ApplyGroups();
    }
}

/// <summary>A lane sphere with its name tag: even indexes start on the left and run right, odd ones the other way; the team is the table's letter.</summary>
BodyComponent BuildSphere(Scene scene, int index)
{
    var entity = game.CreatePrimitive(Primitive3D.Sphere(SphereRadius), new BepuPhysicsOptions
    {
        EntityName = names[index],
        Material = game.CreateMaterial(teams[index] == 'A' ? teamA : teamB, metalness: 0f, glossiness: 0.5f),
        // Bouncy, as E05_3D_PhysicsMaterials explains, so a collision throws the two apart instead of stopping them
        Component = new BodyComponent
        {
            Collider = new CompoundCollider(),
            InterpolationMode = InterpolationMode.Interpolated,
            ContactEventHandler = bounces,
            SpringFrequency = 10f,
            SpringDampingRatio = 0f,
            MaximumRecoveryVelocity = 8f,
        },
        Position = StartPosition(index),
    });

    entity.Scene = scene;

    // The tag is its own entity, following the sphere from the update loop; a child would roll with it
    sphereLabels[index] = AddLabel(scene, "", Color.White, StartPosition(index) + tagOffset, Anchor.BottomCenter);

    return entity.Get<BodyComponent>();
}

/// <summary>
/// A hook and four links end to end. Each link hangs from a ball socket at the bottom of the one above, so the
/// links touch at the sockets without overlapping, and every bend brings their ends together.
/// </summary>
void BuildChain(Scene scene)
{
    var hookEntity = game.CreatePrimitive(Primitive3D.Sphere(0.3f), new BepuPhysicsOptions
    {
        EntityName = "Hook",
        Material = game.CreateMaterial(Color.LightGray, metalness: 0.4f, glossiness: 0.6f),
        Body = BodyMode.Kinematic,
        Position = hookPosition,
    });

    hookEntity.Scene = scene;

    hook = hookEntity.Get<BodyComponent>();

    var above = hook;

    for (var i = 0; i < links.Length; i++)
    {
        var entity = game.CreatePrimitive(Primitive3D.Box(new Vector3(0.225f, LinkLength, 0.225f)), new BepuPhysicsOptions
        {
            EntityName = $"Link {(char)('A' + i)}",
            Material = game.CreateMaterial(linkColour, metalness: 0.2f, glossiness: 0.5f),
            Component = new BodyComponent { Collider = new CompoundCollider(), ContactEventHandler = linkTouches },
            Position = hookPosition + new Vector3(0f, -HookDrop - LinkLength * (i + 0.5f), 0f),
        });

        entity.Scene = scene;

        var link = entity.Get<BodyComponent>();

        links[i] = link;

        // The socket is the top of this link: just under the hook sphere for the first, the bottom of the link above for the rest
        entity.Add(new BallSocketConstraintComponent
        {
            A = above,
            B = link,
            LocalOffsetA = new Vector3(0f, i == 0 ? -HookDrop : -LinkLength * 0.5f, 0f),
            LocalOffsetB = new Vector3(0f, LinkLength * 0.5f, 0f),
        });

        entity.Add(MakeText($"{(char)('A' + i)}: index {i}", linkColour, Anchor.MiddleLeft, 0.22f, new Vector3(0.3f, 0f, 0f)));

        above = link;
    }

    // A chain meant to hang stiff would say so with a constraint, a SwingLimitConstraintComponent on each socket;
    // the stiffness with the filtering off is contacts fighting the sockets, shown here as the problem, not a technique
}

/// <summary>The groups as the two switches say, and the labels that say which: an Id per team or none for the lanes, Id 1 or no filtering for the chain.</summary>
void ApplyGroups()
{
    for (var i = 0; i < spheres.Length; i++)
    {
        var id = (ushort)(teamGroups ? teams[i] - 'A' + 1 : 0);

        spheres[i].CollisionGroup = new CollisionGroup { Id = id };
        sphereLabels[i].Text = $"{names[i]} ({id})";
    }

    for (var lane = 0; lane < laneLabels.Length; lane++) laneLabels[lane].Text = LaneText(lane);

    stationLabels[0].Text = teamGroups ? "1. Teams: same Id passes through, different Id bounces" : "1. Teams: no groups, every pair bounces";
    stationLabels[1].Text = chainFiltered ? "2. Chain: neighbours ignored, bends freely" : "2. Chain: every link collides, bends stiffly";

    for (var i = 0; i < links.Length; i++)
    {
        links[i].CollisionGroup = new CollisionGroup { Id = (ushort)(chainFiltered ? 1 : 0), IndexA = (ushort)i };
        links[i].Awake = true;
    }
}

/// <summary>Every sphere back at its end of the lane and running at the other one.</summary>
void Launch()
{
    bounces.Reset();
    relaunchIn = RunEvery;

    for (var i = 0; i < spheres.Length; i++)
    {
        spheres[i].TeleportAtRest(StartPosition(i));
        spheres[i].LinearVelocity = new Vector3(i % 2 == 0 ? RunSpeed : -RunSpeed, 0f, 0f);
    }
}

Vector3 StartPosition(int index) => teamsCentre + new Vector3(index % 2 == 0 ? -RunDistance : RunDistance, 0f, (index / 2 - 1) * LaneSpacing);

string LaneText(int lane)
{
    var left = teams[lane * 2];
    var right = teams[lane * 2 + 1];

    return $"{left} meets {right}: {(teamGroups && left == right ? "passes through" : "bounces")}";
}

/// <summary>A label on its own entity at a world position, so it keeps the facing it was given whatever rolls beneath it.</summary>
WorldTextComponent AddLabel(Scene scene, string text, Color colour, Vector3 position, Anchor anchor = Anchor.MiddleCenter, float height = 0.25f)
{
    var label = new Entity("Label") { MakeText(text, colour, anchor, height) };

    label.Transform.Position = position;
    label.Transform.Rotation = labelTilt;
    label.Scene = scene;

    return label.Get<WorldTextComponent>();
}

/// <summary>
/// World text, not a billboard: it is a thing in the scene, which scales with distance and turns with the entity it is on.
/// Screen text, the EntityTextComponent, would stay the same size however far the camera goes.
/// </summary>
WorldTextComponent MakeText(string text, Color colour, Anchor anchor, float height, Vector3 offset = default) =>
    new() { Text = text, TextColor = colour, Anchor = anchor, Height = height, Offset = offset, Billboard = false };

IReadOnlyList<TextElement> BuildOverlayLines() =>
[
    new("Space", $"Run the lanes again; they run by themselves every {RunEvery:0} seconds", Color.Gold),
    new("G", $"Team groups {(teamGroups ? "on: A is Id 1, B is Id 2" : "off: every sphere Id 0")}", Color.Gold),
    new("C", $"Chain filtering {(chainFiltered ? "on: Id 1, indexes 0 to 3" : "off: Id 0")}", Color.Gold),
    new("Left mouse", "Grab a sphere or a link", Color.Gold),
    new(""),
    new($"Sphere collisions since the launch: {bounces.Count}", Color.White),
    new($"Link contacts since the switch: {linkTouches.Count}", Color.White),
];

/// <summary>Counts the touches that start between two bodies carrying this handler; the ground is static and does not count.</summary>
sealed class TouchCounter : IContactHandler
{
    public bool NoContactResponse => false;

    internal int Count { get; private set; }

    internal void Reset() => Count = 0;

    void IContactHandler.OnStartedTouching<TManifold>(Contacts<TManifold> contacts)
    {
        // Both bodies carry this handler, so a touch is reported twice; count it from the lower name once
        if (contacts.Other is BodyComponent other && string.CompareOrdinal(contacts.EventSource.Entity.Name, other.Entity.Name) < 0) Count++;
    }
}
/*
---example-metadata
slug: collision-group
title:
  en: Collision Group
level: Intermediate
category: Physics
complexity: 4
order: 80
screenshotFrame: 75
description:
  en: |-
    Collision groups are a number rule inside a layer: bodies with the same group Id ignore each other
    when their indexes are the same or adjacent, and collide when two or more apart. Two stations show
    the two uses: three lanes of named spheres running at each other, where A meets A and B meets B
    pass through and A meets B bounces, and a chain of four links end to end on a swinging hook, with
    indexes 0 to 3, where the ignored neighbours let it bend freely and links further apart still
    collide. Keys switch the teams' groups off, so every lane bounces, and the chain's filtering off,
    so the touching links collide at every bend. Groups suit rules that follow a formula; the collision
    layer example is the table-driven choice.
concepts:
  - Giving bodies a collision group Id and an index
  - "The rule: same Id, indexes less than two apart, no collision"
  - Teams that pass through each other but bounce off opponents
  - A chain whose neighbouring links bend without colliding
  - Counting collisions with a contact handler
  - When an index rule beats a layer matrix
  - "Using helpers: SceneSetup3D, UseBepu, UseSkybox, CreatePrimitive, AddGrabber, TeleportAtRest, TextOverlay, WorldTextComponent"
tags:
  - 3D
  - Bepu
  - Physics
  - Collision
  - Collision Group
  - Filtering
  - Constraints
related:
  - E05_3D_CollisionLayer
  - E05_3D_Constraints_Rope
  - E05_3D_TriggerZones
enabled: true
created: 2025-03-08
---
*/