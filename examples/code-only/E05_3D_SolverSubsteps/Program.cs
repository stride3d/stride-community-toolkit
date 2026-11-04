using Stride.BepuPhysics;
using Stride.BepuPhysics.Definitions;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.Overlay;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Rendering.Text;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Input;
using Stride.Rendering;

// Why a tall stack wobbles, and what firms it up. The solver resolves every contact a few times per
// step; a stack of twenty blocks is a chain of contacts that each pass only partly settles, so the
// errors creep up the stack and the top block drifts, sways or falls. Bepu's answer is substepping:
// the step is cut into pieces and each piece is solved with the velocities integrated between them,
// which the solver's own author rates far above more iterations of a single step.
//
// Two simulations run side by side with the same stack, both with one velocity iteration so that the
// substep count is the only difference: one on the left, four on the right. Both counts are fixed for
// a simulation's life, which is why they are set in the setup's GameSettings and why two simulations
// are needed to compare. The overlay reads the drift of each top block from the stack's axis, and
// Space builds the stacks again. E05_3D_MultipleSimulations is the example for the two-simulation
// pattern itself; here it is only the way to show both settings in one window.

const int LeftSimulation = 0;
const int RightSimulation = 1;
const int StackHeight = 20;
const float BlockSize = 0.8f;
const float LaneOffset = 3.5f;

const int RightSubsteps = 4;

var topBlocks = new BodyComponent?[2];
var lanes = new List<Entity>[] { [], [] };

using var game = new Game();

var sceneSetup = new SceneSetup3D
{
    GameSettings = settings =>
    {
        var bepu = settings.GetOrCreateConfiguration<BepuConfiguration>();

        // Same solver on both, one knob apart: how many pieces each step is cut into
        bepu.BepuSimulations.Add(new BepuSimulation { SolverIteration = 1, SolverSubStep = 1 });
        bepu.BepuSimulations.Add(new BepuSimulation { SolverIteration = 1, SolverSubStep = RightSubsteps });
    },
    Ground = null,
    Camera = CameraSetup.Perspective(position: new Vector3(4f, 10f, 34f), rotation: new Vector3(0f, -5f, 0f)).WithController(),
    WindowTitle = "Solver substeps - Stride Community Toolkit",
    Resizable = true,
    DpiAware = true,
    Profiler = true,
}.UseBepu().UseSkybox();

game.Run(sceneSetup, start: Start, update: Update);

void Start(Scene scene)
{
    AddGround(scene, "Left ground", new Vector3(-LaneOffset, 0f, 0f), LeftSimulation);
    AddGround(scene, "Right ground", new Vector3(LaneOffset, 0f, 0f), RightSimulation);

    Build(scene);

    TextOverlay.GetOrCreate(game).AddSection("Solver substeps", BuildOverlayLines);
}

void Update(Scene scene, GameTime time)
{
    if (game.Input.IsKeyPressed(Keys.Space)) Build(scene);
}

void AddGround(Scene scene, string name, Vector3 position, int simulation)
{
    var ground = game.Add3DGround(new Vector2(6f), new() { EntityName = name, Position = position });

    ground.Get<StaticComponent>().SimulationSelector = new IndexBasedSimulationSelector { Index = simulation };
}

/// <summary>Both lanes again from nothing.</summary>
void Build(Scene scene)
{
    for (var lane = 0; lane < 2; lane++)
    {
        foreach (var entity in lanes[lane]) entity.Scene = null;

        lanes[lane].Clear();

        var x = lane == LeftSimulation ? -LaneOffset : LaneOffset;
        var colour = lane == LeftSimulation ? new Color(230, 140, 80) : new Color(90, 190, 130);
        var material = game.CreateMaterial(colour, metalness: 0f, glossiness: 0.4f);

        for (var i = 0; i < StackHeight; i++)
        {
            var block = AddBlock(scene, $"Block {i + 1}", Primitive3D.Cube(BlockSize), material, new Vector3(x, BlockSize * (i + 0.5f), 0f), lane);

            if (i == StackHeight - 1) topBlocks[lane] = block.Get<BodyComponent>();
        }
    }
}

Entity AddBlock(Scene scene, string name, Primitive3D shape, Material material, Vector3 position, int lane)
{
    var block = game.CreatePrimitive(shape, new BepuPhysicsOptions
    {
        EntityName = name,
        Material = material,
        ShareModel = true,
        Position = position,
    });

    var body = block.Get<BodyComponent>();

    // Chosen before the entity joins the scene, so the body attaches to its lane's simulation from the start
    body.SimulationSelector = new IndexBasedSimulationSelector { Index = lane };
    block.Scene = scene;
    lanes[lane].Add(block);

    return block;
}

// How far the top block has strayed from the stack's axis, in the ground plane
float Drift(int lane)
{
    if (topBlocks[lane] is not { } top) return 0f;

    var position = top.Entity.Transform.Position;
    var axis = lane == LeftSimulation ? -LaneOffset : LaneOffset;

    return new Vector2(position.X - axis, position.Z).Length();
}

IReadOnlyList<TextElement> BuildOverlayLines() =>
[
    new("Space", "Build both stacks again", Color.Gold),
    new(""),
    new($"Left: 1 substep, top block drift {Drift(LeftSimulation):0.00} m", new Color(230, 140, 80)),
    new($"Right: {RightSubsteps} substeps, top block drift {Drift(RightSimulation):0.00} m", new Color(90, 190, 130)),
    new("Both with one velocity iteration", Color.LightGray),
];

/*
---example-metadata
slug: solver-substeps
title:
  en: Solver Substeps
level: Intermediate
category: Physics
complexity: 2
order: 87
description:
  en: |-
    Why a tall stack wobbles and what firms it up. Two simulations run the same stack of twenty
    blocks, one with a single solver substep and one with four, both with one velocity iteration so
    the substeps are the only difference;
    the overlay reads how far each top block has drifted from the stack's axis. Substepping cuts
    each step into pieces solved in turn, which steadies a chain of contacts far more than extra
    iterations of one step. The count is fixed for a simulation's life, so it is set in the setup's
    GameSettings.
concepts:
  - SolverSubStep and SolverIteration on a BepuSimulation, set through the setup's GameSettings for the simulation's life
  - A tall stack as a chain of contacts whose solver error climbs
  - Two simulations side by side to compare one setting
  - One shared model for every block, through ShareModel
  - "Using helpers: SceneSetup3D, UseBepu, UseSkybox, Add3DGround, CreatePrimitive, CreateMaterial, TextOverlay"
tags:
  - 3D
  - Physics
  - Bepu
  - Solver
  - Stability
related:
  - E05_3D_MultipleSimulations
  - E05_3D_TimeControl
  - E10_2D_StressPile
screenshotFrame: 300
enabled: true
created: 2026-11-01
---
*/