using Box2D.NET;
using Stride.CommunityToolkit.Box2D;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Worlds;
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

// The Box2D twin of E10_2D_StressPile: thousands of bodies in two draw calls -
// awake through one master, sleeping tinted green through another - with
// the shape, the batch size and the spawn layout all switchable while it runs - but the physics is
// Box2D.NET instead of Bepu. The rendered shapes are the same nine 3D primitives; each one gets the
// 2D fixture that matches its head-on silhouette (a sphere becomes a circle, a cylinder a box), so
// the pile looks the same while a genuinely 2D engine simulates it.
//
// Each sleep state is drawn by one master entity, so every body on screen necessarily shares one
// of two Models - shapes cannot be mixed. Changing shape therefore clears and respawns the pile, while
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
Box2DSimulation? simulation = null;
BufferedEntityInstancing? awakeInstancing = null;
BufferedEntityInstancing? sleepingInstancing = null;
Entity? awakeMaster = null;
Entity? sleepingMaster = null;

// Sleep-state tracking, aligned with the bodies list: which master each body is drawn by right now
List<B2BodyId> bodyIds = [];
List<bool> awakeStates = [];
int sleepingCount = 0;

// One shape definition for every fixture in the pile. Contact events are off: nothing listens to
// them here, and with tens of thousands of touching bodies just generating them costs real time.
B2ShapeDef pileShapeDef = ShapeFixtureBuilder.CreateDefaultShapeDef();
pileShapeDef.enableContactEvents = false;

var shape = PrimitiveModelType.Sphere;
var layout = SpawnLayout.Grid;
var batchSize = 5000;

List<DebugTextDropdown> menus = [];

using var game = new Game();

game.Run(start: Start, update: Update);

// The buffered instancing owns its GPU buffers, and the engine never releases user-owned buffers.
// The simulation owns the native Box2D world - neither is tied to the scene's lifetime.
awakeInstancing?.Dispose();
sleepingInstancing?.Dispose();
simulation?.Dispose();

void Start(Scene rootScene)
{
    game.Window.AllowUserResizing = true;
    game.Window.Title = "Stress Pile Box2D Example - Stride Community Toolkit";

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

    // Box2D is not a Stride system: the simulation is created here and stepped from Update below.
    // Default gravity is (0, -10), matching what the Bepu version falls under.
    simulation = new Box2DSimulation();

    // Cap how fast anything may move. The boxes fall ~190 units into the funnel and arrive at
    // ~60 m/s; at Box2D's default cap (400) those impacts squeeze bodies out of the pile like
    // popcorn and over the walls, and the endlessly falling escapees then keep the scene awake.
    b2World_SetMaximumLinearSpeed(simulation.GetWorldId(), 40f);

    // Left mouse picks a body out of the pile and throws it back in. The camera is a perspective
    // one here; the grabber picks on the XY plane the bodies live in.
    game.GetCameraEntity().Add(new Grabber2DScript { Simulation = simulation });

    CreateWall(new Vector3(-WallWidth / 2, 0, 0), wallSize);
    CreateWall(new Vector3(WallWidth / 2, 0, 0), wallSize);
    // The ramps overlap deeply at the middle: with a shallow overlap, pressure from the pile
    // squeezes boxes through the crack where they meet and fires them out at high speed
    CreateWall(new Vector3(-23.27f, -47.6f, 0), new Vector3(62.3f, 1, 1), MathUtil.DegreesToRadians(-30));
    CreateWall(new Vector3(23.27f, -47.6f, 0), new Vector3(62.3f, 1, 1), MathUtil.DegreesToRadians(30));

    SetupInstancing();
    SetupMenus();

    SpawnBatch(batchSize);
}

void CreateWall(Vector3 position, Vector3 size, float rotation = 0f)
{
    // Rendering and physics are wired separately: the core helper builds only the visual cube, and
    // the static body with its box fixture is created against the simulation by hand
    var wall = game.Create3DPrimitive(PrimitiveModelType.Cube, new()
    {
        Size = size,
        Material = game.CreateMaterial(Color.LightGray),
    });

    wall.Transform.Position = position;
    wall.Transform.Rotation = Quaternion.RotationZ(rotation);
    wall.Scene = scene;

    var bodyId = simulation!.CreateStaticBody(wall, position, rotation);

    ShapeFixtureBuilder.AttachShape(Primitive2DModelType.Rectangle, new Vector2(size.X, size.Y), bodyId);
}

