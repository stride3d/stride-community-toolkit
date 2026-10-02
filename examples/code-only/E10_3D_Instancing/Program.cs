using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Scripts.Utilities;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Input;
using Stride.Rendering;

// Two walls of the same cube, one green and one orange, built two different ways:
//
//   LEFT  - green: a single entity plus an array of transformation matrices. The renderer
//           issues ONE draw call for the whole wall.
//   RIGHT - orange: one entity per cube. The renderer issues one draw call per cube.
//
// Both walls use the same cube mesh and differ only in colour, so the only rendering difference
// between them is instancing.
// Press 1 and 2 to toggle each wall and watch the frame rate in the profiler.
//
// On a typical desktop the 2000 individual cubes run at roughly 35 FPS (~29 ms per frame) while the
// 2000 instanced cubes run at roughly 380 FPS (~2.6 ms) - about eleven times faster for the same wall
// of cubes. Give the frame rate a couple of seconds to settle after toggling: the counter
// is a rolling average, so it lags the change.

const int GridWidth = 20;
const int GridHeight = 20;
const int GridDepth = 5;
const float Spacing = 2.2f;

// How far each wall sits from the middle of the scene
const float WallOffset = 25f;

var cubeCount = GridWidth * GridHeight * GridDepth;

// Kept so the toggles can switch each wall on and off
var individualCubes = new List<ModelComponent>();
ModelComponent? instancedWall = null;

var showIndividual = true;
var showInstanced = true;

using var game = new Game();

game.Run(start: Start, update: Update);

void Start(Scene rootScene)
{
    // Camera, directional light and a graphics compositor. Note this is SetupBase3D, not
    // SetupBase3DScene: no ground and no physics, because this example is purely about rendering.
    game.SetupBase3D();
    game.Add3DCameraController();
    game.AddSkybox();
    game.AddProfiler();
    DebugOverlay.GetOrCreate(game).AddSection("Instancing", OverlayLines);
    game.SetCameraPosition(new(33, 65, -90));
    game.SetCameraRotation(new(161, -22, 0));

    EnableInstancing();

    CreateIndividualCubes(rootScene, CreateSharedCubeModel(rootScene, Color.Orange));
    CreateInstancedCubes(rootScene, CreateSharedCubeModel(rootScene, Color.LightGreen));
}

/// <summary>
/// Adds the render feature that performs instanced drawing.
/// </summary>
/// <remarks>
/// This step is easy to miss and nothing warns you about it. The code-built compositor from
/// <c>GraphicsCompositorHelper.CreateDefault</c>, which the toolkit uses, wires up Transform,
/// Skinning, Material, ShadowCaster and lighting - but not instancing. Without this call the
/// instanced wall renders as a single cube.
/// </remarks>
void EnableInstancing()
{
    var meshRenderFeature = game.SceneSystem.GraphicsCompositor.RenderFeatures.OfType<MeshRenderFeature>().First();

    meshRenderFeature.RenderFeatures.Add(new InstancingRenderFeature());
}

/// <summary>
/// Creates one cube entity and returns its <see cref="Model"/>, so both walls can share it.
/// </summary>
/// <remarks>
/// Sharing matters for a fair comparison. Calling Create3DPrimitive per cube would also generate a
/// separate vertex and index buffer per cube, so the slow side would be losing on memory as well as
/// on draw calls, and the measurement would not be about instancing any more.
/// </remarks>
Model CreateSharedCubeModel(Scene rootScene, Color color)
{
    var prototype = game.Create3DPrimitive(PrimitiveModelType.Cube, new Primitive3DEntityOptions()
    {
        Material = game.CreateMaterial(color)
    });

    // Park it out of sight; it exists only to own the Model
    prototype.Transform.Position = new Vector3(0, -100, 0);
    prototype.Scene = rootScene;

    return prototype.Get<ModelComponent>().Model;
}

/// <summary>
/// The straightforward approach: one entity per cube, each with its own <see cref="ModelComponent"/>.
/// Costs one draw call per cube.
/// </summary>
void CreateIndividualCubes(Scene rootScene, Model sharedModel)
{
    foreach (var position in GridPositions(-WallOffset))
    {
        var entity = new Entity { new ModelComponent(sharedModel) };

        entity.Transform.Position = position;
        entity.Scene = rootScene;

        individualCubes.Add(entity.Get<ModelComponent>());
    }
}

