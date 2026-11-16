using Stride.BepuPhysics;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Input;
using Stride.CommunityToolkit.Rendering.Instancing;
using Stride.CommunityToolkit.Rendering.Overlay;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Rendering.Text;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Input;
using Stride.Rendering;

// Thousands of 2D bodies in two draw calls - awake bodies through one master entity, sleeping
// bodies tinted green through another - with the shape, the batch size and the spawn layout all
// switchable while it runs.
//
// Each sleep state is drawn by one master entity, so every body shares one of two Models - shapes
// cannot be mixed. Changing shape clears and respawns the pile; changing the layout or the batch
// size only affects what is spawned next.

Vector3 wallSize = new(1, 65, 1);
const float WallWidth = 110;

// A body further than this from the pile, sideways or below, has escaped and is removed
const float EscapeDistance = 100f;
const float SpawnHeight = 150;
const float ColumnWidth = WallWidth - 30;

// How far the grabber and the arena pick reach; the camera sits well back from a pile this size
const float GrabberDistance = 400;

// Radians a wall or ramp turns for a drag across the whole window
const float ArenaRotationSensitivity = 4f;

// The sleeping tint, built once
Material? sleepingMaterial = null;

Primitive3D[] shapes =
[
    Primitive3D.Sphere(),
    Primitive3D.Cube(),
    Primitive3D.Capsule(),
    Primitive3D.Cylinder(),
    Primitive3D.Box(Vector3.One),
    Primitive3D.Cone(),
    Primitive3D.TriangularPrism(Vector3.One),
    Primitive3D.Torus(),
    Primitive3D.Teapot(),
];

int[] batchSizes = [1000, 2500, 5000, 10000, 20000];

var random = new Random(1);

// Every body of the pile, with which master draws it right now
List<PileBody> bodies = [];

CameraComponent camera = null!;
Scene scene = null!;
Entity awakeMaster = null!;
Entity sleepingMaster = null!;

// The two walls and the two ramps, under one parent so the whole arena turns as one; each remembers the
// rotation it was built with, for the reset
Entity arena = null!;
Entity? draggedPart = null;
List<(Entity Part, Quaternion InitialRotation)> arenaParts = [];

var sleepingCount = 0;

var shape = Primitive3D.Sphere();
var layout = SpawnLayout.Grid;
var batchSize = 5000;

List<KeyMenu> menus = [];

using var game = new Game();

// The buffered instancings own their GPU buffers, and the engine never releases user-owned buffers
using var awakeInstancing = new BufferedEntityInstancing(new BepuEntityInstancing());
using var sleepingInstancing = new BufferedEntityInstancing(new BepuEntityInstancing());

game.Run(new SceneSetup3D
{
    // A head-on view of the XY plane; the light is aimed by hand below, so the setup adds none
    Camera = CameraSetup.Perspective(position: new Vector3(0, 0, 80), rotation: Vector3.Zero).WithController(),
    Lighting = null,
    Ground = null,
    Profiler = true,
    WindowTitle = "Stress Pile Example - Stride Community Toolkit",
    Resizable = true,
}.UseBepu().UseSkybox(), start: Start, update: Update);

void Start(Scene rootScene)
{
    // Left mouse grabs any body, drags it and throws it on release
    game.AddGrabber().MaxDistance = GrabberDistance;

    scene = rootScene;

    // The default aim shines toward +Z, which leaves the faces turned towards the camera unlit
    var light = game.AddDirectionalLight();
    light.Transform.Rotation = Quaternion.RotationX(MathUtil.DegreesToRadians(-30)) * Quaternion.RotationY(MathUtil.DegreesToRadians(-30));

    arena = new Entity("Arena") { Scene = scene };

    AddArenaPart(new Vector3(-WallWidth / 2, 0, 0), wallSize);
    AddArenaPart(new Vector3(WallWidth / 2, 0, 0), wallSize);

    // The ramps run from the foot of each wall and stop short of the middle, which leaves a hole at the
    // bottom of the arena on purpose
    AddArenaPart(new Vector3(-28.543f, -48.114f, 0), new Vector3(62.3f, 1, 1), Quaternion.RotationZ(MathUtil.DegreesToRadians(-31)));
    AddArenaPart(new Vector3(28.543f, -48.114f, 0), new Vector3(62.3f, 1, 1), Quaternion.RotationZ(MathUtil.DegreesToRadians(31)));

    SetupInstancing();
    SetupMenus();

    SpawnBatch(batchSize);

    camera = game.GetCameraEntity().Get<CameraComponent>();
}

