using Example22_Instancing_EntityTransform;
using Stride.BepuPhysics;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Games;
using Stride.CommunityToolkit.Rendering.Instancing;
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
// in ONE draw call, because a single master entity holds the model and reads every cube's world
// matrix each frame.
//
// The catch, and the reason this example exists: the instance entities must NOT have a
// ModelComponent of their own, or they are drawn twice - once by themselves and once by the master,
// which is slower than not instancing at all.
//
// Four ways to draw the same pile, so the cost of each can be compared live:
//
//   1 STOCK      Stride's InstancingEntityTransform. Re-reads and re-inverts every matrix every
//                frame, forever, even when the pile has been asleep for ten minutes.
//   2 PLAIN      No instancing: one draw call per cube. The thing instancing exists to avoid.
//   3 TOOLKIT    BepuEntityInstancing. Same result, but it caches transform references, gathers and
//                inverts in one parallel pass, and does nothing at all while every body sleeps.
//   4 BUFFERED   BufferedEntityInstancing wrapping the same. Also owns its GPU buffers, so a
//                settled pile uploads nothing either - the engine otherwise re-sends every matrix
//                every frame, 2.5 MB of them at 20,000 cubes.
//
// Measured on a desktop machine, 20,000 cubes settled on the ground:
//   1 STOCK      239 FPS   (update 1.94 ms every frame)
//   3 TOOLKIT    313 FPS   (update skipped)
//   4 BUFFERED   329 FPS   (update and upload skipped)
//   2 PLAIN      ~3 FPS    (20,000 draw calls)
//
// Those are settled figures. While the cubes are still falling, physics dominates and the
// difference shrinks: instancing removes draw calls, not simulation cost.

const int CubesPerDrop = 200;
const float CubeSize = 0.5f;
const float DropHeight = 12f;
const float DropSpread = 4f;

var random = new Random(1);

// Every cube ever spawned, so they can all be removed again
var stockCubes = new List<Entity>();
var toolkitCubes = new List<Entity>();
var bufferedCubes = new List<Entity>();
var plainCubes = new List<Entity>();

InstancingComponent? stockMaster = null;
BepuEntityInstancing? toolkitInstancing = null;
BufferedEntityInstancing? bufferedInstancing = null;
Model? sharedModel = null;
Scene? scene = null;

using var game = new Game();

game.Run(start: Start, update: Update);

// The buffered instancing owns its GPU buffers, and the engine never releases user-owned buffers
bufferedInstancing?.Dispose();

void Start(Scene rootScene)
{
    scene = rootScene;

    game.SetupBase3D();
    game.Add3DCameraController();

    // A large ground so big drops cannot spill over the edge; the static collider comes with it
    game.Add3DGround(new() { Size = new Vector3(300, 1, 300) });
    game.AddSkybox();
    game.AddProfiler();

    // Without this nothing instanced is drawn, and nothing warns you: the code-built compositor
    // wires up transform, skinning, material and lighting, but not instancing
    game.AddInstancingSupport();

    sharedModel = CreateSharedCubeModel(rootScene);

    // Three masters sharing one model: Stride's own, the toolkit's, and the toolkit's buffered
    stockMaster = CreateMaster(rootScene, sharedModel, new TimedInstancingEntityTransform(), "StockMaster");

    toolkitInstancing = new BepuEntityInstancing();
    CreateMaster(rootScene, sharedModel, toolkitInstancing, "ToolkitMaster");

    // The buffered one wraps a Bepu gather, so it skips the update AND the upload once bodies sleep
    bufferedInstancing = new BufferedEntityInstancing(new BepuEntityInstancing());
    CreateMaster(rootScene, sharedModel, bufferedInstancing, "BufferedMaster");

    // Creates and uploads the buffered master's GPU buffers, ahead of the renderer that draws them
    game.AddInstancingBufferUpload(bufferedInstancing);

    DropCubes(CubesPerDrop, CubeKind.Stock);
}

