using Example13_MeshOutline;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;

// Shows how a custom RootRenderFeature can draw outlines around 3D primitives.
// The original code is from https://github.com/herocrab/StrideMeshOutlineRenderFeature

// How far the outline sticks out, as a fraction of the model's size
const float OutlineThickness = 0.03f;

// Outlines are drawn into the HDR buffer, so values well above 1 make them glow through bloom
const float OutlineIntensity = 100f;

using var game = new Game();

game.Run(start: Start);

void Start(Scene rootScene)
{
    // Set up a basic 3D scene with lighting and camera
    game.SetupBase3DScene();

    // Add a default skybox to the scene for background visuals
    game.AddSkybox();

    // From here on, any entity carrying a MeshOutlineComponent gets an outline
    game.AddRootRenderFeature(new MeshOutlineRenderFeature { Thickness = OutlineThickness });

    CreateOutlinedPrimitive(rootScene, PrimitiveModelType.Sphere, Color.Cyan, new Vector3(2f, 0.5f, -2f));
    CreateOutlinedPrimitive(rootScene, PrimitiveModelType.Capsule, Color.Yellow, new Vector3(-1f, 0.5f, -2f));
    CreateOutlinedPrimitive(rootScene, PrimitiveModelType.Sphere, Color.Red, new Vector3(-1f, 0.5f, 4f));
    CreateOutlinedPrimitive(rootScene, PrimitiveModelType.Capsule, Color.Green, new Vector3(2f, 0.5f, 1f));
    CreateOutlinedPrimitive(rootScene, PrimitiveModelType.Sphere, Color.Magenta, new Vector3(-1f, 0.5f, 1f));
    // Cube has hard per-face normals (duplicated vertices at each edge), so inflating along the
    // normal pushes adjacent faces apart instead of outward as one shell - expect visible gaps/
    // seams at the cube's edges and corners, unlike the smooth outline on curved primitives like
    // Sphere/Capsule.
    CreateOutlinedPrimitive(rootScene, PrimitiveModelType.Cube, Color.Orange, new Vector3(2f, 0.5f, 4f));
}

/// <summary>
/// Creates a 3D primitive, gives it an outline, and adds it to the scene.
/// </summary>
/// <param name="rootScene">The scene to which the entity will be added.</param>
/// <param name="modelType">The type of primitive model to create (e.g., Sphere, Capsule).</param>
/// <param name="color">The color of the outline effect.</param>
/// <param name="position">The position of the entity in the scene.</param>
void CreateOutlinedPrimitive(Scene rootScene, PrimitiveModelType modelType, Color4 color, Vector3 position)
{
    var entity = game.Create3DPrimitive(modelType);

    entity.Transform.Position = position;

    // The presence of this component is what makes the render feature draw an outline
    entity.Add(new MeshOutlineComponent
    {
        Color = color,
        Intensity = OutlineIntensity
    });

    entity.Scene = rootScene;
}

/*
---example-metadata
slug: mesh-outline
title:
  en: Mesh Outline Render Feature
  cs: Obrysy modelů pomocí vlastní render feature
level: Advanced
category: Rendering
complexity: 4
order: 30
description:
  en: |-
    Draw coloured outlines around 3D primitives with a custom RootRenderFeature. Each mesh is drawn a
    second time, inflated along its normals with front faces culled, so only a shell remains visible
    behind the original mesh. Adding a MeshOutlineComponent to an entity is all it takes to outline it,
    and a high intensity pushes the outline into HDR range so bloom makes it glow.
  cs: |-
    Vykreslení barevných obrysů kolem 3D tvarů pomocí vlastní RootRenderFeature. Každý model se vykreslí
    podruhé, nafouknutý podél normál a s odstraněnými přivrácenými stěnami, takže za původním modelem
    zůstane viditelná jen skořepina. K vytvoření obrysu stačí entitě přidat MeshOutlineComponent
    a vysoká intenzita posune obrys do HDR rozsahu, takže díky bloomu září.
concepts:
  - Writing a custom RootRenderFeature
  - Registering a render feature with AddRootRenderFeature
  - Driving rendering from a custom EntityComponent
  - Building a MutablePipelineState and applying a DynamicEffectInstance
  - Inflating a mesh along its normals in a shader
  - Skipping render stages that bind no render target
  - "Using helpers: SetupBase3DScene"
  - "Using helpers: AddSkybox"
tags:
  - 3D
  - Rendering
  - Render Feature
  - Outline
  - Shader
  - SDSL
  - Pipeline State
  - Entity Component
  - HDR
  - Bloom
related:
  - Example13_RootRendererShader
  - Example09_Renderer
  - Example01_Basic3DScene
media: stride-game-engine-example-13-mesh-outline.webp
tocName: Mesh Outline
enabled: true
created: 2025-08-07
---
*/