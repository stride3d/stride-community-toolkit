using E05_3D_CubeFountain;
using Stride.BepuPhysics;
using Stride.BepuPhysics.Definitions;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.Instancing;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Scripts.Utilities;
using Stride.CommunityToolkit.Skyboxes;
using Stride.CommunityToolkit.Windows;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Input;
using Stride.Rendering;

// A fountain of cubes, spheres and cylinders. A nozzle launches them at a steady rate, they fall
// into a basin, and once the fountain owns as many as it may it takes the oldest one's place for
// the next launch, so it runs for as long as you watch with a fixed number of bodies.
//
// The lesson is WHERE the rate is counted. CubeFountain.cs counts it in ISimulationUpdate, the
// callback Bepu makes once per fixed physics step, so the fountain belongs to the simulation's
// time. Press - to slow the simulation, down to a pause, and + to speed it up to five times: the
// fountain slows, stops and speeds up with it, and no line of the fountain's code knows about the
// keys.
//
// Slow motion shows a second thing. At a quarter speed the simulation steps fifteen times a second
// while the screen draws sixty, so a body that is only moved on a physics step visibly jerks. The
// bodies here are interpolated: between two steps each is drawn part of the way from its last pose
// to its current one. Press I in slow motion to turn that off and see the steps.
//
// Two things are deliberately not taught again here:
//   ISimulationUpdate itself    - see E02_3D_GiveMeACube_SimulationUpdate.
//   Drawing many bodies at once - see E10_3D_Instancing_EntityTransform. The bodies here have no
//                                 model of their own; one master entity per shape draws them all.

WindowsDpiManager.EnablePerMonitorV2();

const float NozzleHeight = 1.4f;
const float BasinHalfSize = 5f;
const float ThrowSpeed = 22f;

// What the fountain can launch. Size means what Create3DPrimitive takes for the shape: a cube's
// three sides, a sphere's radius in X, a cylinder's radius in X and height in Z
(string Name, PrimitiveModelType Type, Vector3 Size, Color Colour)[] shapes =
[
    ("Cube", PrimitiveModelType.Cube, new Vector3(0.35f), new Color(70, 160, 235)),
    ("Sphere", PrimitiveModelType.Sphere, new Vector3(0.2f, 0f, 0f), new Color(240, 150, 60)),
    ("Cylinder", PrimitiveModelType.Cylinder, new Vector3(0.18f, 0f, 0.36f), new Color(110, 200, 110)),
];

// The simulation speeds + and - step through, from paused to five times as fast
float[] timeScales = [0f, 0.01f, 0.0625f, 0.125f, 0.25f, 0.5f, 1f, 2f, 3f, 4f, 5f];

// One model and one instancing master per shape, in the order of the shapes
var models = new Model[shapes.Length];
var masters = new BufferedEntityInstancing[shapes.Length];

CubeFountain? fountain = null;
DebugTextDropdown? shapeMenu = null;
var timeScaleIndex = Array.IndexOf(timeScales, 1f);
var interpolated = true;

using var game = new Game();

game.Run(start: Start, update: Update);

foreach (var master in masters) master?.Dispose();

void Start(Scene scene)
{
    game.Window.AllowUserResizing = true;
    game.Window.Title = "Cube fountain - Stride Community Toolkit";

    game.SetupBase3DScene();
    game.AddSkybox();
    game.AddProfiler();

    game.SetCameraPosition(new Vector3(0f, 6.5f, 15f));
    game.SetCameraRotation(new Vector3(0f, -14f, 0f));

    // Without this nothing instanced is drawn, and nothing warns you
    game.AddInstancingSupport();

    for (var i = 0; i < shapes.Length; i++)
    {
        // The model, owned by a prototype parked out of sight
        var prototype = game.Create3DPrimitive(shapes[i].Type, new Primitive3DEntityOptions
        {
            Size = shapes[i].Size,
            Material = game.CreateMaterial(shapes[i].Colour, metalness: 0f, glossiness: 0.5f),
        });

        prototype.Transform.Position = new Vector3(0f, -100f, 0f);
        prototype.Scene = scene;
        models[i] = prototype.Get<ModelComponent>().Model;

        // The master: the one entity that is drawn, once for every body of this shape. An instance
        // shares its master's model and material, which is why each shape and colour has its own.
        // The buffered kind stops gathering and uploading once every body has gone to sleep
        masters[i] = new BufferedEntityInstancing(new BepuEntityInstancing());

        scene.Entities.Add(new Entity($"{shapes[i].Name} master") { new ModelComponent(models[i]), new InstancingComponent { Type = masters[i] } });
    }

    game.AddInstancingBufferUpload(masters);

    BuildBasin(scene);

    fountain = new CubeFountain
    {
        Create = (kind, position) => CreateBody(scene, kind, position),
        Remove = RemoveBody,
    };

    // The fountain's entity is the mouth of the nozzle: bodies start where it is
    var mouth = new Entity("Fountain") { fountain };

    mouth.Transform.Position = new Vector3(0f, NozzleHeight + 0.4f, 0f);
    scene.Entities.Add(mouth);

    // The shape to launch, as a dropdown in the overlay: O opens it, a digit picks
    shapeMenu = new DebugTextDropdown
    {
        Title = "Shape",
        ToggleKey = Keys.O,
        TitleColor = Color.Gold,
        Items = [.. shapes.Index().Select(pair => new DebugTextDropdownItem(
            (Keys)(Keys.D1 + pair.Index), pair.Item.Name, () => fountain.Kind = pair.Index, pair.Item.Colour))],
    };

    DebugOverlay.GetOrCreate(game).AddSection("Cube fountain", OverlayLines);
}

