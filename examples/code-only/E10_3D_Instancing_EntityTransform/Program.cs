using E10_3D_Instancing_EntityTransform;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.Instancing;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Rendering.Overlay;
using Stride.CommunityToolkit.Rendering.Text;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Input;
using Stride.Rendering;

// E10_3D_Instancing showed instancing at its simplest: one entity, an array of matrices, no behaviour.
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
var modelType = Primitive3D.Cube();

const int ItemsPerDrop = 200;
const float ItemScale = 0.5f;
const float DropHeight = 12f;
const float DropSpread = 4f;

// Size means something different for every primitive - extents for a cube, a radius for a sphere,
// radius and height for a cone - so the one scale above is mapped per shape
var modelShape = Scaled(modelType);

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

var sceneSetup = new SceneSetup3D
{
    Ground = GroundSetup.Plane(new Vector2(300)),
    Profiler = true
}.UseBepu().UseSkybox();

game.Run(sceneSetup, start: Start, update: Update);

// The buffered instancing owns its GPU buffers, and the engine never releases user-owned buffers
bufferedInstancing?.Dispose();

void Start(Scene rootScene)
{
    scene = rootScene;

    // A large ground so big drops cannot spill over the edge; the static collider comes with it
    TextOverlay.GetOrCreate(game).AddSection("Instancing", OverlayLines);

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
/// Maps one scale onto the dimensions of the chosen shape, so every shape comes out roughly the
/// same size and the drop area and camera framing stay usable.
/// </summary>
static Primitive3D Scaled(Primitive3D shape) => shape switch
{
    // The radius, so half the scale gives a body one scale across
    Primitive3D.SphereShape => Primitive3D.Sphere(ItemScale / 2),

    // The radius and the length of the cylindrical section between the two caps
    Primitive3D.CapsuleShape => Primitive3D.Capsule(ItemScale / 4, ItemScale / 2),

    // The radius and the height
    Primitive3D.ConeShape => Primitive3D.Cone(ItemScale / 2, ItemScale),
    Primitive3D.CylinderShape => Primitive3D.Cylinder(ItemScale / 2, ItemScale),

    // The radius out to the middle of the ring and the thickness of the ring itself
    Primitive3D.TorusShape => Primitive3D.Torus(ItemScale / 2, ItemScale / 4),

    // The teapot is uniformly scaled
    Primitive3D.TeapotShape => Primitive3D.Teapot(ItemScale),

    Primitive3D.TriangularPrismShape => Primitive3D.TriangularPrism(new Vector3(ItemScale)),

    // Cube and box: full extents on all three axes
    _ => Primitive3D.Cube(ItemScale)
};

/// <summary>
/// Builds the mesh once. This is the model every body is drawn with, instanced or not.
/// </summary>
Model CreateSharedModel(Scene rootScene)
{
    var prototype = game.CreatePrimitive(modelShape, new Primitive3DEntityOptions { IncludePhysics = false });

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
/// A body for one of the masters to draw: the physics body with its
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
/// The model is attached here rather than by <c>CreatePrimitive</c>, which would build a fresh
/// mesh for each of the 20,000 bodies. <c>AddBepuPhysics</c> only requires that a
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

    entity.AddBepuPhysics(modelShape);

    return entity;
}

void Update(Scene rootScene, GameTime time)
{
    HandleInput();
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

IReadOnlyList<TextElement> OverlayLines()
{
    // Read straight from the masters: these are the numbers the renderer actually uses
    var stockType = stockMaster?.Type as TimedInstancingEntityTransform;

    var toolkitStatus = toolkitInstancing?.UpdateSkippedLastFrame == true
        ? "skipped (asleep)"
        : $"{toolkitInstancing?.LastUpdateMilliseconds:0.00} ms";

    var bufferedStatus = bufferedInstancing?.UpdateSkippedLastFrame == true
        ? "skipped"
        : $"{bufferedInstancing?.LastUpdateMilliseconds:0.00} ms";

    var uploadStatus = bufferedInstancing?.UploadSkippedLastFrame == true ? "skipped" : "uploading";

    return
    [
        new("1", $"Stock: {stockItems.Count:N0} bodies, 1 draw call", stockItems.Count > 0 ? Color.LightGreen : Color.Gray),
        new("2", $"Plain: {plainItems.Count:N0} bodies, {plainItems.Count:N0} draw calls", plainItems.Count > 0 ? Color.Orange : Color.Gray),
        new("3", $"Toolkit: {toolkitItems.Count:N0} bodies, 1 draw call", toolkitItems.Count > 0 ? Color.Cyan : Color.Gray),
        new("4", $"Buffered: {bufferedItems.Count:N0} bodies, 1 draw call", bufferedItems.Count > 0 ? Color.Magenta : Color.Gray),
        new("X", "Remove all", Color.Yellow),
        new("Shift", $"Hold for {ItemsPerDrop * 10} per drop", Color.Yellow),
        new(""),
        new($"Stock update {stockType?.LastUpdateMilliseconds:0.00} ms, uploads every frame", Color.LightGreen),
        new($"Toolkit update {toolkitStatus}, uploads every frame", Color.Cyan),
        new($"Buffered update {bufferedStatus}, upload {uploadStatus}", Color.Magenta),
        new($"Shape: {modelType.Name}, set modelType at the top of Program.cs", Color.LightGray),
        new("Drop one kind at a time and let the pile come to rest.", Color.LightGray),
        new("The frame counter is a rolling average; give it a second.", Color.LightGray),
        new("Kinds 3 and 4 stop working once Bepu sleeps every body:", Color.LightGray),
        new("their update cost falls to zero as the pile rests, while", Color.LightGray),
        new("kind 1 keeps paying for a scene that is not moving.", Color.LightGray),
        new("Kind 4 stops uploading to the GPU as well.", Color.LightGray),
    ];
}

/*
---example-metadata
slug: instancing-entity-transform
title:
  en: Instancing with Entity Transforms
  cs: Instancing s transformacemi entit
level: Advanced
category: Performance
complexity: 4
order: 50
description:
  en: |-
    Every object stays a real entity, with a transform and a physics body, while the whole crowd is
    drawn in one draw call: a master entity holds the ModelComponent and an instancing type that reads
    its members' world matrices each frame. Bepu drives the transforms, so the bodies collide and pile
    up as usual. Four kinds of body drop side by side: Stride's InstancingEntityTransform, no instancing,
    the toolkit's BepuEntityInstancing and BufferedEntityInstancing. The toolkit types do no work once
    Bepu puts the bodies to sleep, which takes a settled 20,000-cube pile from 239 to 329 FPS; instancing
    removes draw calls and does nothing about simulation cost. One line switches the pile to another
    primitive.
  cs: |-
    Každý objekt zůstává plnohodnotnou entitou s transformací a fyzikálním tělesem, a přesto se celý
    zástup vykreslí jediným voláním: hlavní entita nese ModelComponent a instancing, který každý
    snímek načítá světové matice svých členů. Transformace řídí Bepu, takže tělesa kolidují a vrší se
    jako obvykle. Vedle sebe padají čtyři druhy těles: InstancingEntityTransform ze Stride, žádný
    instancing, BepuEntityInstancing a BufferedEntityInstancing z toolkitu. Typy z toolkitu přestanou
    pracovat, jakmile Bepu tělesa uspí, což u usazené hromady 20 000 kostek zvýší snímkovou frekvenci
    z 239 na 329; instancing odstraní vykreslovací volání, ne cenu simulace. Jediný řádek přepne
    hromadu na jiný primitivní tvar.
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
  - "Using helpers: SceneSetup3D, CreatePrimitive, AddBepuPhysics, AddInstancingBufferUpload, TextOverlay"
tags:
  - 3D
  - Rendering
  - Instancing
  - Bepu
  - Physics
  - Draw Calls
  - Performance
  - Entity Component
related:
  - E10_3D_Instancing
  - E02_3D_GiveMeACube
enabled: true
created: 2026-08-07
---
*/