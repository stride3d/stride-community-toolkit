using Stride.BepuPhysics;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.Compositing;
using Stride.CommunityToolkit.Rendering.Instancing;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Scripts.Utilities;
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
// Each sleep state is drawn by one master entity, so every body on screen necessarily shares one
// of two Models - shapes cannot be mixed. Changing shape therefore clears the pile and respawns it, while
// changing the layout or the batch size only affects what is spawned next.

Vector3 wallSize = new(1, 65, 1);
const float WallWidth = 100;

// A body further than this from the pile, sideways or below, has escaped and is removed
const float EscapeDistance = 70f;
const float SpawnHeight = 150;
const float ColumnWidth = WallWidth - 30;

// One Model per shape and sleep state, built on first use and kept. Eighteen of them cost under a
// megabyte, and it makes switching back to a shape you have already used free.
Dictionary<PrimitiveModelType, Model> models = [];
Dictionary<PrimitiveModelType, Model> sleepingModels = [];

PrimitiveModelType[] shapes =
[
    PrimitiveModelType.Sphere,
    PrimitiveModelType.Cube,
    PrimitiveModelType.Capsule,
    PrimitiveModelType.Cylinder,
    PrimitiveModelType.RectangularPrism,
    PrimitiveModelType.Cone,
    PrimitiveModelType.TriangularPrism,
    PrimitiveModelType.Torus,
    PrimitiveModelType.Teapot,
];

int[] batchSizes = [1000, 2500, 5000, 10000, 20000];

var random = new Random(1);
var bodies = new List<Entity>();

Scene? scene = null;
BufferedEntityInstancing? awakeInstancing = null;
BufferedEntityInstancing? sleepingInstancing = null;
Entity? awakeMaster = null;
Entity? sleepingMaster = null;

// Sleep-state tracking, aligned with the bodies list: which master each body is drawn by right now
List<BodyComponent> bodyComponents = [];
List<bool> awakeStates = [];
int sleepingCount = 0;

var shape = PrimitiveModelType.Sphere;
var layout = SpawnLayout.Grid;
var batchSize = 5000;

List<DebugTextDropdown> menus = [];

using var game = new Game();

game.Run(start: Start, update: Update);

// The buffered instancings own their GPU buffers, and the engine never releases user-owned buffers
awakeInstancing?.Dispose();
sleepingInstancing?.Dispose();

void Start(Scene rootScene)
{
    game.Window.AllowUserResizing = true;
    game.Window.Title = "Stress Pile Example - Stride Community Toolkit";

    scene = rootScene;

    // SetupBase3D() unrolled, so the camera and the light can be aimed for a head-on view of the XY plane
    game.AddGraphicsCompositor().AddCleanUIStage();
    game.Add3DCamera(initialPosition: new Vector3(0, 0, 80), initialRotation: Vector3.Zero);
    game.AddProfiler();

    // The default aim shines toward +Z, which leaves the faces turned towards the camera unlit
    var light = game.AddDirectionalLight();
    light.Transform.Rotation = Quaternion.RotationX(MathUtil.DegreesToRadians(-30)) *
                               Quaternion.RotationY(MathUtil.DegreesToRadians(-30));

    game.Add3DCameraController();
    game.AddSkybox();

    CreateWall(new Vector3(-WallWidth / 2, 0, 0), wallSize);
    CreateWall(new Vector3(WallWidth / 2, 0, 0), wallSize);
    // The ramps overlap deeply at the middle: with a shallow overlap, pressure from the pile
    // squeezes boxes through the crack where they meet and fires them out at high speed
    CreateWall(new Vector3(-23.27f, -47.6f, 0), new Vector3(62.3f, 1, 1), Quaternion.RotationZ(MathUtil.DegreesToRadians(-30)));
    CreateWall(new Vector3(23.27f, -47.6f, 0), new Vector3(62.3f, 1, 1), Quaternion.RotationZ(MathUtil.DegreesToRadians(30)));

    SetupInstancing();
    SetupMenus();

    SpawnBatch(batchSize);
}

void CreateWall(Vector3 position, Vector3 size, Quaternion? rotation = null)
{
    var wall = game.Create3DPrimitive(PrimitiveModelType.Cube, new()
    {
        Size = size,
        Material = game.CreateMaterial(Color.LightGray),
        Component = new StaticComponent { Collider = new CompoundCollider() }
    });

    wall.Transform.Position = position;
    if (rotation.HasValue)
    {
        wall.Transform.Rotation = rotation.Value;
    }
    wall.Scene = scene;
}