// A wall or a ramp: a static box, a child of the arena
void AddArenaPart(Vector3 position, Vector3 size, Quaternion? rotation = null)
{
    var part = game.CreatePrimitive(Primitive3D.Box(size), new BepuPhysicsOptions
    {
        Material = game.CreateMaterial(Color.LightGray),
        Body = BodyMode.Static,
        Position = position,
        EntityName = "Arena part",
    });

    part.Transform.Rotation = rotation ?? Quaternion.Identity;

    arena.AddChild(part);
    arenaParts.Add((part, part.Transform.Rotation));
}

void SetupInstancing()
{
    // One master per sleep state for the whole run. Their Models are swapped when the shape changes;
    // the instancing objects are reused, because they grow their own buffers and retire the old ones
    // safely. The sleeping master is where the sleep skip pays off: once the pile settles it holds
    // every body, all asleep, and its gather and upload both stop.
    awakeMaster = new Entity("AwakeMaster")
    {
        new ModelComponent(ModelFor(shape)),
        new InstancingComponent { Type = awakeInstancing }
    };

    sleepingMaster = new Entity("SleepingMaster")
    {
        new ModelComponent(SleepingModelFor(shape)),
        new InstancingComponent { Type = sleepingInstancing }
    };

    awakeMaster.Scene = scene;
    sleepingMaster.Scene = scene;

    // Registers with the graphics compositor, not the scene, so it outlives anything in the scene
    game.AddInstancingBufferUpload(awakeInstancing);
    game.AddInstancingBufferUpload(sleepingInstancing);
}

// One Model per shape and sleep state, built on first use and kept. Eighteen of them cost under a
// megabyte, and it makes switching back to a shape you have already used free.

/// <summary>Returns the shared model for a shape, built once on first use and kept by the game.</summary>
Model ModelFor(Primitive3D type) => game.GetOrCreateModel(type);

/// <summary>Returns the shared sleeping-tint model for a shape, built once on first use and kept by the game.</summary>
Model SleepingModelFor(Primitive3D type) => game.GetOrCreateModel(type, sleepingMaterial ??= game.CreateMaterial(Color.MediumSeaGreen));

void SetupMenus()
{
    menus =
    [
        new KeyMenu
        {
            Title = "Shape",
            ToggleKey = Keys.C,
            TitleColor = Color.Yellow,
            SelectedIndex = Array.IndexOf(shapes, shape),
            Items = [.. shapes.Index().Select(pair => new KeyMenuItem(
                (Keys)(Keys.D1 + pair.Index), pair.Item.ToString(), () => ChangeShape(pair.Item)))]
        },
        new KeyMenu
        {
            Title = "Layout",
            ToggleKey = Keys.L,
            TitleColor = Color.Yellow,
            SelectedIndex = (int)layout,
            Items =
            [
                new(Keys.D1, "Grid (even)", () => layout = SpawnLayout.Grid),
                new(Keys.D2, "Random", () => layout = SpawnLayout.Random),
            ]
        },
        new KeyMenu
        {
            Title = "Batch",
            ToggleKey = Keys.N,
            TitleColor = Color.Yellow,
            SelectedIndex = Array.IndexOf(batchSizes, batchSize),
            Items = [.. batchSizes.Index().Select(pair => new KeyMenuItem(
                (Keys)(Keys.D1 + pair.Index), $"{pair.Item:N0}", () => batchSize = pair.Item))]
        },
    ];

    // Shares one position and one toggle key with the camera controller's help, rather than being a
    // second block of text drawn somewhere else
    TextOverlay.GetOrCreate(game).AddSection("Stress pile", BuildOverlayLines);
}

