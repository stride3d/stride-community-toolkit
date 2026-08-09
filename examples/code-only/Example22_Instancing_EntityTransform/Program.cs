using Example22_Instancing_EntityTransform;
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
using Stride.Rendering.Compositing;

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
//
// NEW: key 3 drops cubes onto a second master running FastEntityTransformInstancing, a prototype
// rewrite of the stock gather/invert path (parallel + SIMD + sleep-skip; see PLAN.md in this
// folder). The overlay shows the per-frame CPU cost of both masters so they can be compared live.

const int CubesPerDrop = 200;
const float CubeSize = 0.5f;
const float DropHeight = 12f;
const float DropSpread = 4f;

var random = new Random(1);

// Every cube ever spawned, so they can all be removed again
var instancedCubes = new List<Entity>();
var fastCubes = new List<Entity>();
var bufferedCubes = new List<Entity>();
var plainCubes = new List<Entity>();

InstancingComponent? master = null;
FastEntityTransformInstancing? fastInstancing = null;
FastBufferedEntityTransformInstancing? bufferedInstancing = null;
Model? sharedModel = null;
Scene? scene = null;

using var game = new Game();

game.Run(start: Start, update: Update);

// The buffered master owns its GPU buffers; the engine never disposes user-owned buffers
bufferedInstancing?.Dispose();

