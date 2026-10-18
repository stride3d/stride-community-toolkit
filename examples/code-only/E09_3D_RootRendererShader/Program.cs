using E09_3D_RootRendererShader.Renderers;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.Compositing;
using Stride.Engine;
using Stride.Rendering;

using var game = new Game();

var sceneSetup = new SceneSetup3D
{
    Camera = CameraSetup.Perspective(),
    Ground = null,
    Profiler = true
};

game.Run(sceneSetup, start: Start);

void Start(Scene scene)
{
    AddRenderFeature();

    // A component, because its processor is what hands the render objects to the visibility group
    var background = new RibbonBackgroundComponent
    {
        Intensity = 0.9f,
        Frequency = 0.9f,
        Amplitude = 0.9f,
        Speed = 2f,
        WidthFactor = 0.9f
    };

    var entity = new Entity { background };
    scene.Entities.Add(entity);

    game.Window.Position = new Stride.Core.Mathematics.Int2(50, 50);
    game.Window.AllowUserResizing = true;
}

// The render feature that draws a RibbonBackgroundComponent
void AddRenderFeature()
{
    game.SceneSystem.GraphicsCompositor.TryGetRenderStage("Opaque", out var opaqueRenderStage);
    var renderFeature = new RibbonBackgroundRenderFeature()
    {
        RenderStageSelectors =
        {
            new SimpleGroupToRenderStageSelector
            {
                EffectName = "RibbonBackground",
                RenderGroup = RenderGroupMask.All,
                RenderStage = opaqueRenderStage,
            }
        }
    };

    game.AddRootRenderFeature(renderFeature);
}
/*
---example-metadata
slug: root-renderer-shader
title:
  en: Root Renderer Shader
level: Advanced
category: Rendering
complexity: 5
order: 20
description:
  en: |-
    An animated ribbon background drawn by a custom RootRenderFeature, which is the deepest extension
    point Stride offers short of writing your own compositor. Three pieces make it work and each has a
    distinct job: a component holding the settings an author edits, a render object carrying just what
    the shader needs, and the render feature that ties them to a stage. Intensity, frequency, amplitude,
    speed and width are all live properties.
concepts:
  - Writing a custom RootRenderFeature
  - "Splitting state across component, render object and render feature"
  - Registering a render feature with the graphics compositor
  - Choosing the render stage and group a feature draws in
  - Feeding component properties through to shader parameters
tags:
  - 3D
  - Rendering
  - Shader
  - Render Feature
  - Graphics Compositor
  - Background
  - Effects
related:
  - E09_3D_MeshOutline
  - E09_3D_SceneRenderer
enabled: true
created: 2024-12-08
---
*/