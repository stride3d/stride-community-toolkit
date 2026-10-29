using Stride.BepuPhysics;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.BepuPhysics.Definitions.Contacts;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering;
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

// Collision layers. A body sits on one layer, and a matrix on the simulation says which pairs of layers
// collide. Four layers here - Players, Enemies, Shots, Debris - and the matrix shown twice: as a grid on
// screen, and as the scene itself. Each lane is a shelf on one layer, and a cube of every layer is dropped
// onto every shelf: a cube whose layer collides with the shelf's stays on it, any other falls through to
// the ground. Keys 1 to 0, or a click on a cell of the grid, flip one pair of the matrix, and the cubes drop again.
//
// A right click fires a small body on the Shots layer from the camera at the cursor, so the matrix is seen a
// third way: the shot flies through whatever its layer ignores and is stopped by whatever it collides with,
// a ring marks each hit, and its trail stays behind for a moment to show the path.
//
// Layers are the readable choice when the rules are a table: one value on a body, one answer per pair.
// E05_3D_CollisionGroup is the other tool, for rules that follow a formula within a layer. A raycast's
// mask is the same idea for queries: M cycles which layers the mouse ray can hit.

const float LaneSpacing = 3.2f;
const float RowSpacing = 2.2f;
const float ShelfHeight = 1.8f;
const float DropHeight = 4f;
const float CubeSize = 0.8f;
const float ShotRadius = 0.15f;
const float ShotSpeed = 20f;
const float ShotLife = 2.5f;

string[] names = ["Players", "Enemies", "Shots", "Debris"];
CollisionLayer[] layers = [CollisionLayer.Layer1, CollisionLayer.Layer2, CollisionLayer.Layer3, CollisionLayer.Layer4];
Color[] colours = [new(90, 200, 120), new(230, 80, 80), new(255, 200, 60), new(160, 160, 170)];

// Every pair once, the layer with itself included, in the order the keys 1 to 0 flip them
(int A, int B)[] pairs = [(0, 0), (0, 1), (0, 2), (0, 3), (1, 1), (1, 2), (1, 3), (2, 2), (2, 3), (3, 3)];
Keys[] pairKeys = [Keys.D1, Keys.D2, Keys.D3, Keys.D4, Keys.D5, Keys.D6, Keys.D7, Keys.D8, Keys.D9, Keys.D0];

// What the mouse ray may hit: everything, everything but the enemies, or the players alone
(string Name, CollisionMask Mask)[] rayMasks =
[
    ("everything", CollisionMask.Everything),
    ("all but Enemies", CollisionMask.Everything & ~CollisionMask.Layer2),
    ("Players only", CollisionMask.Layer1),
];

var cubes = new BodyComponent[4, 4];

// The grid cells' pick tags, boxed once; a cell's tag is its row times four plus its column
object[] cellTags = [.. Enumerable.Range(0, 16).Select(i => (object)i)];
var shots = new List<(Entity Entity, Vector3 Start, float Age)>();
var hits = new ShotHits();
Material shotMaterial = null!;
var rayMask = 0;
string rayHit = "nothing";
BepuSimulation simulation = null!;
ShapeBatch shapes = null!;
EntityTextComponent[] gridLabels = null!;

using var game = new Game();

var sceneSetup = new SceneSetup3D
{
    Camera = CameraSetup.Perspective(position: new Vector3(2f, 5.5f, 17.5f), rotation: new Vector3(0f, -15f, 0f)).WithController(),
    WindowTitle = "Collision layers - Stride Community Toolkit",
    Resizable = true,
    DpiAware = true,
}.UseBepu().UseSkybox();

game.Run(sceneSetup, start: Start, update: Update);

void Start(Scene scene)
{
    simulation = game.GetCameraEntity().GetSimulation();
    shapes = game.AddShapeBatch();

    game.AddEntityTextRenderer();
    game.AddGrabber();

    shotMaterial = game.CreateMaterial(colours[2], metalness: 0f, glossiness: 0.6f);

    // The starting rules: players walk through enemies, shots hit enemies but not players, debris
    // touches only itself and the ground. Every pair not set here collides, which is the default.
    simulation.CollisionMatrix.Set(layers[0], layers[1], shouldCollide: false);
    simulation.CollisionMatrix.Set(layers[0], layers[2], shouldCollide: false);
    simulation.CollisionMatrix.Set(layers[0], layers[3], shouldCollide: false);
    simulation.CollisionMatrix.Set(layers[1], layers[3], shouldCollide: false);
    simulation.CollisionMatrix.Set(layers[2], layers[2], shouldCollide: false);
    simulation.CollisionMatrix.Set(layers[2], layers[3], shouldCollide: false);

    for (var lane = 0; lane < 4; lane++)
    {
        BuildShelf(scene, lane);

        for (var row = 0; row < 4; row++) cubes[row, lane] = BuildCube(scene, row, lane);
    }

    gridLabels = [.. Enumerable.Range(0, 8).Select(i => AddGridLabel(scene, i))];

    var overlay = TextOverlay.GetOrCreate(game);

    overlay.Position = Anchor.TopLeft;

    overlay.AddSection("Collision layers", BuildOverlayLines);
    overlay.AddCollapsibleSection("Pairs", "The pairs, by key", Keys.L, BuildPairLines, collapsed: true);
}