/// <summary>
/// Swaps the shape. Every body shares the master's model, so the existing pile has to go: leaving it
/// would draw old bodies as the new shape while they kept their original colliders.
/// </summary>
void ChangeShape(Primitive3D type)
{
    shape = type;

    Clear();

    awakeMaster.Get<ModelComponent>().Model = ModelFor(shape);
    sleepingMaster.Get<ModelComponent>().Model = SleepingModelFor(shape);

    SpawnBatch(batchSize);
}

/// <summary>Removes every body from the scene and from the instancing.</summary>
void Clear()
{
    // First: Clear also unhooks the instancings from the scene's removal events, so the loop below
    // does not unregister the bodies one at a time
    awakeInstancing.Clear();
    sleepingInstancing.Clear();

    foreach (var body in bodies) body.Entity.Scene = null;

    bodies.Clear();
    sleepingCount = 0;
}

void SpawnBatch(int count)
{
    if (layout == SpawnLayout.Grid)
    {
        var perRow = (int)ColumnWidth;

        // One loop over the count, so the batch is exactly as large as asked for: whole rows, then
        // a last row that is only partly filled
        for (var i = 0; i < count; i++)
        {
            var row = i / perRow;
            var column = i % perRow;

            // The jitter matters. A perfectly regular lattice of touching bodies degenerates
            // Bepu's broad-phase tree and kills the process with a stack overflow in
            // Refit2WithCacheOptimization - a millimetre of noise is enough to avoid it.
            Spawn(new Vector3((column - perRow / 2f) * 1.2f + Jitter(), SpawnHeight + row * 1.2f + Jitter(), 0));
        }
    }
    else
    {
        for (var i = 0; i < count; i++)
        {
            Spawn(new Vector3((random.NextSingle() - 0.5f) * (WallWidth - 10), SpawnHeight + random.NextSingle() * (count / 20f), 0));
        }
    }

    float Jitter() => (random.NextSingle() - 0.5f) * 0.05f;
}

void Spawn(Vector3 position)
{
    // Not CreatePrimitive, which would build one mesh and one pair of GPU buffers per body.
    // AddBepuPhysics derives the collider from the primitive type, not the mesh.
    // No ModelComponent of its own: the master draws every instance, and one per entity would draw the
    // whole pile twice - once per entity, once instanced - which is slower than not instancing at all
    var entity = new Entity("InstancedItem", position);

    entity.AddBepuPhysics(shape, new BepuPhysicsOptions { Component = new Body2DComponent { Collider = new CompoundCollider() } });

    // New bodies are awake by definition; UpdateSleepTint moves them once Bepu puts them to sleep
    awakeInstancing.AddInstance(entity);

    entity.Scene = scene;

    bodies.Add(new PileBody(entity, entity.Get<BodyComponent>()));
}

void Update(Scene rootScene, GameTime time)
{
    RotateArena();
    HandleInput();
    UpdateSleepTint();
    DespawnEscaped();
}

/// <summary>
/// A left drag that starts on a wall or a ramp turns that part about its own centre; with Ctrl held it
/// turns the whole arena about the middle. A drag that starts on a body is the grabber's.
/// </summary>
void RotateArena()
{
    if (game.Input.IsMouseButtonPressed(MouseButton.Left))
    {
        // Whatever the ray meets first: a body in front of a wall is grabbed, not the wall turned
        var hit = camera.Raycast(game.Input.MousePosition, GrabberDistance, out var hitInfo);

        draggedPart = hit && hitInfo.Collidable.Entity.GetParent() == arena ? hitInfo.Collidable.Entity : null;
    }

    if (!game.Input.IsMouseButtonDown(MouseButton.Left))
    {
        draggedPart = null;

        return;
    }

    var delta = game.Input.MouseDelta.X;

    if (draggedPart is not { } part || delta == 0) return;

    var target = game.Input.IsCtrlDown() ? arena : part;

    target.Transform.Rotation *= Quaternion.RotationZ(-delta * ArenaRotationSensitivity);

    WakeBodies();
}