void SetupInstancing()
{
    // Without this nothing instanced is drawn, and nothing warns you: the code-built compositor
    // wires up transform, skinning, material and lighting, but not instancing
    game.AddInstancingSupport();

    awakeInstancing = new BufferedEntityInstancing(new Box2DEntityInstancing());
    sleepingInstancing = new BufferedEntityInstancing(new Box2DEntityInstancing());

    // One master per sleep state for the whole run. Their Models are swapped when the shape changes;
    // the instancing objects are reused, because they grow their own buffers and retire the old ones
    // safely. The sleeping master is where the sleep skip pays off: everything it holds is asleep,
    // so its gather and upload stop entirely.
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

    // The entity is discarded - the model is generated by the call, so it needs no scene
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

/// <summary>
/// Maps a rendered 3D primitive to the 2D fixture matching its head-on silhouette, sized to the
/// primitive's default dimensions. Torus and teapot have no faithful 2D shape and become circles.
/// </summary>
(Primitive2DModelType type, Vector2 size) FixtureFor(PrimitiveModelType type) => type switch
{
    PrimitiveModelType.Sphere => (Primitive2DModelType.Circle, new Vector2(0.5f, 0.5f)),
    PrimitiveModelType.Cube => (Primitive2DModelType.Square, new Vector2(1f, 1f)),
    // Radius 0.35 and length 0.5 make the default capsule 1.2 units tall in total
    PrimitiveModelType.Capsule => (Primitive2DModelType.Capsule, new Vector2(0.35f, 1.2f)),
    PrimitiveModelType.Cylinder => (Primitive2DModelType.Rectangle, new Vector2(1f, 1f)),
    PrimitiveModelType.RectangularPrism => (Primitive2DModelType.Square, new Vector2(1f, 1f)),
    PrimitiveModelType.Cone => (Primitive2DModelType.Triangle, new Vector2(1f, 1f)),
    PrimitiveModelType.TriangularPrism => (Primitive2DModelType.Triangle, new Vector2(1f, 1f)),
    PrimitiveModelType.Torus => (Primitive2DModelType.Circle, new Vector2(0.5f, 0.5f)),
    PrimitiveModelType.Teapot => (Primitive2DModelType.Circle, new Vector2(0.5f, 0.5f)),
    _ => (Primitive2DModelType.Square, new Vector2(1f, 1f)),
};

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
/// would draw old bodies as the new shape while they kept their original fixtures.
/// </summary>
void ChangeShape(PrimitiveModelType type)
{
    shape = type;

    Clear();

    awakeMaster!.Get<ModelComponent>().Model = ModelFor(shape);
    sleepingMaster!.Get<ModelComponent>().Model = SleepingModelFor(shape);

    SpawnBatch(batchSize);
}

/// <summary>Removes every body from the scene, the simulation and the instancing.</summary>
void Clear()
{
    // Before the entities leave the scene: an entity removed from the scene stays registered with
    // the instancing, which would keep reading transforms off it and drawing ghosts
    awakeInstancing?.Clear();
    sleepingInstancing?.Clear();

    foreach (var body in bodies)
    {
        // Unlike Bepu, no engine processor watches the scene: the simulation must be told the body
        // is gone, or the invisible pile keeps colliding with everything spawned after it
        simulation?.RemoveBody(body);

        body.Scene = null;
    }

    bodies.Clear();
    bodyIds.Clear();
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

            // Box2D copes with a regular lattice, but the jitter is kept from the Bepu version:
            // perfectly aligned columns balance on each other instead of toppling into a pile
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
    // No ModelComponent dance here: the Bepu version needs a model on the entity for its physics
    // helper and removes it again, but the Box2D fixture is built from the primitive type directly,
    // so the entity never carries a model - the master draws it
    var entity = new Entity("InstancedItem");

    var bodyId = simulation!.CreateDynamicBody(entity, position);
    var (fixtureType, fixtureSize) = FixtureFor(shape);

    ShapeFixtureBuilder.AttachShape(fixtureType, fixtureSize, bodyId, pileShapeDef);

    // The instancing reads this component to skip all its work while every body is asleep
    entity.Add(new Box2DBodyComponent { BodyId = bodyId });

    entity.Transform.Position = position;

    // New bodies are awake by definition; UpdateSleepTint moves them once Box2D puts them to sleep
    awakeInstancing?.AddInstance(entity);

    entity.Scene = scene;

    bodies.Add(entity);
    bodyIds.Add(bodyId);
    awakeStates.Add(true);
}

void Update(Scene rootScene, GameTime time)
{
    // Box2D is stepped by hand: fixed-timestep accumulation and entity transform sync happen inside
    simulation?.Update(time.Elapsed);

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
        var awake = b2Body_IsAwake(bodyIds[i]);

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
        var position = b2Body_GetPosition(bodyIds[i]);

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

        simulation?.RemoveBody(entity);
        entity.Scene = null;

        bodies.RemoveAt(i);
        bodyIds.RemoveAt(i);
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
        new("Left mouse", "Pick a body up and throw it", Color.Yellow),
    ];

    // Laid out in sequence, so an expanded menu pushes the ones below it down instead of overlapping
    foreach (var menu in menus)
    {
        lines.AddRange(menu.GetLines());
    }

    lines.Add(new(string.Empty));
    lines.Add(new($"{bodies.Count:N0} bodies, two draw calls, Box2D", Color.LightGreen));
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
slug: stress-pile-2d-box2d
title:
  en: Basic 2D Scene (Stress Pile, Box2D)
  cs: Základní 2D scéna (Zátěžová hromada, Box2D)
level: Advanced
category: Performance
complexity: 4
order: 61
description:
  en: |-
    The Box2D twin of the stress pile: thousands of bodies piling up, drawn in two instanced draw
    calls - awake bodies through one master, sleeping bodies tinted green through another - with the
    shape, batch size and spawn layout switchable while it runs, simulated by Box2D.NET instead of
    Bepu. The tint makes the engines' sleep behaviour directly comparable. The rendered shapes are the same nine 3D primitives; each
    gets the 2D fixture matching its head-on silhouette, so a sphere falls as a circle and a cylinder
    as a box. The differences from the Bepu version are the lesson: the simulation is created and
    stepped by hand, bodies must be removed from it explicitly when the pile is cleared, and the
    sleep-skipping instancing reads Box2DBodyComponent instead of Bepu's BodyComponent.
  cs: |-
    Box2D dvojče zátěžové hromady: tisíce těles se vrší na sebe a vykreslují se dvěma voláními díky
    instancingu, za běhu lze měnit tvar, velikost dávky i rozmístění - ale simulaci řídí Box2D.NET
    místo Bepu. Vykreslují se stejné 3D tvary; každý dostane 2D fixture odpovídající jeho siluetě
    zepředu, takže koule padá jako kruh a válec jako obdélník. Poučení jsou právě rozdíly: simulace
    se vytváří a krokuje ručně a tělesa je při mazání hromady nutné ze simulace odstranit explicitně.
concepts:
  - Drawing thousands of physics bodies in two instanced draw calls, split by sleep state
  - Tinting sleeping bodies by moving instances between an awake and a sleeping master
  - Driving the pile with Box2D.NET through Stride.CommunityToolkit.Box2D
  - Mapping 3D rendered primitives to their 2D head-on fixtures
  - Removing bodies from an external simulation explicitly, no processor does it
  - Skipping instancing work while every Box2D body sleeps with Box2DEntityInstancing
  - Disabling contact events on fixtures nothing listens to
  - Switching shape, batch size and layout at runtime with DebugTextDropdown
  - Picking bodies out of the pile with Grabber2DScript, on a perspective camera
tags:
  - 2D
  - Box2D
  - Physics
  - Instancing
  - Performance
  - Draw Calls
  - Stress Test
  - Third Party
related:
  - E10_2D_StressPile
  - E06_Box2D
  - E10_3D_Instancing_EntityTransform
enabled: true
screenshotFrame: 380
created: 2026-08-31
---
*/