/// <summary>
/// The instanced approach: a single entity carrying an <see cref="InstancingComponent"/>, plus one
/// world matrix per cube. Costs one draw call for the whole wall.
/// </summary>
void CreateInstancedCubes(Scene rootScene, Model sharedModel)
{
    var matrices = new Matrix[cubeCount];
    var index = 0;

    foreach (var position in GridPositions(WallOffset))
    {
        // A full world matrix, so it could carry rotation and scale too
        matrices[index++] = Matrix.Translation(position);
    }

    // The instancing component lives alongside a normal ModelComponent: that model is what gets
    // drawn, once, for every matrix in the array.
    var entity = new Entity
    {
        new ModelComponent(sharedModel),
        new InstancingComponent { Type = new InstancingUserArray() }
    };

    // Call UpdateWorldMatrices rather than assigning the WorldMatrices field. InstanceCount has a
    // private setter and only this method sets it, so assigning the field leaves the count at zero
    // and nothing is drawn.
    ((InstancingUserArray)entity.Get<InstancingComponent>().Type).UpdateWorldMatrices(matrices);

    entity.Scene = rootScene;

    instancedWall = entity.Get<ModelComponent>();
}

/// <summary>
/// Walks a 3D grid of positions, centred horizontally on <paramref name="offsetX"/>.
/// </summary>
IEnumerable<Vector3> GridPositions(float offsetX)
{
    for (var x = 0; x < GridWidth; x++)
    {
        for (var y = 0; y < GridHeight; y++)
        {
            for (var z = 0; z < GridDepth; z++)
            {
                yield return new Vector3(
                    offsetX + (x - GridWidth / 2f) * Spacing,
                    1f + y * Spacing,
                    (z - GridDepth / 2f) * Spacing);
            }
        }
    }
}

void Update(Scene rootScene, GameTime time)
{
    HandleInput();
}

void HandleInput()
{
    if (!game.Input.HasKeyboard) return;

    if (game.Input.IsKeyPressed(Keys.D2))
    {
        showIndividual = !showIndividual;

        foreach (var model in individualCubes)
        {
            model.Enabled = showIndividual;
        }
    }

    if (game.Input.IsKeyPressed(Keys.D1))
    {
        showInstanced = !showInstanced;

        if (instancedWall is not null) instancedWall.Enabled = showInstanced;
    }
}

IReadOnlyList<TextElement> OverlayLines() =>
[
    new("1", $"Instanced wall {(showInstanced ? "shown" : "hidden")}", showInstanced ? Color.LightGreen : Color.Gray),
    new("2", $"Individual wall {(showIndividual ? "shown" : "hidden")}", showIndividual ? Color.Orange : Color.Gray),
    new(""),
    new($"Left wall: 1 entity, 1 draw call, {cubeCount} instances", Color.LightGreen),
    new($"Right wall: {cubeCount} entities, {cubeCount} draw calls", Color.Orange),
    new($"Visible: {(showIndividual && showInstanced ? "both walls" : showIndividual ? $"individual ({cubeCount} draws)" : showInstanced ? "instanced (1 draw)" : "nothing")}", Color.Yellow),
    new("Both walls draw the same cube; only the colour differs.", Color.LightGray),
    new("Toggle each one and compare the frame rate above.", Color.LightGray),
    new("The counter is a rolling average; give it a second.", Color.LightGray),
];

/*
---example-metadata
slug: instancing
title:
  en: GPU Instancing
  cs: GPU instancing
level: Intermediate
category: Performance
complexity: 3
order: 130
description:
  en: |-
    Render two walls of the same cube built two different ways, side by side. The right wall uses one
    entity per cube and costs one draw call each; the left wall uses a single entity with an
    InstancingComponent and an array of world matrices, and costs one draw call in total. Each wall
    shares one Model across its cubes, so the only rendering difference is instancing. Toggle each wall to compare the frame rate, and
    note that the InstancingRenderFeature has to be added to the compositor by hand in code-only projects.
  cs: |-
    Vykreslení dvou stěn ze stejné kostky postavených dvěma způsoby vedle sebe. Pravá stěna používá jednu
    entitu na kostku a stojí jedno vykreslovací volání za každou z nich; levá stěna používá jedinou entitu
    s komponentou InstancingComponent a pole světových matic a stojí celkem jedno volání. Každá stěna
    sdílí jeden Model pro všechny své kostky, takže jediným rozdílem ve vykreslování je instancing. Přepínáním stěn porovnáte snímkovou frekvenci.
    Pozor, v projektech psaných pouze kódem je nutné přidat InstancingRenderFeature do kompozitoru ručně.
concepts:
  - Reducing draw calls with an InstancingComponent
  - Building an InstancingUserArray from world matrices
  - Registering InstancingRenderFeature on the MeshRenderFeature
  - Sharing one Model between many entities
  - Toggling a ModelComponent to compare rendering cost
  - "Using helpers: SetupBase3D"
  - "Using helpers: Add3DCameraController"
  - "Using helpers: AddProfiler"
tags:
  - 3D
  - Rendering
  - Instancing
  - Draw Calls
  - Performance
  - GPU
  - Model
  - Compositor
related:
  - E10_3D_Instancing_EntityTransform
  - E02_3D_Primitives
  - E09_3D_SceneRenderer
enabled: true
created: 2026-08-07
---
*/