void Update(Scene scene, GameTime time)
{
    for (var i = 0; i < pairs.Length; i++)
    {
        if (!game.Input.IsKeyPressed(pairKeys[i])) continue;

        var (a, b) = pairs[i];

        FlipPair(a, b);
    }

    if (game.Input.IsMouseButtonPressed(MouseButton.Left) && shapes.TryPick(game.Input.MousePosition, out var cell))
    {
        var index = (int)cell.Tag;

        FlipPair(index / 4, index % 4);
    }

    if (game.Input.IsKeyPressed(Keys.Space) || game.Input.IsKeyPressed(Keys.R)) DropCubes();

    if (game.Input.IsKeyPressed(Keys.M)) rayMask = (rayMask + 1) % rayMasks.Length;

    if (game.Input.IsMouseButtonPressed(MouseButton.Right)) Shoot(scene);

    var elapsed = (float)time.Elapsed.TotalSeconds;

    AgeShots(elapsed);
    hits.Draw(shapes, elapsed, colours[2]);
    CastMouseRay();
    DrawGrid();
}

/// <summary>A floating slab on the lane's layer; what falls through it lands on the ground below.</summary>
void BuildShelf(Scene scene, int lane)
{
    var shelf = game.CreatePrimitive(Primitive3D.Box(new Vector3(2.6f, 0.2f, 4 * RowSpacing)), new BepuPhysicsOptions
    {
        EntityName = $"{names[lane]} shelf",
        Material = game.CreateMaterial(colours[lane] * 0.75f, metalness: 0f, glossiness: 0.3f),
        Body = BodyMode.Static,
        Position = new Vector3(LaneX(lane), ShelfHeight, 0f),
    });

    shelf.Get<StaticComponent>().CollisionLayer = layers[lane];
    shelf.Scene = scene;

    AddLabel(scene, $"{names[lane]} shelf", colours[lane], new Vector3(LaneX(lane), ShelfHeight + 0.45f, 2 * RowSpacing + 0.9f));
}

/// <summary>A cube on the row's layer, over the lane's shelf: the scene's own copy of one matrix cell.</summary>
BodyComponent BuildCube(Scene scene, int row, int lane)
{
    var cube = game.CreatePrimitive(Primitive3D.Cube(CubeSize), new BepuPhysicsOptions
    {
        EntityName = $"{names[row]} cube, {names[lane]} lane",
        Material = game.CreateMaterial(colours[row], metalness: 0f, glossiness: 0.4f),
        Position = StartPosition(row, lane),
    });

    var body = cube.Get<BodyComponent>();

    body.CollisionLayer = layers[row];
    cube.Scene = scene;

    return body;
}

/// <summary>One pair of the matrix flipped, and the cubes dropped again so the scene shows the new rule.</summary>
void FlipPair(int a, int b)
{
    // The matrix is read as the solver meets each pair, so a change applies to the next drop
    simulation.CollisionMatrix.Set(layers[a], layers[b], !simulation.CollisionMatrix.Get(layers[a], layers[b]));
    DropCubes();
}

/// <summary>Every cube back over its shelf, still, so the current matrix decides where it lands.</summary>
void DropCubes()
{
    for (var row = 0; row < 4; row++)
    {
        for (var lane = 0; lane < 4; lane++) cubes[row, lane].TeleportAtRest(StartPosition(row, lane));
    }
}