void Start(Scene rootScene)
{
    scene = rootScene;

    // Camera, light and a ground plane with a static collider for the cubes to land on
    game.SetupBase3D();
    game.Add3DCameraController();
    // A large ground so big drops cannot spill over the edge into the void; the static collider
    // comes with it by default
    game.Add3DGround(new() { Size = new Vector3(300, 1, 300) });
    game.AddSkybox();
    game.AddProfiler();

    EnableInstancing();

    sharedModel = CreateSharedCubeModel(rootScene);

    // Three masters, same shared model: stock instancing (timed), the fast prototype, and the
    // fast prototype with user-managed GPU buffers
    master = CreateMaster(rootScene, sharedModel, new TimedInstancingEntityTransform(), "InstancingMaster");
    fastInstancing = new FastEntityTransformInstancing();
    CreateMaster(rootScene, sharedModel, fastInstancing, "FastInstancingMaster");
    bufferedInstancing = new FastBufferedEntityTransformInstancing();
    CreateMaster(rootScene, sharedModel, bufferedInstancing, "BufferedInstancingMaster");

    AddBufferUploadRenderer(bufferedInstancing);

    DropCubes(CubesPerDrop, CubeKind.Instanced);
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
/// Registers the renderer that manages and uploads the buffered master's GPU buffers.
/// </summary>
/// <remarks>
/// It must run BEFORE the scene camera renderer so the upload lands on the command list ahead of
/// the frame's draw calls (same-frame data). The toolkit's AddSceneRenderer appends after the scene
/// renderer, which would add a frame of latency, so this inserts at the front by hand.
/// </remarks>
void AddBufferUploadRenderer(FastBufferedEntityTransformInstancing target)
{
    var uploader = new InstancingBufferUploadRenderer { Targets = { target } };
    var compositor = game.SceneSystem.GraphicsCompositor;

    if (compositor.Game is SceneRendererCollection collection)
    {
        collection.Children.Insert(0, uploader);
    }
    else
    {
        var wrapped = new SceneRendererCollection();
        wrapped.Children.Add(uploader);
        if (compositor.Game is not null) wrapped.Children.Add(compositor.Game);
        compositor.Game = wrapped;
    }
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
/// Creates a master entity: the one that actually gets drawn, once, for every instance.
/// </summary>
/// <remarks>
/// A master needs both a <see cref="ModelComponent"/> and an <see cref="InstancingComponent"/>.
/// The instancing type decides where the matrices come from: <see cref="InstancingEntityTransform"/>
/// collects them from registered instance entities every frame, <see cref="InstancingUserArray"/>
/// (which <see cref="FastEntityTransformInstancing"/> builds on) has them supplied in code.
/// </remarks>
InstancingComponent CreateMaster(Scene rootScene, Model model, IInstancing instancingType, string name)
{
    var entity = new Entity(name)
    {
        new ModelComponent(model),
        new InstancingComponent { Type = instancingType }
    };

    entity.Scene = rootScene;

    return entity.Get<InstancingComponent>();
}

/// <summary>
/// Drops a batch of physics-driven cubes into the scene.
/// </summary>
/// <remarks>
/// All kinds are identical in every other respect: same shared <see cref="Model"/>, same collider,
/// same spawn area. The only difference is how (or whether) the cube gets instanced.
/// </remarks>
void DropCubes(int count, CubeKind kind)
{
    if (scene is null || master is null || fastInstancing is null || bufferedInstancing is null) return;

    for (var i = 0; i < count; i++)
    {
        var entity = kind switch
        {
            CubeKind.Instanced => CreateInstancedCube(master),
            CubeKind.FastInstanced => CreatePhysicsCube("FastInstancedCube"),
            CubeKind.FastBuffered => CreatePhysicsCube("BufferedInstancedCube"),
            _ => CreatePlainCube()
        };

        entity.Transform.Position = new Vector3(
            (random.NextSingle() - 0.5f) * DropSpread,
            DropHeight + random.NextSingle() * DropHeight,
            (random.NextSingle() - 0.5f) * DropSpread);

        entity.Scene = scene;

        switch (kind)
        {
            case CubeKind.Instanced:
                instancedCubes.Add(entity);
                break;
            case CubeKind.FastInstanced:
                // No InstanceComponent involved: the fast master is told about the entity directly
                fastInstancing.AddInstance(entity);
                fastCubes.Add(entity);
                break;
            case CubeKind.FastBuffered:
                bufferedInstancing.AddInstance(entity);
                bufferedCubes.Add(entity);
                break;
            default:
                plainCubes.Add(entity);
                break;
        }
    }
}

/// <summary>
/// Removes every cube from the scene.
/// </summary>
/// <remarks>
/// Taking an entity out of the scene removes its components too, so each
/// <see cref="InstanceComponent"/> unregisters itself from the stock master and the instance count
/// drops back to zero on its own. The fast master has no such hook - it must be cleared explicitly.
/// </remarks>
void ClearCubes()
{
    foreach (var entity in instancedCubes.Concat(fastCubes).Concat(bufferedCubes).Concat(plainCubes))
    {
        entity.Scene = null;
    }

    fastInstancing?.Clear();
    bufferedInstancing?.Clear();

    instancedCubes.Clear();
    fastCubes.Clear();
    bufferedCubes.Clear();
    plainCubes.Clear();
}

/// <summary>
/// Builds the same physics body as an instanced cube, but with its own <see cref="ModelComponent"/>
/// so it is drawn on its own. This is the comparison case: one draw call per cube.
/// </summary>
Entity CreatePlainCube()
{
    var entity = CreatePhysicsCube("PlainCube");

    entity.Add(new ModelComponent(sharedModel));

    return entity;
}

/// <summary>
/// Creates one falling cube: a physics body that renders through the stock master.
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
Entity CreateInstancedCube(InstancingComponent masterInstancing)
{
    var entity = CreatePhysicsCube("InstancedCube");

    // Links this entity's transform into the stock master's instance list
    entity.Add(new InstanceComponent { Master = masterInstancing });

    return entity;
}

/// <summary>
/// The shared core of every cube: a normal dynamic body and nothing else. The collider is declared
/// by hand because there is no model to derive it from.
/// </summary>
Entity CreatePhysicsCube(string name) => new(name)
{
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

    if (game.Input.IsKeyPressed(Keys.D1)) DropCubes(batch, CubeKind.Instanced);
    if (game.Input.IsKeyPressed(Keys.D2)) DropCubes(batch, CubeKind.Plain);
    if (game.Input.IsKeyPressed(Keys.D3)) DropCubes(batch, CubeKind.FastInstanced);
    if (game.Input.IsKeyPressed(Keys.D4)) DropCubes(batch, CubeKind.FastBuffered);
    if (game.Input.IsKeyPressed(Keys.X)) ClearCubes();
}

void DrawOverlay()
{
    var line = 0;

    void Print(string text, Color? color = null)
        => game.DebugTextSystem.Print(text, new Int2(6, 60 + line++ * 18), color ?? Color.White);

    // Read straight from the masters: these are the numbers the renderer will actually draw
    var stockType = master?.Type as TimedInstancingEntityTransform;
    var stockCount = stockType?.InstanceCount ?? 0;
    var fastCount = fastInstancing?.InstanceCount ?? 0;
    var bufferedCount = bufferedInstancing?.RegisteredInstanceCount ?? 0;

    var fastStatus = fastInstancing?.SleepSkippedLastFrame == true
        ? "skipped (all asleep)"
        : $"{fastInstancing?.LastUpdateMilliseconds:0.00} ms";

    var bufferedStatus = bufferedInstancing?.SleepSkippedLastFrame == true
        ? "skipped"
        : $"{bufferedInstancing?.LastUpdateMilliseconds:0.00} ms";
    var uploadStatus = bufferedInstancing?.UploadSkippedLastFrame == true ? "skipped" : "uploading";

    Print($"1 STOCK INSTANCED {instancedCubes.Count,6} cubes -> 1 draw call   update {stockType?.LastUpdateMilliseconds:0.00} ms (engine uploads every frame)",
        instancedCubes.Count > 0 ? Color.LightGreen : Color.Gray);
    Print($"3 FAST INSTANCED  {fastCubes.Count,6} cubes -> 1 draw call   update {fastStatus} (engine uploads every frame)",
        fastCubes.Count > 0 ? Color.Cyan : Color.Gray);
    Print($"4 FAST + BUFFERS  {bufferedCubes.Count,6} cubes -> 1 draw call   update {bufferedStatus}, upload {uploadStatus}",
        bufferedCubes.Count > 0 ? Color.Magenta : Color.Gray);
    Print($"2 NOT INSTANCED   {plainCubes.Count,6} cubes -> {plainCubes.Count} draw calls",
        plainCubes.Count > 0 ? Color.Orange : Color.Gray);

    if (stockCount != instancedCubes.Count || fastCount != fastCubes.Count || bufferedCount != bufferedCubes.Count)
        Print($"   (masters report {stockCount} stock / {fastCount} fast / {bufferedCount} buffered)", Color.Red);

    Print("");
    Print($"1 - stock    2 - plain    3 - fast    4 - fast+buffers    X - remove all    (SHIFT = {CubesPerDrop * 10} per drop)", Color.Yellow);
    Print("");
    Print("All kinds share the same model, collider and spawn area. Drop one");
    Print("kind at a time and compare the costs while cubes fall; the frame");
    Print("counter is a rolling average, so give it a second to settle.");
    Print("");
    Print("The fast masters gather matrices in parallel, use a cheap rigid");
    Print("inverse, and skip their CPU work once Bepu puts every body to");
    Print("sleep. Kind 4 also owns its GPU buffers: when the pile rests it");
    Print("stops uploading too (kinds 1 and 3 re-send every matrix, every");
    Print("frame, forever - 2.5 MB per frame at 20,000 cubes).");
    Print("See PLAN.md in this example's folder for the full optimisation plan.");
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
