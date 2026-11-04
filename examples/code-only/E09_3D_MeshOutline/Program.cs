using E09_3D_MeshOutline;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;

// Shows how a custom RootRenderFeature can draw outlines around 3D primitives.
// The original code is from https://github.com/herocrab/StrideMeshOutlineRenderFeature

// How far the outline sticks out, in model units
const float OutlineThickness = 0.03f;

// Outlines are drawn into the HDR buffer, so values well above 1 would glow if bloom were enabled
const float OutlineIntensity = 100f;

using var game = new Game();

game.Run(new SceneSetup3D().UseBepu().UseSkybox(), start: Start);

void Start(Scene rootScene)
{
    // From here on, any entity carrying a MeshOutlineComponent gets an outline
    game.AddRootRenderFeature(new MeshOutlineRenderFeature { Thickness = OutlineThickness });

    CreateOutlinedPrimitive(rootScene, Primitive3D.Sphere(), Color.Cyan, new Vector3(2f, 0.5f, -2f));
    CreateOutlinedPrimitive(rootScene, Primitive3D.Capsule(), Color.Yellow, new Vector3(-1f, 0.5f, -2f));
    CreateOutlinedPrimitive(rootScene, Primitive3D.Sphere(), Color.Red, new Vector3(-1f, 0.5f, 4f));
    CreateOutlinedPrimitive(rootScene, Primitive3D.Capsule(), Color.Green, new Vector3(2f, 0.5f, 1f));
    CreateOutlinedPrimitive(rootScene, Primitive3D.Sphere(), Color.Magenta, new Vector3(-1f, 0.5f, 1f));
    // The cube's per-face normals push its faces apart, so its outline has gaps at the edges and corners
    CreateOutlinedPrimitive(rootScene, Primitive3D.Cube(), Color.Orange, new Vector3(2f, 0.5f, 4f));
}

void CreateOutlinedPrimitive(Scene rootScene, Primitive3D modelType, Color4 color, Vector3 position)
{
    var entity = game.CreatePrimitive(modelType, new PrimitiveEntityOptions { Position = position });

    entity.Add(new MeshOutlineComponent { Color = color, Intensity = OutlineIntensity });

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
    and a high intensity pushes the outline into HDR range, ready to glow under a bloom effect, none
    being enabled here.
  cs: |-
    Vykreslení barevných obrysů kolem 3D tvarů pomocí vlastní RootRenderFeature. Každý model se vykreslí
    podruhé, nafouknutý podél normál a s odstraněnými přivrácenými stěnami, takže za původním modelem
    zůstane viditelná jen skořepina. K vytvoření obrysu stačí entitě přidat MeshOutlineComponent
    a vysoká intenzita posune obrys do HDR rozsahu, připravený zářit pod efektem bloom, který zde
    zapnutý není.
concepts:
  - Writing a custom RootRenderFeature
  - Registering a render feature with AddRootRenderFeature
  - Driving rendering from a custom EntityComponent
  - Building a MutablePipelineState and applying a DynamicEffectInstance
  - Inflating a mesh along its normals in a shader
  - Skipping render stages that bind no render target
  - "Using helpers: SceneSetup3D, UseSkybox"
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

related:
  - E09_3D_RootRendererShader
  - E09_3D_SceneRenderer
  - E01_3D_BasicScene
tocName: Mesh Outline
enabled: true
created: 2025-08-07
---
*/