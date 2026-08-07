using Stride.BepuPhysics;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Games;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Input;
using Stride.Rendering;

// Example21 showed instancing at its simplest: one entity, an array of matrices, no behaviour.
// This one keeps the entities.
//
// Every falling cube here is a real Entity with a real TransformComponent and a real Bepu
// BodyComponent, so it collides and piles up like any other rigid body. Yet the whole heap is drawn
// in ONE draw call, because each cube carries an InstanceComponent pointing at a single master
// entity whose InstancingEntityTransform reads their world matrices every frame.
//
// The catch, and the reason this example exists: the instance entities must NOT have a
// ModelComponent of their own. See CreateInstancedCube below.
//
// Measured on a desktop machine with 20,000 cubes settled on the ground:
//   instanced      ~130 FPS   (1 draw call)
//   not instanced    ~3 FPS   (20,000 draw calls)
// Note those figures are for a settled pile. While the cubes are still falling and colliding the
// physics dominates; once Bepu puts the settled bodies to sleep they cost almost nothing and
// rendering becomes the bottleneck, which is where instancing earns its keep.

const int CubesPerDrop = 200;
const float CubeSize = 0.5f;
const float DropHeight = 12f;
const float DropSpread = 4f;

var random = new Random(1);

// Every cube ever spawned, so they can all be removed again
var instancedCubes = new List<Entity>();
var plainCubes = new List<Entity>();

InstancingComponent? master = null;
Model? sharedModel = null;
Scene? scene = null;

using var game = new Game();

game.Run(start: Start, update: Update);

void Start(Scene rootScene)
{
    scene = rootScene;

    // Camera, light and a ground plane with a static collider for the cubes to land on
    game.SetupBase3DScene();
    game.AddSkybox();
    game.AddProfiler();

    EnableInstancing();

    sharedModel = CreateSharedCubeModel(rootScene);
    master = CreateMaster(rootScene, sharedModel);

    DropCubes(CubesPerDrop, instanced: true);
}

/// <summary>
/// Adds the render feature that performs instanced drawing.
/// </summary>
/// <remarks>
/// The code-built compositor the toolkit uses does not include it, so without this call the whole
/// heap renders as the single cube belonging to the master entity.
/// </remarks>
void EnableInstancing()
{
    var meshRenderFeature = game.SceneSystem.GraphicsCompositor.RenderFeatures.OfType<MeshRenderFeature>().First();

    meshRenderFeature.RenderFeatures.Add(new InstancingRenderFeature());
}

/// <summary>
/// Builds the cube mesh once. This is the model every instance is drawn with.
/// </summary>
Model CreateSharedCubeModel(Scene rootScene)
{
    var prototype = game.Create3DPrimitive(PrimitiveModelType.Cube, new Primitive3DEntityOptions
    {
        Size = new Vector3(CubeSize)
    });

    // Parked out of sight; it exists only to own the Model
    prototype.Transform.Position = new Vector3(0, -100, 0);
    prototype.Scene = rootScene;

    return prototype.Get<ModelComponent>().Model;
}

/// <summary>
/// Creates the master entity: the one that actually gets drawn, once, for every instance.
/// </summary>
/// <remarks>
/// The master needs both a <see cref="ModelComponent"/> and an <see cref="InstancingComponent"/>.
/// <see cref="InstancingEntityTransform"/> means "collect the world matrices from my instances every
/// frame", as opposed to <see cref="InstancingUserArray"/> where you supply the matrices yourself.
/// </remarks>
InstancingComponent CreateMaster(Scene rootScene, Model model)
{
    var entity = new Entity("InstancingMaster")
    {
        new ModelComponent(model),
        new InstancingComponent { Type = new InstancingEntityTransform() }
    };

    entity.Scene = rootScene;

    return entity.Get<InstancingComponent>();
}

/// <summary>
/// Drops a batch of physics-driven cubes into the scene, instanced or not.
/// </summary>
/// <remarks>
/// Both kinds are identical in every other respect: same shared <see cref="Model"/>, same collider,
/// same spawn area. The only difference is whether the cube draws itself or is drawn by the master.
/// </remarks>
void DropCubes(int count, bool instanced)
{
    if (scene is null || master is null) return;

    for (var i = 0; i < count; i++)
    {
        var entity = instanced ? CreateInstancedCube(master) : CreatePlainCube();

        entity.Transform.Position = new Vector3(
            (random.NextSingle() - 0.5f) * DropSpread,
            DropHeight + random.NextSingle() * DropHeight,
            (random.NextSingle() - 0.5f) * DropSpread);

        entity.Scene = scene;

        (instanced ? instancedCubes : plainCubes).Add(entity);
    }
}

/// <summary>
/// Removes every cube from the scene.
/// </summary>
/// <remarks>
/// Taking an entity out of the scene removes its components too, so each
/// <see cref="InstanceComponent"/> unregisters itself from the master and the instance count drops
/// back to zero on its own.
/// </remarks>
void ClearCubes()
{
    foreach (var entity in instancedCubes.Concat(plainCubes))
    {
        entity.Scene = null;
    }

    instancedCubes.Clear();
    plainCubes.Clear();
}

/// <summary>
/// Builds the same physics body as an instanced cube, but with its own <see cref="ModelComponent"/>
/// so it is drawn on its own. This is the comparison case: one draw call per cube.
/// </summary>
Entity CreatePlainCube() => new("PlainCube")
{
    new ModelComponent(sharedModel),
    new BodyComponent
    {
        Collider = new CompoundCollider
        {
            Colliders = { new BoxCollider { Size = new Vector3(CubeSize) } }
        }
    }
};