/// <summary>
/// Builds the cube mesh once. This is the model every cube is drawn with, instanced or not.
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
/// A master needs both a <see cref="ModelComponent"/> and an <see cref="InstancingComponent"/>. The
/// instancing type decides where the matrices come from - collected from entities in every case
/// here, but by three different implementations.
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
/// Every kind shares the same <see cref="Model"/>, collider and spawn area, so the only difference
/// is how each one is drawn.
/// </remarks>
void DropCubes(int count, CubeKind kind)
{
    if (scene is null || stockMaster is null || toolkitInstancing is null || bufferedInstancing is null) return;

    for (var i = 0; i < count; i++)
    {
        var entity = kind switch
        {
            CubeKind.Stock => CreateStockInstancedCube(stockMaster),
            CubeKind.Plain => CreatePlainCube(),
            _ => CreatePhysicsCube("InstancedCube")
        };

        entity.Transform.Position = new Vector3(
            (random.NextSingle() - 0.5f) * DropSpread,
            DropHeight + random.NextSingle() * DropHeight,
            (random.NextSingle() - 0.5f) * DropSpread);

        entity.Scene = scene;

        switch (kind)
        {
            case CubeKind.Stock:
                stockCubes.Add(entity);
                break;

            case CubeKind.Toolkit:
                // No InstanceComponent: the toolkit master is told about the entity directly
                toolkitInstancing.AddInstance(entity);
                toolkitCubes.Add(entity);
                break;

            case CubeKind.ToolkitBuffered:
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
/// Taking an entity out of the scene removes its components too, so each <see cref="InstanceComponent"/>
/// unregisters itself from the stock master on its own. The toolkit types have no such hook - an
/// entity leaving the scene stays registered until it is removed explicitly.
/// </remarks>
void ClearCubes()
{
    foreach (var entity in stockCubes.Concat(toolkitCubes).Concat(bufferedCubes).Concat(plainCubes))
    {
        entity.Scene = null;
    }

    toolkitInstancing?.Clear();
    bufferedInstancing?.Clear();

    stockCubes.Clear();
    toolkitCubes.Clear();
    bufferedCubes.Clear();
    plainCubes.Clear();
}

/// <summary>
/// The comparison case: a cube that draws itself, costing one draw call.
/// </summary>
Entity CreatePlainCube()
{
    var entity = CreatePhysicsCube("PlainCube");

    entity.Add(new ModelComponent(sharedModel));

    return entity;
}

/// <summary>
/// A cube drawn by Stride's own instancing, which registers itself through an
/// <see cref="InstanceComponent"/> pointing at the master.
/// </summary>
Entity CreateStockInstancedCube(InstancingComponent master)
{
    var entity = CreatePhysicsCube("StockInstancedCube");

    entity.Add(new InstanceComponent { Master = master });

    return entity;
}

/// <summary>
/// The shared core of every cube: a dynamic body and nothing else. Note what is missing - a
/// <see cref="ModelComponent"/>. The collider is declared by hand because there is no model to
/// derive it from.
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

    if (game.Input.IsKeyPressed(Keys.D1)) DropCubes(batch, CubeKind.Stock);
    if (game.Input.IsKeyPressed(Keys.D2)) DropCubes(batch, CubeKind.Plain);
    if (game.Input.IsKeyPressed(Keys.D3)) DropCubes(batch, CubeKind.Toolkit);
    if (game.Input.IsKeyPressed(Keys.D4)) DropCubes(batch, CubeKind.ToolkitBuffered);
    if (game.Input.IsKeyPressed(Keys.X)) ClearCubes();
}

void DrawOverlay()
{
    var line = 0;

    void Print(string text, Color? color = null)
        => game.DebugTextSystem.Print(text, new Int2(6, 60 + line++ * 18), color ?? Color.White);

    // Read straight from the masters: these are the numbers the renderer actually uses
    var stockType = stockMaster?.Type as TimedInstancingEntityTransform;

    var toolkitStatus = toolkitInstancing?.UpdateSkippedLastFrame == true
        ? "skipped (asleep)"
        : $"{toolkitInstancing?.LastUpdateMilliseconds:0.00} ms";

    var bufferedStatus = bufferedInstancing?.UpdateSkippedLastFrame == true
        ? "skipped"
        : $"{bufferedInstancing?.LastUpdateMilliseconds:0.00} ms";

    var uploadStatus = bufferedInstancing?.UploadSkippedLastFrame == true ? "skipped" : "uploading";

    Print($"1 STOCK    {stockCubes.Count,6} cubes -> 1 draw call   update {stockType?.LastUpdateMilliseconds:0.00} ms, uploads every frame",
        stockCubes.Count > 0 ? Color.LightGreen : Color.Gray);

    Print($"3 TOOLKIT  {toolkitCubes.Count,6} cubes -> 1 draw call   update {toolkitStatus}, uploads every frame",
        toolkitCubes.Count > 0 ? Color.Cyan : Color.Gray);

    Print($"4 BUFFERED {bufferedCubes.Count,6} cubes -> 1 draw call   update {bufferedStatus}, upload {uploadStatus}",
        bufferedCubes.Count > 0 ? Color.Magenta : Color.Gray);

    Print($"2 PLAIN    {plainCubes.Count,6} cubes -> {plainCubes.Count} draw calls",
        plainCubes.Count > 0 ? Color.Orange : Color.Gray);

    Print("");
    Print($"1 stock   2 plain   3 toolkit   4 buffered   X remove all   (SHIFT = {CubesPerDrop * 10} per drop)", Color.Yellow);
    Print("");
    Print("Drop one kind at a time and let the pile come to rest. The frame");
    Print("counter is a rolling average, so give it a second to settle.");
    Print("");
    Print("Kinds 3 and 4 stop working entirely once Bepu puts every body to");
    Print("sleep - watch their update cost fall to zero as the pile rests,");
    Print("while kind 1 keeps paying the same price for a scene that is not");
    Print("moving. Kind 4 stops uploading to the GPU as well.");
    Print("See PLAN.md in this example's folder for how far this was taken.");
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
    instancing type that reads its members' world matrices each frame, and the members carry no
    ModelComponent of their own. Bepu drives the transforms, so the cubes collide and pile up normally.
    Four kinds of cube can be dropped side by side to compare: Stride's own InstancingEntityTransform,
    no instancing at all, the toolkit's BepuEntityInstancing, and BufferedEntityInstancing. The toolkit
    types stop working once Bepu puts the bodies to sleep, which takes a settled 20,000-cube pile from
    239 to 329 FPS, and the example also shows where the real ceiling lies, because instancing removes
    draw calls and does nothing about simulation cost.
  cs: |
    Zachovejte každý objekt jako plnohodnotnou entitu - s transformací, fyzikálním tělesem i čímkoli dalším -
    a přesto vykreslete celý zástup jediným vykreslovacím voláním. Hlavní entita nese ModelComponent
    a instancing, který každý snímek načítá světové matice svých členů; členové sami žádný ModelComponent
    nemají. Transformace řídí Bepu, takže kostky normálně kolidují a vrší se na sebe. Vedle sebe lze
    porovnat čtyři druhy kostek: vlastní InstancingEntityTransform ze Stride, žádný instancing,
    BepuEntityInstancing z toolkitu a BufferedEntityInstancing. Typy z toolkitu přestanou pracovat, jakmile
    Bepu uspí tělesa, což u usazené hromady 20 000 kostek zvýší snímkovou frekvenci z 239 na 329.
concepts:
  - Combining physics bodies with instanced rendering
  - Comparing four instancing strategies side by side at runtime
  - The master and instance split for entity-driven instancing
  - Why an instance entity must not have its own ModelComponent
  - Skipping instancing work entirely while physics bodies sleep
  - Owning GPU instance buffers to avoid redundant uploads
  - Declaring a Bepu collider without a model to derive it from
  - Knowing when instancing does not help
  - "Using helpers: AddInstancingSupport, AddInstancingBufferUpload"
  - "Using helpers: SetupBase3D, Add3DGround"
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
