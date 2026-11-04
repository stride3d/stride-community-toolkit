using Stride.BepuPhysics;
using Stride.BepuPhysics.Definitions;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Rendering.Overlay;
using Stride.CommunityToolkit.Rendering.Text;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Input;

// A collider made from real geometry, two ways. A convex hull wraps the model in the smallest convex
// shape that holds it, so a ring becomes a disc: the hole is filled. A mesh collider is the model's
// own triangles, so the hole stays a hole. Four stations show what each one does, and what a mesh
// is not:
//
//   1. Hull ring, a ball dropped at the centre: the ball rests on a lid that is not drawn. P shows it.
//   2. Mesh ring, the same drop: the ball falls through the hole to the ground.
//   3. Mesh sphere, one ball dropped onto it and one started inside it. A mesh is a surface with no
//      inside: the outer ball rests on top, and the overlay says what became of the inner one.
//   4. A moving mesh ring dropped onto a mesh ring: meshes do meet, triangle against triangle.
//
// So the rule: a mesh collider is for what the scene is made of, the level, the ground, a bowl, and
// it is usually static. What moves is better off convex, a hull or a compound of several, because a
// convex pair is cheap and a convex shape has an inside.
//
// A mesh collider is built from the model, so it is attached once the model exists. The entity's
// scale scales the mesh with it. Closed says the surface has no holes, which is what the inertia of a
// moving mesh is computed from; the ring is closed, a bowl or a terrain is not.

const float RingRadius = 1.2f;      // From the centre to the middle of the tube
const float TubeRadius = 0.35f;     // So the hole is 0.85 across from the centre, and the ball fits
const float ShellRadius = 1.2f;
const float BallRadius = 0.4f;
const float RestHeight = 2.5f;      // The static shapes float here: a static needs no support
const float DropHeight = 3f;        // Above the shape
const float StationSpacing = 3.8f;

var hullColour = new Color(240, 160, 70);
var meshColour = new Color(90, 190, 200);
var ballColour = new Color(235, 235, 240);

List<DropStation> stations = [];
var launched = false;

using var game = new Game();

var sceneSetup = new SceneSetup3D
{
    // Above and in front of the stations, shifted right so they stay clear of the overlay
    Camera = CameraSetup.Perspective(position: new Vector3(4.8f, 9.5f, 15.5f), rotation: new Vector3(0f, -30f, 0f)).WithController(),
    Profiler = true,
    WindowTitle = "Mesh colliders - Stride Community Toolkit",
    Resizable = true,
    DpiAware = true,
}.UseBepu().UseSkybox();

game.Run(sceneSetup, start: Start, update: Update);

void Start(Scene scene)
{
    // P draws every collider, so the hull's lid and the mesh's triangles can be seen
    game.AddPhysicsDebug();

    var x = -1.5f * StationSpacing;

    BuildRingStation(scene, "1. Hull", x, mesh: false);
    BuildRingStation(scene, "2. Mesh", x + StationSpacing, mesh: true);
    BuildShellStation(scene, "3. Mesh shell", x + 2 * StationSpacing);
    BuildMovingRingStation(scene, "4. Mesh on mesh", x + 3 * StationSpacing);

    TextOverlay.GetOrCreate(game).AddSection("Mesh colliders", BuildOverlayLines);
}

void Update(Scene scene, GameTime time)
{
    // A teleport only takes on a body the simulation already knows, so the first drop waits for it
    var ready = stations.Count > 0 && stations[^1].Body.Simulation is not null;

    if (ready && (!launched || game.Input.IsKeyPressed(Keys.R)))
    {
        launched = true;

        foreach (var station in stations) station.Replay();
    }
}

/// <summary>A static ring with the collider the station is about, and a ball dropped at its centre.</summary>
void BuildRingStation(Scene scene, string label, float x, bool mesh)
{
    AddRing(scene, new Vector3(x, RestHeight, 0f), mesh, moving: false);

    var ball = AddBall(scene, new Vector3(x, RestHeight + DropHeight, 0f));

    stations.Add(new DropStation(label, ball, mesh ? meshColour : hullColour,
        static y => y > RestHeight ? "on the lid" : "through the hole"));

    AddLabel(scene, label, x);
}