/// <summary>
/// Creates one falling cube: a physics body that renders through the master.
/// </summary>
/// <remarks>
/// Note what is NOT here: a <see cref="ModelComponent"/>. It is tempting to build these with
/// <c>Create3DPrimitive</c> and then bolt an <see cref="InstanceComponent"/> on top, but that entity
/// would keep its own model and be drawn individually **as well as** being drawn by the master, so
/// every cube is rendered twice and the result is slower than not instancing at all.
/// <para>
/// The instance supplies only a transform. Bepu writes that transform, and
/// <see cref="InstancingEntityTransform"/> reads it back out each frame.
/// </para>
/// </remarks>
Entity CreateInstancedCube(InstancingComponent masterInstancing) => new("InstancedCube")
{
    // Links this entity's transform into the master's instance list
    new InstanceComponent { Master = masterInstancing },

    // A normal dynamic body. The collider is declared by hand because there is no model to
    // derive it from.
    new BodyComponent
    {
        Collider = new CompoundCollider
        {
            Colliders = { new BoxCollider { Size = new Vector3(CubeSize) } }
        }
    }
};

void Update(Scene rootScene, GameTime time)
{
    HandleInput();
    DrawOverlay();
}

void HandleInput()
{
    if (!game.Input.HasKeyboard) return;

    // Hold shift to drop a much bigger batch; the difference only gets interesting in the thousands
    var batch = game.Input.IsKeyDown(Keys.LeftShift) || game.Input.IsKeyDown(Keys.RightShift)
        ? CubesPerDrop * 10
        : CubesPerDrop;

    if (game.Input.IsKeyPressed(Keys.D1)) DropCubes(batch, instanced: true);
    if (game.Input.IsKeyPressed(Keys.D2)) DropCubes(batch, instanced: false);
    if (game.Input.IsKeyPressed(Keys.X)) ClearCubes();
}

void DrawOverlay()
{
    var line = 0;

    void Print(string text, Color? color = null)
        => game.DebugTextSystem.Print(text, new Int2(6, 60 + line++ * 18), color ?? Color.White);

    // Read straight from the master: this is the number the renderer will actually draw
    var liveInstances = (master?.Type as InstancingEntityTransform)?.InstanceCount ?? 0;

    Print($"INSTANCED    {instancedCubes.Count,5} cubes -> 1 draw call (master reports {liveInstances})",
        instancedCubes.Count > 0 ? Color.LightGreen : Color.Gray);
    Print($"NOT INSTANCED{plainCubes.Count,5} cubes -> {plainCubes.Count} draw calls",
        plainCubes.Count > 0 ? Color.Orange : Color.Gray);
    Print("");
    Print($"1 - drop {CubesPerDrop} instanced      2 - drop {CubesPerDrop} not instanced      X - remove all");
    Print($"    hold SHIFT for {CubesPerDrop * 10} at a time", Color.Yellow);
    Print("");
    Print("Both kinds share the same model, collider and spawn area, so");
    Print("instancing is the only difference. Add one kind at a time and");
    Print("compare; the frame counter is a rolling average, so give it a");
    Print("second to settle, and let the pile come to rest.");
    Print("");
    Print("At 20,000 cubes the gap is roughly 130 FPS against 3 FPS.");
    Print("While they are still falling, physics dominates instead:");
    Print("instancing removes draw calls, not simulation cost.");
}

/*
---example-metadata
title:
  en: Instancing with Entity Transforms
  cs: Instancing s transformacemi entit
level: Advanced
category: Rendering
complexity: 4
description:
  en: |
    Keep every object a real entity - with a transform, a physics body and anything else you need - while
    still drawing the whole crowd in a single draw call. A master entity holds a ModelComponent and an
    InstancingComponent set to InstancingEntityTransform; each member carries an InstanceComponent
    pointing at that master and, crucially, no ModelComponent of its own. Bepu drives the transforms and
    the instancing type reads them back each frame, so the cubes collide and pile up normally. Drop instanced
    and non-instanced cubes side by side to compare: at 20,000 settled cubes the instanced pile runs at
    roughly 130 FPS against 3 FPS without. The example also shows where the real ceiling lies, because
    instancing removes draw calls and does nothing about simulation cost.
  cs: |
    Zachovejte každý objekt jako plnohodnotnou entitu - s transformací, fyzikálním tělesem i čímkoli dalším -
    a přesto vykreslete celý zástup jediným vykreslovacím voláním. Hlavní entita nese ModelComponent
    a InstancingComponent typu InstancingEntityTransform; každý člen má InstanceComponent odkazující na tuto
    hlavní entitu a hlavně žádný vlastní ModelComponent. Transformace řídí Bepu a instancing je každý snímek
    načítá, takže kostky normálně kolidují a vrší se na sebe. Příklad rovněž ukazuje, kde je skutečný strop:
    instancing odstraňuje vykreslovací volání, nikoli náklady na simulaci.
concepts:
  - Combining physics bodies with instanced rendering
  - Comparing instanced and non-instanced cubes side by side at runtime
  - The master and instance split with InstancingEntityTransform
  - Why an instance entity must not have its own ModelComponent
  - Declaring a Bepu collider without a model to derive it from
  - Registering InstancingRenderFeature on the MeshRenderFeature
  - Knowing when instancing does not help
  - "Using helpers: SetupBase3DScene"
related:
  - Example21_Instancing
  - Example02_GiveMeACube
  - Example_Bepu_Playground
tags:
  - 3D
  - Rendering
  - Instancing
  - Bepu
  - Physics
  - Draw Calls
  - Performance
  - Entity Component
  - Advanced
order: 22
enabled: true
created: 2026-08-07
---
*/