void ResetArena()
{
    arena.Transform.Rotation = Quaternion.Identity;

    foreach (var (part, initialRotation) in arenaParts)
    {
        part.Transform.Rotation = initialRotation;
    }

    draggedPart = null;

    WakeBodies();
}

// The engine moves a static's collider with its entity at the end of the frame, but a body asleep on
// the old position stays asleep, hanging where the wall used to be
void WakeBodies()
{
    foreach (var body in bodies)
    {
        if (!body.Body.Awake) body.Body.Awake = true;
    }
}

/// <summary>
/// Moves bodies between the awake and sleeping masters as their sleep state changes, so the pile
/// shows where the engine has stopped simulating. A body is in exactly one instancing at a time.
/// </summary>
void UpdateSleepTint()
{
    foreach (var body in bodies)
    {
        var awake = body.Body.Awake;

        if (awake == body.Awake) continue;

        body.Awake = awake;

        if (awake)
        {
            sleepingInstancing.RemoveInstance(body.Entity);
            awakeInstancing.AddInstance(body.Entity);
            sleepingCount--;
        }
        else
        {
            awakeInstancing.RemoveInstance(body.Entity);
            sleepingInstancing.AddInstance(body.Entity);
            sleepingCount++;
        }
    }

    // An instancing with zero instances falls back to drawing the master's model once, un-instanced,
    // at the master's own transform - a lone shape floating at the origin. Hide the master instead.
    awakeMaster.Get<ModelComponent>().Enabled = awakeInstancing.RegisteredInstanceCount > 0;
    sleepingMaster.Get<ModelComponent>().Enabled = sleepingInstancing.RegisteredInstanceCount > 0;
}

/// <summary>
/// Removes bodies that were ejected from the arena. An escapee free-falls forever, so it never
/// sleeps - it would keep the awake counter up and the instancing sleep-skip disabled for good.
/// </summary>
void DespawnEscaped()
{
    for (var i = bodies.Count - 1; i >= 0; i--)
    {
        var entity = bodies[i].Entity;
        var position = entity.Transform.Position;

        if (MathF.Abs(position.X) < EscapeDistance && position.Y > -EscapeDistance) continue;

        if (bodies[i].Awake)
        {
            awakeInstancing.RemoveInstance(entity);
        }
        else
        {
            sleepingInstancing.RemoveInstance(entity);
            sleepingCount--;
        }

        // Leaving the scene removes the Bepu body; there is no simulation to notify by hand
        entity.Scene = null;

        bodies.RemoveAt(i);
    }
}

void HandleInput()
{
    if (!game.Input.HasKeyboard) return;

    // Only one menu open at a time, so their entry keys are free to overlap
    foreach (var menu in menus)
    {
        if (!menu.Update(game.Input)) continue;

        if (menu.IsOpen)
        {
            foreach (var other in menus)
            {
                if (other != menu) other.IsOpen = false;
            }
        }

        return;
    }

    if (game.Input.IsKeyPressed(Keys.Space)) SpawnBatch(batchSize);
    if (game.Input.IsKeyPressed(Keys.X)) Clear();
    if (game.Input.IsKeyPressedUnmodified(Keys.R)) ResetArena();
}