void SetupInstancing()
{
    // Without this nothing instanced is drawn, and nothing warns you: the code-built compositor
    // wires up transform, skinning, material and lighting, but not instancing
    game.AddInstancingSupport();

    awakeInstancing = new BufferedEntityInstancing(new BepuEntityInstancing());
    sleepingInstancing = new BufferedEntityInstancing(new BepuEntityInstancing());

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

/// <summary>Returns the shared model for a shape, building it once on first use.</summary>
Model ModelFor(PrimitiveModelType type)
{
    if (models.TryGetValue(type, out var cached)) return cached;

    // Primitive3DEntityOptions, explicitly typed, selects the overload that does NOT attach a body.
    // Passing new() here would pick the Bepu one instead and leave a dynamic body falling forever.
    // The entity is discarded - the model is generated by the call, so it needs no scene.
    var model = game.Create3DPrimitive(type, new Primitive3DEntityOptions()).Get<ModelComponent>().Model;

    models[type] = model;

    return model;
}

/// <summary>Returns the shared sleeping-tint model for a shape, building it once on first use.</summary>
Model SleepingModelFor(PrimitiveModelType type)
{
    if (sleepingModels.TryGetValue(type, out var cached)) return cached;

    var model = game.Create3DPrimitive(type, new Primitive3DEntityOptions
    {
        Material = game.CreateMaterial(Color.MediumSeaGreen)
    }).Get<ModelComponent>().Model;

    sleepingModels[type] = model;

    return model;
}

void SetupMenus()
{
    menus =
    [
        new DebugTextDropdown
        {
            Title = "Shape",
            ToggleKey = Keys.C,
            TitleColor = Color.Yellow,
            SelectedIndex = Array.IndexOf(shapes, shape),
            Items = [.. shapes.Index().Select(pair => new DebugTextDropdownItem(
                (Keys)(Keys.D1 + pair.Index), pair.Item.ToString(), () => ChangeShape(pair.Item)))]
        },
        new DebugTextDropdown
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
        new DebugTextDropdown
        {
            Title = "Batch",
            ToggleKey = Keys.N,
            TitleColor = Color.Yellow,
            SelectedIndex = Array.IndexOf(batchSizes, batchSize),
            Items = [.. batchSizes.Index().Select(pair => new DebugTextDropdownItem(
                (Keys)(Keys.D1 + pair.Index), $"{pair.Item:N0}", () => batchSize = pair.Item))]
        },
    ];

    // Shares one position and one toggle key with the camera controller's help, rather than being a
    // second block of text drawn somewhere else
    DebugOverlay.GetOrCreate(game).AddSection("Stress pile", BuildOverlayLines);
}

/// <summary>
/// Swaps the shape. Every body shares the master's model, so the existing pile has to go: leaving it
/// would draw old bodies as the new shape while they kept their original colliders.
/// </summary>
void ChangeShape(PrimitiveModelType type)
{
    shape = type;

    Clear();

    awakeMaster!.Get<ModelComponent>().Model = ModelFor(shape);
    sleepingMaster!.Get<ModelComponent>().Model = SleepingModelFor(shape);

    SpawnBatch(batchSize);
}

/// <summary>Removes every body from the scene and from the instancing.</summary>
void Clear()
{
    // Before the entities leave the scene: an entity removed from the scene stays registered with
    // the instancing, which would keep reading transforms off it and drawing ghosts
    awakeInstancing?.Clear();
    sleepingInstancing?.Clear();

    foreach (var body in bodies)
    {
        body.Scene = null;
    }

    bodies.Clear();
    bodyComponents.Clear();
    awakeStates.Clear();
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
            Spawn(new Vector3(
                (column - perRow / 2f) * 1.2f + Jitter(),
                SpawnHeight + row * 1.2f + Jitter(),
                0));
        }
    }
    else
    {
        for (var i = 0; i < count; i++)
        {
            Spawn(new Vector3(
                (random.NextSingle() - 0.5f) * (WallWidth - 10),
                SpawnHeight + random.NextSingle() * (count / 20f),
                0));
        }
    }

    float Jitter() => (random.NextSingle() - 0.5f) * 0.05f;
}

