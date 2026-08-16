using Example22_Instancing_EntityTransform;
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
// Every falling body here is a real Entity with a real TransformComponent and a real Bepu
// BodyComponent, so it collides and piles up like any other rigid body. Yet the whole heap is drawn
// in ONE draw call, because a single master entity holds the model and reads every body's world
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
//   2 PLAIN      No instancing: one draw call per body. The thing instancing exists to avoid.
//   3 TOOLKIT    BepuEntityInstancing. Same result, but it caches transform references, gathers and
//                inverts in one parallel pass, and does nothing at all while every body sleeps.
//   4 BUFFERED   BufferedEntityInstancing wrapping the same. Also owns its GPU buffers, so a
//                settled pile uploads nothing either - the engine otherwise re-sends every matrix
//                every frame, 2.5 MB of them at 20,000 bodies.
//
// Measured on a desktop machine, 20,000 cubes settled on the ground:
//   1 STOCK      239 FPS   (update 1.94 ms every frame)
//   3 TOOLKIT    313 FPS   (update skipped)
//   4 BUFFERED   329 FPS   (update and upload skipped)
//   2 PLAIN      ~3 FPS    (20,000 draw calls)
//
// Those are settled figures. While the bodies are still falling, physics dominates and the
// difference shrinks: instancing removes draw calls, not simulation cost.

// The shape every body uses. Change this one line to drop something else: spheres roll, cones tip
// over, and Cone, Teapot, Torus and TriangularPrism come out as convex hulls, which are far more
// expensive to simulate than a box or a sphere. Everything below is shape-agnostic.
var modelType = PrimitiveModelType.Cone;

const int ItemsPerDrop = 200;
const float ItemScale = 0.5f;
const float DropHeight = 12f;
const float DropSpread = 4f;

// Size means something different for every primitive - extents for a cube, a radius for a sphere,
// radius and height for a cone - so the one scale above is mapped per shape
var modelSize = SizeFor(modelType);

var random = new Random(1);

// Every body ever spawned, so they can all be removed again
var stockItems = new List<Entity>();
var toolkitItems = new List<Entity>();
var bufferedItems = new List<Entity>();
var plainItems = new List<Entity>();

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

    sharedModel = CreateSharedModel(rootScene);

    // Three masters sharing one model: Stride's own, the toolkit's, and the toolkit's buffered
    stockMaster = CreateMaster(rootScene, sharedModel, new TimedInstancingEntityTransform(), "StockMaster");

    toolkitInstancing = new BepuEntityInstancing();
    CreateMaster(rootScene, sharedModel, toolkitInstancing, "ToolkitMaster");

    // The buffered one wraps a Bepu gather, so it skips the update AND the upload once bodies sleep
    bufferedInstancing = new BufferedEntityInstancing(new BepuEntityInstancing());
    CreateMaster(rootScene, sharedModel, bufferedInstancing, "BufferedMaster");

    // Creates and uploads the buffered master's GPU buffers, ahead of the renderer that draws them
    game.AddInstancingBufferUpload(bufferedInstancing);

    DropItems(ItemsPerDrop, ItemKind.Stock);
}

/// <summary>
/// Maps one scale onto whatever <c>Size</c> means for the chosen primitive, so every shape comes out
/// roughly the same size and the drop area and camera framing stay usable.
/// </summary>
static Vector3 SizeFor(PrimitiveModelType type) => type switch
{
    // X is the radius, so half the scale gives a body one scale across
    PrimitiveModelType.Sphere => new Vector3(ItemScale / 2, 0, 0),

    // X radius, Y the length of the cylindrical section between the two caps
    PrimitiveModelType.Capsule => new Vector3(ItemScale / 4, ItemScale / 2, 0),

    // X radius, Y height
    PrimitiveModelType.Cone => new Vector3(ItemScale / 2, ItemScale, 0),

    // X radius, Z height - not Y, which the cylinder ignores
    PrimitiveModelType.Cylinder => new Vector3(ItemScale / 2, 0, ItemScale),

    // X the radius out to the middle of the ring, Y the thickness of the ring itself
    PrimitiveModelType.Torus => new Vector3(ItemScale / 2, ItemScale / 4, 0),

    // X only; the teapot is uniformly scaled
    PrimitiveModelType.Teapot => new Vector3(ItemScale, 0, 0),

    // Cube, RectangularPrism, TriangularPrism: full extents on all three axes
    _ => new Vector3(ItemScale)
};