/// <summary>A static mesh sphere, hollow as every mesh is, with a ball dropped onto it and a ball started inside it.</summary>
void BuildShellStation(Scene scene, string label, float x)
{
    var shell = game.CreatePrimitive(Primitive3D.Sphere(ShellRadius), new BepuPhysicsOptions
    {
        Material = game.CreateMaterial(meshColour, metalness: 0f, glossiness: 0.4f),
        Body = BodyMode.None,
        Position = new Vector3(x, RestHeight, 0f),
    });

    shell.Add(new StaticComponent { Collider = new MeshCollider { Model = shell.Get<ModelComponent>().Model, Closed = true } });
    shell.Scene = scene;

    var outer = AddBall(scene, new Vector3(x, RestHeight + DropHeight, 0f));
    var inner = AddBall(scene, new Vector3(x, RestHeight, 0f));

    stations.Add(new DropStation("3. Shell, outside", outer, meshColour,
        static y => y > RestHeight + ShellRadius ? "on top" : "through it"));

    stations.Add(new DropStation("3. Shell, inside", inner, meshColour,
        static y => y > RestHeight - ShellRadius ? "still inside" : "fell out"));

    AddLabel(scene, label, x);
}

/// <summary>A static mesh ring, and a moving mesh ring dropped onto it.</summary>
void BuildMovingRingStation(Scene scene, string label, float x)
{
    AddRing(scene, new Vector3(x, RestHeight, 0f), mesh: true, moving: false);

    var ring = AddRing(scene, new Vector3(x, RestHeight + DropHeight, 0f), mesh: true, moving: true);

    stations.Add(new DropStation(label, ring.Get<BodyComponent>(), meshColour,
        static y => y > RestHeight ? "on the ring" : "through it, on the ground"));

    AddLabel(scene, label, x);
}

/// <summary>
/// One ring of the shared model. A hull ring gets <see cref="TorusCollider"/>, the convex hull of the
/// same mesh, inside a compound like any other shape; a mesh ring gets a <see cref="MeshCollider"/>
/// built from its model once the model exists.
/// </summary>
Entity AddRing(Scene scene, Vector3 position, bool mesh, bool moving)
{
    var options = new BepuPhysicsOptions
    {
        Material = game.CreateMaterial(mesh ? meshColour : hullColour, metalness: 0f, glossiness: 0.4f),
        Position = position,
    };

    if (mesh)
    {
        // The mesh collider needs the model, so the physics is attached below
        options.Body = BodyMode.None;
    }
    else
    {
        var hull = new CompoundCollider { Colliders = { TorusCollider.Create(RingRadius, TubeRadius) } };

        options.Component = moving
            ? new BodyComponent { Collider = hull, InterpolationMode = InterpolationMode.Interpolated }
            : new StaticComponent { Collider = hull };
    }

    var ring = game.CreatePrimitive(Primitive3D.Torus(RingRadius, TubeRadius), options);

    if (mesh)
    {
        // The model is all the collider needs: when the component attaches, Bepu reads the model's vertex
        // and index buffers and builds the triangles from them, scaled by the entity's transform. No
        // shape is written by hand and no asset is involved, so any model will do, loaded or procedural
        var collider = new MeshCollider { Model = ring.Get<ModelComponent>().Model, Closed = true };

        ring.Add(moving
            ? new BodyComponent { Collider = collider, InterpolationMode = InterpolationMode.Interpolated }
            : new StaticComponent { Collider = collider });
    }

    ring.Scene = scene;

    return ring;
}

/// <summary>A ball with the default sphere collider, drawn between physics steps too so its fall is smooth.</summary>
BodyComponent AddBall(Scene scene, Vector3 position)
{
    var ball = game.CreatePrimitive(Primitive3D.Sphere(BallRadius), new BepuPhysicsOptions
    {
        Material = game.CreateMaterial(ballColour, metalness: 0f, glossiness: 0.6f),
        Position = position,
    });

    ball.Scene = scene;

    return ball.Get<BodyComponent>();
}