void Update(Scene scene, GameTime time)
{
    if (fountain is null) return;

    shapeMenu?.Update(game.Input);

    if (game.Input.IsKeyPressed(Keys.J)) fountain.Rate = MathF.Max(fountain.Rate - 5f, 0f);
    if (game.Input.IsKeyPressed(Keys.K)) fountain.Rate = MathF.Min(fountain.Rate + 5f, 120f);

    // Plus and minus on the main row or on the number pad
    var faster = game.Input.IsKeyPressed(Keys.OemPlus) || game.Input.IsKeyPressed(Keys.Add);
    var slower = game.Input.IsKeyPressed(Keys.OemMinus) || game.Input.IsKeyPressed(Keys.Subtract);

    if ((faster || slower) && fountain.Simulation is { } simulation)
    {
        timeScaleIndex = Math.Clamp(timeScaleIndex + (faster ? 1 : -1), 0, timeScales.Length - 1);

        // The one line that slows, stops or speeds up the fountain. Nothing is sent to the fountain
        simulation.TimeScale = timeScales[timeScaleIndex];

        // Five times the speed is five physics steps a frame at 60 frames a second, and the
        // simulation is meant to run at most this many, three by default
        simulation.MaxStepPerFrame = 8;
    }

    if (game.Input.IsKeyPressed(Keys.I))
    {
        interpolated = !interpolated;

        foreach (var body in fountain.Bodies) body.InterpolationMode = Interpolation();
    }

    if (game.Input.IsMouseButtonPressed(MouseButton.Left))
    {
        // A thrown body comes from the same store as the fountain's, so the count stays capped
        var camera = game.GetCameraEntity().Transform;
        var forward = Vector3.TransformNormal(-Vector3.UnitZ, camera.WorldMatrix);

        forward.Normalize();

        fountain.Launch(camera.WorldMatrix.TranslationVector + forward * 1.5f, forward * ThrowSpeed);
    }
}

IReadOnlyList<TextElement> OverlayLines()
{
    List<TextElement> lines = [];

    if (shapeMenu is not null) lines.AddRange(shapeMenu.GetLines());

    if (fountain is null) return lines;

    lines.AddRange(
    [
        new(["J", "K"], "Fewer or more bodies per second", Color.Gold),
        new(["+", "-"], "Simulation speed, from paused to 5x", Color.Gold),
        new("I", interpolated ? "Interpolation on: smooth between steps" : "Interpolation off: moved on steps only", Color.Gold),
        new("Left mouse", "Throw a body where you look", Color.Gold),
        new(""),
        new($"Launching: {shapes[fountain.Kind].Name}", shapes[fountain.Kind].Colour),
        new($"{fountain.Rate:0} bodies per simulated second", Color.LightGreen),
        new($"{fountain.Count} of {fountain.Capacity} bodies, {fountain.Recycled} places reused", Color.LightGreen),
        new(timeScales[timeScaleIndex] > 0f ? $"Simulation at {timeScales[timeScaleIndex]:0.##}x" : "Simulation paused", Color.LightGreen),
        new("The rate is counted in physics steps,", Color.LightGray),
        new("so the fountain keeps the simulation's time.", Color.LightGray),
    ]);

    return lines;
}

InterpolationMode Interpolation() => interpolated ? InterpolationMode.Interpolated : InterpolationMode.None;

/// <summary>
/// One body: a dynamic Bepu body with the collider that fits its shape and no model of its own.
/// The shape's master draws it.
/// </summary>
Entity CreateBody(Scene scene, int kind, Vector3 position)
{
    // AddBepu3DPhysics derives the collider from the primitive type and size; it needs a model
    // component to be present, which is then taken off again
    var body = new Entity(shapes[kind].Name) { new ModelComponent(models[kind]) };

    body.AddBepu3DPhysics(shapes[kind].Type, new Bepu3DPhysicsOptions
    {
        Size = shapes[kind].Size,

        // A body is not interpolated unless asked: by default it is moved on physics steps only
        Component = new BodyComponent { Collider = new CompoundCollider(), InterpolationMode = Interpolation() },
    });

    body.Remove<ModelComponent>();

    body.Transform.Position = position;
    body.Scene = scene;

    masters[kind].AddInstance(body);

    return body;
}