/// <summary>A small body on the Shots layer, fired from the camera along the mouse ray; the matrix, not the mask, says what stops it.</summary>
void Shoot(Scene scene)
{
    var ray = game.GetCameraEntity().Get<CameraComponent>().ScreenToWorldRay(game.Input.MousePosition);
    var start = ray.Position + ray.Direction;

    var shot = game.CreatePrimitive(Primitive3D.Sphere(ShotRadius), new BepuPhysicsOptions
    {
        EntityName = "Shot",
        Material = shotMaterial,
        // No gravity and continuous detection, so a fast small body flies straight and cannot skip a thin shelf
        Component = new BodyComponent
        {
            Collider = new CompoundCollider(),
            ContactEventHandler = hits,
            Gravity = false,
            ContinuousDetectionMode = BepuPhysics.Collidables.ContinuousDetectionMode.Continuous,
        },
        Position = start,
    });

    var body = shot.Get<BodyComponent>();

    body.CollisionLayer = layers[2];
    shot.Scene = scene;
    body.LinearVelocity = ray.Direction * ShotSpeed;

    shots.Add((shot, start, 0f));
}

/// <summary>Each shot's trail from its muzzle to where it is now, and the shot itself gone once its time is up.</summary>
void AgeShots(float elapsed)
{
    for (var i = shots.Count - 1; i >= 0; i--)
    {
        var (entity, start, age) = shots[i];

        age += elapsed;

        if (age > ShotLife)
        {
            entity.Scene = null;
            shots.RemoveAt(i);
            continue;
        }

        shots[i] = (entity, start, age);
        shapes.DrawPixelLine(start, entity.Transform.Position, 1.5f, colours[2]);
    }
}

/// <summary>The mouse ray under the current mask: what it hits is named in the overlay and ringed on the ground.</summary>
void CastMouseRay()
{
    var camera = game.GetCameraEntity().Get<CameraComponent>();

    if (camera.Raycast(game.Input.MousePosition, 100f, out var hit, rayMasks[rayMask].Mask))
    {
        rayHit = hit.Collidable.Entity.Name;

        shapes.BorderWidth = 2f;
        shapes.DrawRing(hit.Point + Vector3.UnitY * 0.02f, Vector3.UnitY, 0.5f, Color.White);
    }
    else
    {
        rayHit = "nothing";
    }
}

/// <summary>The matrix as a grid in the bottom-right corner: a green cell collides, a dark one passes through, and the cell under the mouse is lit.</summary>
void DrawGrid()
{
    const float Cell = 46f;

    var origin = shapes.AnchorPoint(Anchor.BottomRight) - new Vector2(3 * Cell + 30f, 40f);
    var hovered = shapes.TryPick(game.Input.MousePosition, out var hit) ? (int)hit.Tag : -1;

    shapes.Screen = true;
    shapes.BorderWidth = 1f;

    for (var row = 0; row < 4; row++)
    {
        for (var column = 0; column < 4; column++)
        {
            var index = row * 4 + column;
            var collides = simulation.CollisionMatrix.Get(layers[row], layers[column]);
            var centre = origin + new Vector2(column * Cell, -(3 - row) * Cell);
            var colour = collides ? new Color(90, 200, 120) : new Color(70, 40, 45);

            // Tagged, so a click can find the cell again
            shapes.Tag = cellTags[index];
            shapes.DrawRectangle(centre, new Vector2(Cell - 4f, Cell - 4f), index == hovered ? colour * 1.4f : colour, cornerRadius: 4f);
        }
    }

    shapes.Tag = null;
    shapes.Screen = false;

    // The labels follow the grid, so a resized window keeps them beside it
    for (var i = 0; i < 4; i++)
    {
        gridLabels[i].ScreenPosition = origin + new Vector2(-Cell * 0.5f - 8f, -(3 - i) * Cell);
        gridLabels[4 + i].ScreenPosition = origin + new Vector2(i * Cell, -3.5f * Cell - 6f);
    }
}

EntityTextComponent AddGridLabel(Scene scene, int index)
{
    var row = index < 4;
    var layer = index % 4;

    var label = new EntityTextComponent
    {
        Text = names[layer],
        FontSize = 12,
        TextColor = colours[layer],
        PositionMode = TextPositionMode.Screen,
        Anchor = row ? Anchor.MiddleRight : Anchor.BottomCenter,
        EnableShadow = true,
    };

    scene.Entities.Add(new Entity("Grid label") { label });

    return label;
}

void AddLabel(Scene scene, string text, Color colour, Vector3 position)
{
    var label = new Entity("Label")
    {
        new EntityTextComponent { Text = text, FontSize = 13, TextColor = colour, Anchor = Anchor.MiddleCenter, EnableShadow = true },
    };

    label.Transform.Position = position;
    label.Scene = scene;
}