/// <summary>The station's name on the ground in front of it.</summary>
void AddLabel(Scene scene, string text, float x)
{
    var label = new Entity("Label")
    {
        new EntityTextComponent { Text = text, FontSize = 14, Anchor = Anchor.MiddleCenter, EnableShadow = true },
    };

    label.Transform.Position = new Vector3(x, 0f, RingRadius + 0.8f);
    label.Scene = scene;
}

IReadOnlyList<TextElement> BuildOverlayLines()
{
    List<TextElement> lines =
    [
        new("R", "Drop everything again", Color.Gold),
        new("P", "Draw the collider gizmos", Color.Gold),
        new("F11", "Draw the debug meshes", Color.Gold),
        new(""),
        new("Where each came to rest", Color.LightGray),
    ];

    foreach (var station in stations)
    {
        lines.Add(new(station.Line, station.Colour));
    }

    return lines;
}

/// <summary>
/// The thing a station drops, a ball or a ring, where it starts, and the <paramref name="Verdict"/> its
/// height earns once it has stopped.
/// </summary>
sealed record DropStation(string Label, BodyComponent Body, Color Colour, Func<float, string> Verdict)
{
    /// <summary>Where the body was made, which is where it goes back to.</summary>
    internal Vector3 Start { get; } = Body.Entity.Transform.Position;

    /// <summary>The label, the height, and the verdict, or "falling" while the body still moves.</summary>
    internal string Line
    {
        get
        {
            var y = Body.Position.Y;
            var verdict = Body.LinearVelocity.LengthSquared() > 0.01f ? "falling" : Verdict(y);

            return $"{Label}: {y:0.00} m, {verdict}";
        }
    }

    /// <summary>Puts the body back where it started, still.</summary>
    internal void Replay() => Body.TeleportAtRest(Start);
}

/*
---example-metadata
slug: mesh-colliders
title:
  en: Mesh Colliders
  cs: Síťové kolidery
level: Beginner
category: Physics
complexity: 4
order: 47
description:
  en: |-
    A collider made from real geometry, two ways: a convex hull wraps the model in the smallest convex
    shape that holds it, so a ring becomes a disc, and a mesh collider is the model's own triangles,
    so the hole stays a hole. A ball rests on the hull's invisible lid and falls through the mesh's
    hole; a mesh sphere shows that a mesh is a surface with no inside; a moving mesh ring lands on a
    mesh ring. The overlay prints where each dropped thing came to rest, and P draws the colliders.
  cs: |-
    Kolider z opravdové geometrie, dvěma způsoby: konvexní obal zabalí model do nejmenšího konvexního
    tvaru, který ho obsáhne, takže z prstence je disk, a síťový kolider jsou trojúhelníky modelu samotné,
    takže díra zůstane dírou. Míč zůstane ležet na neviditelném víku obalu a propadne dírou sítě; síťová
    koule ukazuje, že síť je jen povrch bez vnitřku; padající síťový prstenec přistane na síťovém
    prstenci. Překryv vypisuje, kde se každá puštěná věc zastavila, a P vykreslí kolidery.
concepts:
  - ConvexHullCollider from a model through TorusCollider, and what a hull does to a hole
  - MeshCollider from a model, attached once the model exists, Closed for the inertia
  - A mesh is a surface with no inside, so a body that starts inside is not pushed out
  - Meshes meet meshes, triangle against triangle, which is why what moves is better off convex
  - Reading where a body came to rest from its position and velocity, with Teleport for the replay
  - "Using helpers: SceneSetup3D, CreatePrimitive, TorusCollider, AddPhysicsDebug, TextOverlay"
tags:
  - 3D
  - Physics
  - Bepu
  - Colliders
  - Mesh
  - Convex Hull
related:
  - E05_3D_PhysicsMaterials
  - E05_3D_Raycast
  - E07_3D_ProceduralGeometry
screenshotFrame: 180
enabled: true
created: 2026-10-16
---
*/