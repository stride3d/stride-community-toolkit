using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Rendering.Text;
using Stride.CommunityToolkit.Scripts.Utilities;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Input;
using Stride.Rendering;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;

const float IntensityChangeStep = 0.5f;
DebugOverlaySection? instructions = null;
LightComponent? skyBoxLightComponent = null;
float skyBoxLightIntensity = 0;

using var game = new Game();

game.Run(start: Start, update: Update);

void Start(Scene scene)
{
    game.SetupBase3DScene();
    var skyboxEntity = game.AddSkybox();

    skyBoxLightComponent = skyboxEntity.GetComponent<LightComponent>();
    skyBoxLightIntensity = skyBoxLightComponent?.Intensity ?? 1;

    Create3DPrimitive(scene, new Vector3(-5f, 0.5f, -1f), game.CreateMaterial(Color.Green));
    Create3DPrimitive(scene, new Vector3(-5f, 0.5f, -3f), game.CreateMaterial(Color.Green, 0.1f, 0.1f));
    Create3DPrimitive(scene, new Vector3(-5f, 0.5f, -5f), game.CreateMaterial(Color.Green, 4f, 0.75f));
    Create3DPrimitive(scene, new Vector3(-1f, 0.5f, -1f), GetMaterial1());
    Create3DPrimitive(scene, new Vector3(1f, 0.5f, -1f), GetMaterial2());
    Create3DPrimitive(scene, new Vector3(0f, 0.5f, 1f), GetMaterial3());

    InitializeDebugOverlay();
}

void Create3DPrimitive(Scene scene, Vector3 position, Material material)
{
    var entity = game.Create3DPrimitive(PrimitiveModelType.Cube, new() { Material = material });
    entity.Transform.Position = position;
    entity.Scene = scene;
}

void Update(Scene scene, GameTime time)
{
    if (skyBoxLightComponent == null) return;

    if (game.Input.IsKeyPressed(Keys.Z))
    {
        skyBoxLightIntensity -= IntensityChangeStep;

        skyBoxLightComponent.Intensity = skyBoxLightIntensity;
    }

    if (game.Input.IsKeyPressed(Keys.X))
    {
        skyBoxLightIntensity += IntensityChangeStep;

        skyBoxLightComponent.Intensity = skyBoxLightIntensity;
    }

}

Material GetMaterial1()
{
    return Material.New(game.GraphicsDevice, new()
    {
        Attributes = new()
        {
            MicroSurface = new MaterialGlossinessMapFeature
            {
                GlossinessMap = new ComputeFloat(0.9f)
            },
            Diffuse = new MaterialDiffuseMapFeature
            {
                DiffuseMap = new ComputeColor(new Color4(1, 0.3f, 0.5f, 1))
            },
            DiffuseModel = new MaterialDiffuseLambertModelFeature(),
            Specular = new MaterialMetalnessMapFeature
            {
                MetalnessMap = new ComputeFloat(0.0f)
            },
            SpecularModel = new MaterialSpecularMicrofacetModelFeature
            {
                Environment = new MaterialSpecularMicrofacetEnvironmentGGXPolynomial()
            },
        }
    });
}

Material GetMaterial2()
{
    return Material.New(game.GraphicsDevice, new()
    {
        Attributes = new()
        {
            MicroSurface = new MaterialGlossinessMapFeature
            {
                GlossinessMap = new ComputeFloat(0.9f)
            },
            Diffuse = new MaterialDiffuseMapFeature
            {
                DiffuseMap = new ComputeColor(Color.Blue)
            },
            DiffuseModel = new MaterialDiffuseLambertModelFeature(),
            Specular = new MaterialMetalnessMapFeature
            {
                MetalnessMap = new ComputeFloat(0.0f)
            },
            SpecularModel = new MaterialSpecularMicrofacetModelFeature
            {
                Environment = new MaterialSpecularMicrofacetEnvironmentGGXPolynomial()
            },
        }
    });
}

Material GetMaterial3()
{
    return Material.New(game.GraphicsDevice, new()
    {
        Attributes = new()
        {
            MicroSurface = new MaterialGlossinessMapFeature
            {
                GlossinessMap = new ComputeFloat(0.1f)
            },
            Diffuse = new MaterialDiffuseMapFeature
            {
                DiffuseMap = new ComputeColor(Color.Gold)
            },
            DiffuseModel = new MaterialDiffuseLambertModelFeature(),
            Specular = new MaterialMetalnessMapFeature
            {
                MetalnessMap = new ComputeFloat(0.8f)
            },
            SpecularModel = new MaterialSpecularMicrofacetModelFeature
            {
                Environment = new MaterialSpecularMicrofacetEnvironmentGGXPolynomial()
            },
        }
    });
}

void InitializeDebugOverlay()
{
    var overlay = DebugOverlay.GetOrCreate(game);

    overlay.Position = DisplayPosition.BottomLeft;

    // The callback runs every frame the overlay is drawn, so the live light intensity appears without
    // anything having to push it
    instructions = overlay.AddSection("Game", () => GenerateInstructions(skyBoxLightIntensity));
}

static List<TextElement> GenerateInstructions(float skyBoxLightIntensity)
    => [
            new("GAME INSTRUCTIONS"),
            //new("Click the golden sphere and drag to move it (Y-axis locked)"),
            new("Hold Z to decrease, X to increase Skybox light intensity", Color.Yellow),
            new($"Intensity: {skyBoxLightIntensity}", Color.Yellow),
        ];

/*
---example-metadata
slug: material
title:
  en: Material
level: Beginner
category: Rendering
complexity: 2
order: 20
description:
  en: |-
    A row of cubes that differ only in their material, so the effect of each property is visible in
    isolation. Simple colours with varying glossiness and metalness come first, then materials assembled
    by hand from feature objects - diffuse model, glossiness map, metalness map and a microfacet
    specular model. Z and X change the skybox light intensity while it runs, which matters because a
    metallic surface is almost entirely a reflection of its environment and looks wrong in a vacuum.
concepts:
  - Creating a simple coloured material with CreateMaterial
  - Building a material from MaterialDescriptor feature by feature
  - What glossiness and metalness each change on screen
  - Combining a diffuse model with a microfacet specular model
  - Why environment light intensity dominates a metallic surface
  - Adjusting skybox light at runtime from keyboard input
  - "Using helpers: SetupBase3DScene, AddSkybox, Create3DPrimitive, CreateMaterial"
tags:
  - 3D
  - Rendering
  - Material
  - Skybox
  - Lighting
  - Glossiness
  - Metalness
  - Input
related:
  - Example01_Basic3DScene
  - Example05_ProceduralGeometry
media: stride-game-engine-example-01-material.webp
enabled: true
created: 2025-03-09
---
*/