/// <summary>
/// Builds the mesh once. This is the model every body is drawn with, instanced or not.
/// </summary>
Model CreateSharedModel(Scene rootScene)
{
    var prototype = game.Create3DPrimitive(modelType, new Primitive3DEntityOptions
    {
        Size = modelSize
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
/// Drops a batch of physics-driven bodies into the scene.
/// </summary>
/// <remarks>
/// Every kind shares the same <see cref="Model"/>, collider and spawn area, so the only difference
/// is how each one is drawn.
/// </remarks>
void DropItems(int count, ItemKind kind)
{
    if (scene is null || stockMaster is null || toolkitInstancing is null || bufferedInstancing is null) return;

    for (var i = 0; i < count; i++)
    {
        var entity = kind switch
        {
            ItemKind.Stock => CreateStockInstancedItem(stockMaster),
            ItemKind.Plain => CreatePlainItem(),
            _ => CreateInstancedItem("InstancedItem")
        };

        entity.Transform.Position = new Vector3(
            (random.NextSingle() - 0.5f) * DropSpread,
            DropHeight + random.NextSingle() * DropHeight,
            (random.NextSingle() - 0.5f) * DropSpread);

        entity.Scene = scene;

        switch (kind)
        {
            case ItemKind.Stock:
                stockItems.Add(entity);
                break;

            case ItemKind.Toolkit:
                // No InstanceComponent: the toolkit master is told about the entity directly
                toolkitInstancing.AddInstance(entity);
                toolkitItems.Add(entity);
                break;

            case ItemKind.ToolkitBuffered:
                bufferedInstancing.AddInstance(entity);
                bufferedItems.Add(entity);
                break;

            default:
                plainItems.Add(entity);
                break;
        }
    }
}

/// <summary>
/// Removes every body from the scene.
/// </summary>
/// <remarks>
/// Taking an entity out of the scene removes its components too, so each <see cref="InstanceComponent"/>
/// unregisters itself from the stock master on its own. The toolkit types have no such hook - an
/// entity leaving the scene stays registered until it is removed explicitly.
/// </remarks>
void ClearItems()
{
    foreach (var entity in stockItems.Concat(toolkitItems).Concat(bufferedItems).Concat(plainItems))
    {
        entity.Scene = null;
    }

    toolkitInstancing?.Clear();
    bufferedInstancing?.Clear();

    stockItems.Clear();
    toolkitItems.Clear();
    bufferedItems.Clear();
    plainItems.Clear();
}

/// <summary>
/// The comparison case: a body that draws itself, costing one draw call.
/// </summary>
Entity CreatePlainItem() => CreatePhysicsItem("PlainItem");

/// <summary>
/// A body drawn by Stride's own instancing, which registers itself through an
/// <see cref="InstanceComponent"/> pointing at the master.
/// </summary>
Entity CreateStockInstancedItem(InstancingComponent master)
{
    var entity = CreateInstancedItem("StockInstancedItem");

    entity.Add(new InstanceComponent { Master = master });

    return entity;
}

/// <summary>
/// A body for one of the toolkit masters to draw: the physics body with its
/// <see cref="ModelComponent"/> taken back off, which is the whole point of the example.
/// </summary>
Entity CreateInstancedItem(string name)
{
    var entity = CreatePhysicsItem(name);

    entity.Remove<ModelComponent>();

    return entity;
}

/// <summary>
/// The shared core of every body: a dynamic Bepu body with the collider the toolkit derives for the
/// chosen primitive, plus the shared model.
/// </summary>
/// <remarks>
/// <para>
/// The model is attached here rather than by <c>Create3DPrimitive</c>, which would build a fresh
/// mesh for each of the 20,000 bodies. <c>AddBepu3DPhysics</c> only requires that a
/// <see cref="ModelComponent"/> is present - it derives the collider from the primitive type and
/// size, and reads nothing out of the mesh.
/// </para>
/// <para>
/// Hull shapes get a hull collider each, but the hull data behind them is shared across every body
/// of the same shape and size, so Bepu builds exactly one hull no matter how many are dropped.
/// </para>
/// </remarks>
Entity CreatePhysicsItem(string name)
{
    var entity = new Entity(name) { new ModelComponent(sharedModel) };

    entity.AddBepu3DPhysics(modelType, new Bepu3DPhysicsOptions { Size = modelSize });

    return entity;
}

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
        ? ItemsPerDrop * 10
        : ItemsPerDrop;

    if (game.Input.IsKeyPressed(Keys.D1)) DropItems(batch, ItemKind.Stock);
    if (game.Input.IsKeyPressed(Keys.D2)) DropItems(batch, ItemKind.Plain);
    if (game.Input.IsKeyPressed(Keys.D3)) DropItems(batch, ItemKind.Toolkit);
    if (game.Input.IsKeyPressed(Keys.D4)) DropItems(batch, ItemKind.ToolkitBuffered);
    if (game.Input.IsKeyPressed(Keys.X)) ClearItems();
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

    Print($"1 STOCK    {stockItems.Count,6} bodies -> 1 draw call   update {stockType?.LastUpdateMilliseconds:0.00} ms, uploads every frame",
        stockItems.Count > 0 ? Color.LightGreen : Color.Gray);

    Print($"3 TOOLKIT  {toolkitItems.Count,6} bodies -> 1 draw call   update {toolkitStatus}, uploads every frame",
        toolkitItems.Count > 0 ? Color.Cyan : Color.Gray);

    Print($"4 BUFFERED {bufferedItems.Count,6} bodies -> 1 draw call   update {bufferedStatus}, upload {uploadStatus}",
        bufferedItems.Count > 0 ? Color.Magenta : Color.Gray);

    Print($"2 PLAIN    {plainItems.Count,6} bodies -> {plainItems.Count} draw calls",
        plainItems.Count > 0 ? Color.Orange : Color.Gray);

    Print("");
    Print($"1 stock   2 plain   3 toolkit   4 buffered   X remove all   (SHIFT = {ItemsPerDrop * 10} per drop)", Color.Yellow);
    Print("");
    Print($"Shape: {modelType} - change modelType at the top of Program.cs to drop something else.");
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
    ModelComponent of their own. Bepu drives the transforms, so the bodies collide and pile up normally.
    Four kinds of body can be dropped side by side to compare: Stride's own InstancingEntityTransform,
    no instancing at all, the toolkit's BepuEntityInstancing, and BufferedEntityInstancing. The toolkit
    types stop working once Bepu puts the bodies to sleep, which takes a settled 20,000-cube pile from
    239 to 329 FPS, and the example also shows where the real ceiling lies, because instancing removes
    draw calls and does nothing about simulation cost. One line at the top switches the whole pile to
    any other primitive, so the same comparison can be run with spheres, cones or hulls.
  cs: |
    Zachovejte každý objekt jako plnohodnotnou entitu - s transformací, fyzikálním tělesem i čímkoli dalším -
    a přesto vykreslete celý zástup jediným vykreslovacím voláním. Hlavní entita nese ModelComponent
    a instancing, který každý snímek načítá světové matice svých členů; členové sami žádný ModelComponent
    nemají. Transformace řídí Bepu, takže tělesa normálně kolidují a vrší se na sebe. Vedle sebe lze
    porovnat čtyři druhy těles: vlastní InstancingEntityTransform ze Stride, žádný instancing,
    BepuEntityInstancing z toolkitu a BufferedEntityInstancing. Typy z toolkitu přestanou pracovat, jakmile
    Bepu uspí tělesa, což u usazené hromady 20 000 kostek zvýší snímkovou frekvenci z 239 na 329.
    Jediný řádek na začátku přepne celou hromadu na libovolnou jinou primitivní tvar.
concepts:
  - Combining physics bodies with instanced rendering
  - Comparing four instancing strategies side by side at runtime
  - The master and instance split for entity-driven instancing
  - Why an instance entity must not have its own ModelComponent
  - Skipping instancing work entirely while physics bodies sleep
  - Owning GPU instance buffers to avoid redundant uploads
  - Deriving a Bepu collider from a primitive type without building a mesh per body
  - Switching the whole scene to a different primitive from one line
  - Knowing when instancing does not help
  - "Using helpers: AddInstancingSupport, AddInstancingBufferUpload"
  - "Using helpers: SetupBase3D, Add3DGround, AddBepu3DPhysics"
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