void Spawn(Vector3 position)
{
    // AddBepu3DPhysics needs a ModelComponent present, but reads nothing from the mesh - it derives
    // the collider from the primitive type. Using the shared model here rather than
    // Create3DPrimitive avoids building one mesh and one pair of GPU buffers per body.
    var entity = new Entity("InstancedItem") { new ModelComponent(models[shape]) };

    entity.AddBepu3DPhysics(shape, new Bepu3DPhysicsOptions
    {
        Component = new Body2DComponent { Collider = new CompoundCollider() }
    });

    // The master draws every instance. Leaving each entity its own ModelComponent would draw the
    // whole pile twice - once per entity, once instanced - which is slower than not instancing at all
    entity.Remove<ModelComponent>();

    entity.Transform.Position = position;

    // New bodies are awake by definition; UpdateSleepTint moves them once Bepu puts them to sleep
    awakeInstancing?.AddInstance(entity);

    entity.Scene = scene;

    bodies.Add(entity);
    bodyComponents.Add(entity.Get<BodyComponent>()!);
    awakeStates.Add(true);
}

void Update(Scene rootScene, GameTime time)
{
    UpdateSleepTint();
    DespawnEscaped();
    HandleInput();
}

/// <summary>
/// Moves bodies between the awake and sleeping masters as their sleep state changes, so the pile
/// shows where the engine has stopped simulating. A body is in exactly one instancing at a time.
/// </summary>
void UpdateSleepTint()
{
    for (var i = 0; i < bodies.Count; i++)
    {
        var awake = bodyComponents[i].Awake;

        if (awake == awakeStates[i]) continue;

        awakeStates[i] = awake;

        if (awake)
        {
            sleepingInstancing?.RemoveInstance(bodies[i]);
            awakeInstancing?.AddInstance(bodies[i]);
            sleepingCount--;
        }
        else
        {
            awakeInstancing?.RemoveInstance(bodies[i]);
            sleepingInstancing?.AddInstance(bodies[i]);
            sleepingCount++;
        }
    }

    // An instancing with zero instances falls back to drawing the master's model once, un-instanced,
    // at the master's own transform - a lone shape floating at the origin. Hide the master instead.
    awakeMaster!.Get<ModelComponent>().Enabled = awakeInstancing!.RegisteredInstanceCount > 0;
    sleepingMaster!.Get<ModelComponent>().Enabled = sleepingInstancing!.RegisteredInstanceCount > 0;
}

/// <summary>
/// Removes bodies that were ejected from the arena. An escapee free-falls forever, so it never
/// sleeps - it would keep the awake counter up and the instancing sleep-skip disabled for good.
/// </summary>
void DespawnEscaped()
{
    for (var i = bodies.Count - 1; i >= 0; i--)
    {
        var position = bodies[i].Transform.Position;

        if (MathF.Abs(position.X) < EscapeDistance && position.Y > -EscapeDistance) continue;

        var entity = bodies[i];

        if (awakeStates[i])
        {
            awakeInstancing?.RemoveInstance(entity);
        }
        else
        {
            sleepingInstancing?.RemoveInstance(entity);
            sleepingCount--;
        }

        // Leaving the scene removes the Bepu body; there is no simulation to notify by hand
        entity.Scene = null;

        bodies.RemoveAt(i);
        bodyComponents.RemoveAt(i);
        awakeStates.RemoveAt(i);
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
}

/// <summary>
/// Contributes this example's lines to the shared overlay, alongside the camera controller's.
/// </summary>
/// <remarks>
/// The overlay calls this every frame it draws, so the body count and the menus stay live without
/// anything having to push them. Camera keys are not listed: the camera controller contributes its
/// own section, including the F2 and F3 keys that toggle and move the whole overlay.
/// </remarks>
IReadOnlyList<TextElement> BuildOverlayLines()
{
    List<TextElement> lines =
    [
        new("Space", $"Spawn {batchSize:N0} more", Color.Yellow),
        new("X", "Clear", Color.Yellow),
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
  - Switching shape, batch size and layout at runtime with DebugTextDropdown
  - Why a perfectly regular spawn lattice must be jittered
  - "Using helpers: AddInstancingSupport, AddInstancingBufferUpload, AddBepu3DPhysics"
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