/// <summary>
/// Contributes this example's lines to the shared overlay, alongside the camera controller's.
/// </summary>
/// <remarks>
/// The overlay calls this every frame it draws, so the body count and the menus stay live without
/// anything having to push them. Camera keys are not listed: the camera controller contributes its
/// own section, including the F3 and F4 keys that move and hide the whole overlay.
/// </remarks>
IReadOnlyList<TextElement> BuildOverlayLines()
{
    List<TextElement> lines =
    [
        new("Space", $"Spawn {batchSize:N0} more", Color.Yellow),
        new("X", "Clear", Color.Yellow),
        new("Left mouse", "Grab and throw a body", Color.Yellow),
        new("Left drag", "Turn a wall or a ramp", Color.Yellow),
        new("Ctrl + left drag", "Turn the whole arena", Color.Yellow),
        new("R", "Put the arena back", Color.Yellow),
    ];

    // Laid out in sequence, so an expanded menu pushes the ones below it down instead of overlapping
    foreach (var menu in menus)
    {
        lines.AddRange(menu.GetLines());
    }

    lines.Add(new(string.Empty));
    lines.Add(new($"{bodies.Count:N0} bodies, two draw calls", Color.LightGreen));
    lines.Add(new($"{bodies.Count - sleepingCount:N0} awake, {sleepingCount:N0} asleep", Color.MediumSeaGreen));

    return lines;
}

/// <summary>How a batch is positioned as it spawns.</summary>
public enum SpawnLayout
{
    /// <summary>Rows and columns, lightly jittered.</summary>
    Grid,

    /// <summary>Scattered through a tall band above the walls.</summary>
    Random
}

/// <summary>One body of the pile: its entity, its body, and whether the awake master is the one drawing it.</summary>
sealed record PileBody(Entity Entity, BodyComponent Body)
{
    public bool Awake { get; set; } = true;
}

/*
---example-metadata
slug: stress-pile-2d
title:
  en: Basic 2D Scene (Stress Pile)
  cs: Základní 2D scéna (Zátěžová hromada)
level: Advanced
category: Performance
complexity: 4
order: 60
description:
  en: |-
    Thousands of 2D physics bodies piling up, drawn in two instanced draw calls - awake bodies through
    one master, sleeping bodies tinted green through another - with the shape, batch size and spawn
    layout switchable while it runs. Because one master entity per sleep state draws every body, all
    of them share one of two Models and shapes cannot be mixed - changing shape clears and
    respawns the pile, which the example uses to show how to tear a pile down safely. Models are cached
    per shape and the instancing objects are reused rather than recreated, so switching costs nothing.
    Grid spawns are deliberately jittered: a perfectly regular lattice of touching bodies degenerates
    Bepu's broad-phase tree.
  cs: |-
    Tisíce 2D fyzikálních těles se vrší na sebe a vykreslují se dvěma voláními díky instancingu; spící tělesa se zbarví zeleně.
    Za běhu lze měnit tvar, velikost dávky i způsob rozmístění. Protože každý stav spánku vykresluje
    jedna hlavní entita, sdílejí všechna tělesa jeden ze dvou modelů a tvary nelze míchat - změna tvaru proto hromadu smaže
    a vytvoří znovu. Modely se ukládají do mezipaměti podle tvaru a instancing se používá opakovaně.
concepts:
  - Drawing thousands of physics bodies in two instanced draw calls, split by sleep state
  - Tinting sleeping bodies by moving instances between an awake and a sleeping master
  - Confining bodies to the XY plane with Body2DComponent
  - Sharing one Model across every body instead of generating one each
  - Tearing down an instanced pile safely, clearing the instancing before the entities
  - Switching shape, batch size and layout at runtime with KeyMenu
  - Why a perfectly regular spawn lattice must be jittered
  - "Using helpers: AddInstancingBufferUpload, AddBepuPhysics, AddGrabber"
tags:
  - 2D
  - Bepu
  - Physics
  - Instancing
  - Performance
  - Draw Calls
  - Stress Test
related:
  - E10_2D_StressPile_Box2D
  - E10_3D_Instancing_EntityTransform
  - E04_2D_SpawnMenu
  - E05_2D_FallingShapes
enabled: true
screenshotFrame: 380
created: 2026-08-16
---
*/