/// <summary>Takes a body out of its master's instances and out of the scene.</summary>
void RemoveBody(Entity body)
{
    foreach (var master in masters) master.RemoveInstance(body);

    body.Scene = null;
}

/// <summary>The nozzle in the middle and four low walls around it, all static.</summary>
void BuildBasin(Scene scene)
{
    var stone = game.CreateMaterial(new Color(150, 150, 158), metalness: 0f, glossiness: 0.3f);

    // A cylinder's size is its radius in X and its height in Z
    Static(PrimitiveModelType.Cylinder, new Vector3(0.3f, 0f, NozzleHeight), new Vector3(0f, NozzleHeight * 0.5f, 0f));

    const float WallHeight = 0.8f;
    const float WallThickness = 0.3f;

    var length = BasinHalfSize * 2f + WallThickness;

    Static(PrimitiveModelType.Cube, new Vector3(length, WallHeight, WallThickness), new Vector3(0f, WallHeight * 0.5f, BasinHalfSize));
    Static(PrimitiveModelType.Cube, new Vector3(length, WallHeight, WallThickness), new Vector3(0f, WallHeight * 0.5f, -BasinHalfSize));
    Static(PrimitiveModelType.Cube, new Vector3(WallThickness, WallHeight, length), new Vector3(BasinHalfSize, WallHeight * 0.5f, 0f));
    Static(PrimitiveModelType.Cube, new Vector3(WallThickness, WallHeight, length), new Vector3(-BasinHalfSize, WallHeight * 0.5f, 0f));

    void Static(PrimitiveModelType type, Vector3 size, Vector3 position)
    {
        // An empty compound collider: Create3DPrimitive adds the shape that fits the primitive and
        // its size. A shape given here as well would be a second one, at its default size
        var entity = game.Create3DPrimitive(type, new Bepu3DPhysicsOptions
        {
            Size = size,
            Material = stone,
            Component = new StaticComponent { Collider = new CompoundCollider() },
        });

        entity.Transform.Position = position;
        entity.Scene = scene;
    }
}

/*
---example-metadata
slug: cube-fountain
title:
  en: Cube Fountain
  cs: Fontána z krychlí
level: Intermediate
category: Physics
complexity: 4
order: 85
description:
  en: |-
    A fountain of cubes, spheres and cylinders that runs on the physics clock. A nozzle launches
    bodies at a steady rate counted in ISimulationUpdate, the callback Bepu makes once per fixed
    physics step, so the fountain keeps the simulation's time: slow the simulation or pause it and
    the fountain slows or stops with it. Slow motion also shows what body interpolation is for,
    and a key turns it off to compare. Once the fountain owns as many bodies as it may, the oldest
    one's place is reused, so it runs indefinitely with a fixed number of bodies. One instancing
    master per shape draws them all.
  cs: |-
    Fontána z krychlí, koulí a válců, která běží podle fyzikálních hodin. Tryska vypouští tělesa
    stálým tempem počítaným v ISimulationUpdate, zpětném volání, které Bepu provede jednou za pevný
    fyzikální krok, takže fontána drží čas simulace: zpomalte simulaci nebo ji pozastavte a fontána
    zpomalí nebo se zastaví s ní. Zpomalený pohyb také ukazuje, k čemu je interpolace těles, a
    klávesa ji pro srovnání vypne. Jakmile fontána vlastní tolik těles, kolik smí, místo nejstaršího
    se použije znovu, takže běží neomezeně se stálým počtem těles. Všechna kreslí jeden instancing
    master na tvar.
concepts:
  - A spawn rate counted in ISimulationUpdate, with the fraction of a body carried from step to step
  - Why the physics clock - the fountain follows BepuSimulation.TimeScale with no code of its own
  - BodyComponent.InterpolationMode - smooth motion between physics steps, seen in slow motion
  - Reusing a body - Teleport, then a new velocity, and Awake so a sleeping body takes it
  - A cap on the number of bodies, with the oldest one's place reused
  - Throwing a body along the camera's forward vector
  - Bodies without a model of their own, drawn by one BufferedEntityInstancing master per shape
  - A DebugTextDropdown in the overlay to choose the shape
tags:
  - 3D
  - Physics
  - Bepu
  - Spawning
  - Instancing
related:
  - E02_3D_GiveMeACube_SimulationUpdate
  - E10_3D_Instancing_EntityTransform
  - E05_3D_MultipleSimulations
screenshotFrame: 900
enabled: true
created: 2026-10-02
---
*/