IReadOnlyList<TextElement> BuildOverlayLines()
{
    var lines = new List<TextElement>
    {
        new("1 to 0, or a click on a cell", "Flip a pair of the matrix, and drop the cubes again", Color.Gold),
        new("Space", "Drop the cubes again", Color.Gold),
        new("M", $"Mouse ray hits {rayMasks[rayMask].Name}: now {rayHit}", Color.Gold),
        new("Right mouse", $"Shoot at the cursor from the Shots layer: last hit {hits.Last}", Color.Gold),
        new("Left mouse", "Grab a cube and drop it on another shelf", Color.Gold),
    };

    return lines;
}

IReadOnlyList<TextElement> BuildPairLines()
{
    var lines = new List<TextElement>();

    for (var i = 0; i < pairs.Length; i++)
    {
        var (a, b) = pairs[i];
        var collides = simulation.CollisionMatrix.Get(layers[a], layers[b]);

        lines.Add(new($"{(i + 1) % 10}  {names[a]} and {names[b]}: {(collides ? "collide" : "pass through")}", collides ? colours[a] : Color.Gray));
    }

    return lines;
}

static float LaneX(int lane) => (lane - 1.5f) * LaneSpacing;

static float RowZ(int row) => (row - 1.5f) * RowSpacing;

static Vector3 StartPosition(int row, int lane) => new(LaneX(lane), DropHeight, RowZ(row));

/// <summary>Where the shots land: a ring that grows for a moment at each new contact, and the name of the last thing hit.</summary>
sealed class ShotHits : IContactHandler
{
    private const float MarkLife = 1.5f;

    private readonly (Vector3 Point, Vector3 Normal, float Age)[] _marks = new (Vector3, Vector3, float)[8];
    private int _next;

    public ShotHits()
    {
        for (var i = 0; i < _marks.Length; i++) _marks[i].Age = MarkLife;
    }

    public bool NoContactResponse => false;

    internal string Last { get; private set; } = "nothing yet";

    void IContactHandler.OnStartedTouching<TManifold>(Contacts<TManifold> contacts)
    {
        Last = contacts.Other.Entity.Name;

        var contact = contacts.GetEnumerator();

        if (!contact.MoveNext()) return;

        _marks[_next] = (contact.Current.Point, contact.Current.Normal, 0f);
        _next = (_next + 1) % _marks.Length;
    }

    internal void Draw(ShapeBatch shapes, float elapsed, Color colour)
    {
        for (var i = 0; i < _marks.Length; i++)
        {
            ref var mark = ref _marks[i];

            if (mark.Age >= MarkLife) continue;

            mark.Age += elapsed;
            shapes.BorderWidth = 2f;
            shapes.DrawRing(mark.Point, mark.Normal, 0.3f + mark.Age * 0.3f, colour);
        }
    }
}
/*
---example-metadata
slug: collision-layer
title:
  en: Collision Layer
level: Intermediate
category: Physics
complexity: 4
order: 90
description:
  en: |-
    Four layers, Players, Enemies, Shots and Debris, and the collision matrix that says which pairs
    collide, shown twice: as a grid on screen and as the scene itself. Each lane is a shelf on one layer
    and a cube of every layer is dropped onto every shelf, so a cube whose layer collides with the shelf's
    stays on it and any other falls through to the ground. Keys, or clicks on the grid, flip one pair at a
    time and the cubes drop again; the mouse ray shows the same layers filtering a raycast through its mask, and a right click
    fires a small body on the Shots layer from the camera, which flies through whatever its layer ignores
    and is stopped by whatever it collides with. Layers are the readable choice when the rules are a
    table; the collision group example is the tool for rules that follow a formula.
concepts:
  - Putting a body on a collision layer
  - Filling in the simulation's collision matrix pair by pair, and changing it while the game runs, by key or by a click on the grid
  - Seeing every cell of the matrix as a physical outcome, on a shelf or through it
  - Filtering a raycast with a CollisionMask built from the same layers
  - Firing a body on one layer and watching the matrix decide what it flies through
  - When to prefer a layer matrix over a collision group's index rule
  - "Using helpers: SceneSetup3D, UseBepu, UseSkybox, CreatePrimitive, AddShapeBatch, AddEntityTextRenderer, AddGrabber, TeleportAtRest, ScreenToWorldRay, TextOverlay"
tags:
  - 3D
  - Bepu
  - Physics
  - Collision
  - Collision Layer
  - Filtering
  - Raycast
related:
  - E05_3D_CollisionGroup
  - E05_3D_Raycast
  - E05_3D_TriggerZones
enabled: true
created: 2025-03-09
---
*/