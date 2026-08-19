using Stride.BepuPhysics;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Games;
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

// Thousands of 2D bodies in one draw call, with the shape, the batch size and the spawn layout all
// switchable while it runs.
//
// Everything is drawn by a single master entity, so every body on screen necessarily shares one
// Model - shapes cannot be mixed. Changing shape therefore clears the pile and respawns it, while
// changing the layout or the batch size only affects what is spawned next.

Vector3 wallHeight = new(1, 65, 1);
const float WallWidth = 100;
const float ColumnWidth = WallWidth - 30;

// One Model per shape, built on first use and kept. Nine of them cost a few hundred KB, and it makes
// switching back to a shape you have already used free.
Dictionary<PrimitiveModelType, Model> models = [];

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
BufferedEntityInstancing? instancing = null;
Entity? master = null;

var shape = PrimitiveModelType.Sphere;
var layout = SpawnLayout.Grid;
var batchSize = 5000;

List<DebugTextDropdown> menus = [];

using var game = new Game();

game.Run(start: Start, update: Update);

// The buffered instancing owns its GPU buffers, and the engine never releases user-owned buffers
instancing?.Dispose();

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

    CreateWall(new Vector3(-WallWidth / 2, 0, 0), wallHeight);
    CreateWall(new Vector3(WallWidth / 2, 0, 0), wallHeight);
    CreateWall(new Vector3(-25, -46.6f, 0), new Vector3(58.3f, 1, 1), Quaternion.RotationZ(MathUtil.DegreesToRadians(-30)));
    CreateWall(new Vector3(25, -46.6f, 0), new Vector3(58.3f, 1, 1), Quaternion.RotationZ(MathUtil.DegreesToRadians(30)));

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

    instancing = new BufferedEntityInstancing(new BepuEntityInstancing());

    // One master for the whole run. Its Model is swapped when the shape changes; the instancing
    // object is reused, because it grows its own buffers and retires the old ones safely
    master = new Entity("BufferedMaster")
    {
        new ModelComponent(ModelFor(shape)),
        new InstancingComponent { Type = instancing }
    };

    master.Scene = scene;

    // Registers with the graphics compositor, not the scene, so it outlives anything in the scene
    game.AddInstancingBufferUpload(instancing);
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

    master!.Get<ModelComponent>().Model = ModelFor(shape);

    SpawnBatch(batchSize);
}

/// <summary>Removes every body from the scene and from the instancing.</summary>
void Clear()
{
    // Before the entities leave the scene: an entity removed from the scene stays registered with
    // the instancing, which would keep reading transforms off it and drawing ghosts
    instancing?.Clear();

    foreach (var body in bodies)
    {
        body.Scene = null;
    }

    bodies.Clear();
}

void SpawnBatch(int count)
{
    if (layout == SpawnLayout.Grid)
    {
        var perRow = (int)ColumnWidth;
        var rows = Math.Max(1, count / perRow);

        for (var i = 0; i < rows; i++)
        {
            for (var j = 0; j < perRow; j++)
            {
                // The jitter matters. A perfectly regular lattice of touching bodies degenerates
                // Bepu's broad-phase tree and kills the process with a stack overflow in
                // Refit2WithCacheOptimization - a millimetre of noise is enough to avoid it.
                Spawn(new Vector3(
                    (j - perRow / 2f) * 1.2f + Jitter(),
                    30 + i * 1.2f + Jitter(),
                    0));
            }
        }
    }
    else
    {
        for (var i = 0; i < count; i++)
        {
            Spawn(new Vector3(
                (random.NextSingle() - 0.5f) * (WallWidth - 10),
                30 + random.NextSingle() * (count / 20f),
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

    instancing?.AddInstance(entity);

    entity.Scene = scene;

    bodies.Add(entity);
}

void Update(Scene rootScene, GameTime time)
{
    HandleInput();
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
        new($"{bodies.Count:N0} bodies, one draw call", Color.LightGreen),
        new(string.Empty),
    ];

    // Laid out in sequence, so an expanded menu pushes the ones below it down instead of overlapping
    foreach (var menu in menus)
    {
        lines.AddRange(menu.GetLines());
    }

    lines.Add(new(string.Empty));
    lines.Add(new($"SPACE - spawn {batchSize:N0} more     X - clear", Color.Yellow));

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
title:
  en: Basic2D Scene (Stress Pile)
  cs: Základní 2D scéna (Zátěžová hromada)
level: Advanced
category: Physics
complexity: 4
description:
  en: |
    Thousands of 2D physics bodies piling up, drawn in a single draw call through instancing, with the
    shape, batch size and spawn layout switchable while it runs. Because one master entity draws every
    body, all of them share a single Model and shapes cannot be mixed - changing shape clears and
    respawns the pile, which the example uses to show how to tear a pile down safely. Models are cached
    per shape and the instancing object is reused rather than recreated, so switching costs nothing.
    Grid spawns are deliberately jittered: a perfectly regular lattice of touching bodies degenerates
    Bepu's broad-phase tree.
  cs: |
    Tisíce 2D fyzikálních těles se vrší na sebe a vykreslují se jediným voláním díky instancingu.
    Za běhu lze měnit tvar, velikost dávky i způsob rozmístění. Protože vše vykresluje jedna hlavní
    entita, sdílejí všechna tělesa jeden model a tvary nelze míchat - změna tvaru proto hromadu smaže
    a vytvoří znovu. Modely se ukládají do mezipaměti podle tvaru a instancing se používá opakovaně.
concepts:
  - Drawing thousands of physics bodies in a single draw call
  - Confining bodies to the XY plane with Body2DComponent
  - Sharing one Model across every body instead of generating one each
  - Tearing down an instanced pile safely, clearing the instancing before the entities
  - Switching shape, batch size and layout at runtime with DebugTextDropdown
  - Why a perfectly regular spawn lattice must be jittered
  - "Using helpers: AddInstancingSupport, AddInstancingBufferUpload, AddBepu3DPhysics"
related:
  - Example22_Instancing_EntityTransform
  - Example01_Basic2DScene_SpawnMenu
  - Example01_Basic2DScene_FallingShapes
tags:
  - 2D
  - Bepu
  - Physics
  - Instancing
  - Performance
  - Draw Calls
  - Stress Test
  - Advanced
order: 57
enabled: true
created: 2026-08-16
---
*/