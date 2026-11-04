using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Rendering.Overlay;
using Stride.CommunityToolkit.Rendering.Text;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Input;
using Stride.Rendering;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;

// What a material's numbers do, in three rows of cubes. A material in Stride is a colour, a
// glossiness (0 rough to 1 mirror) and a metalness (0 dielectric to 1 metal) under two shading
// models, and everything else - textures, glass, hair, layers - is a feature added to that bag.
// The back row sweeps glossiness, the middle row sweeps metalness, and the front row builds the
// same numbers as a MaterialDescriptor, feature by feature, which is what CreateMaterial does for
// you. The manual page "Materials from code" explains the numbers; E02_3D_Material_Gallery shows
// every feature the engine has, on a ring of stations.
//
// A metal has no diffuse colour: its colour is the colour of its reflection, and what it reflects
// is the skybox. Z and X change the skybox light so that can be seen - dim it and the metal end of
// the middle row goes dark while the dielectric end keeps its green.

const float IntensityStep = 0.5f;

LightComponent? skyboxLight = null;
var skyboxIntensity = 0f;

using var game = new Game();

game.Run(new SceneSetup3D().UseBepu(), start: Start, update: Update);

void Start(Scene scene)
{
    skyboxLight = game.AddSkybox().Get<LightComponent>();
    skyboxIntensity = skyboxLight?.Intensity ?? 1;

    // Back row: glossiness from rough to mirror, on a dielectric. Rough spreads the highlight into a
    // haze; glossy tightens it to a point and brings the sky into focus in it.
    for (var i = 0; i < 5; i++)
    {
        var glossiness = i / 4f;

        Cube(scene, new Vector3(-9f + 2f * i, 0.5f, -4f), game.CreateMaterial(Color.Green, metalness: 0f, glossiness: glossiness), $"glossiness {glossiness:0.00}");
    }

    // Middle row: metalness from dielectric to metal, at one glossiness. The dielectric keeps its
    // green as diffuse and reflects a colourless 4 percent; the metal has no diffuse at all and
    // reflects the sky in green.
    for (var i = 0; i < 5; i++)
    {
        var metalness = i / 4f;

        Cube(scene, new Vector3(-9f + 2f * i, 0.5f, -1f), game.CreateMaterial(Color.Green, metalness: metalness, glossiness: 0.65f), $"metalness {metalness:0.00}");
    }

    // Front row: the same numbers written out as a descriptor - a glossy pink and a glossy blue
    // dielectric, and a rough gold metal
    Cube(scene, new Vector3(-7f, 0.5f, 2f), FromDescriptor(new Color(255, 77, 128), glossiness: 0.9f, metalness: 0f), "pink 0.90 / 0.00");
    Cube(scene, new Vector3(-5f, 0.5f, 2f), FromDescriptor(Color.Blue, glossiness: 0.9f, metalness: 0f), "blue 0.90 / 0.00");
    Cube(scene, new Vector3(-3f, 0.5f, 2f), FromDescriptor(Color.Gold, glossiness: 0.1f, metalness: 0.8f), "gold 0.10 / 0.80");

    AddInstructions();
}

void Cube(Scene scene, Vector3 position, Material material, string label)
{
    var entity = game.CreatePrimitive(Primitive3D.Cube(), new() { Material = material });

    entity.Transform.Position = position;

    // The number above each cube: screen-space text anchored to the cube's centre, nudged up
    entity.Add(new EntityTextComponent
    {
        Text = label,
        FontSize = 14,
        Anchor = Anchor.BottomCenter,
        Offset = new Vector2(0, -34),
        EnableShadow = true,
    });

    entity.Scene = scene;
}

// The five features CreateMaterial fills in: a colour node in the diffuse slot, the Lambert diffuse
// model, a number in the glossiness slot, a number in the metalness slot, and the microfacet specular
// model. Any slot that takes a number can take a texture or an expression instead, and either model
// can be swapped for another - which is how the material gallery gets to glass, cel shading and hair.
// The descriptor overload of CreateMaterial compiles it with the content manager: the microfacet model's
// environment term is a lookup texture the engine ships as an asset, and without the content manager
// nothing throws and every metal renders black.
Material FromDescriptor(Color colour, float glossiness, float metalness) => game.CreateMaterial(new MaterialDescriptor
{
    Attributes =
    {
        Diffuse = new MaterialDiffuseMapFeature(new ComputeColor(colour)),
        DiffuseModel = new MaterialDiffuseLambertModelFeature(),
        MicroSurface = new MaterialGlossinessMapFeature(new ComputeFloat(glossiness)),
        Specular = new MaterialMetalnessMapFeature(new ComputeFloat(metalness)),
        SpecularModel = new MaterialSpecularMicrofacetModelFeature(),
    },
});

void Update(Scene scene, GameTime time)
{
    if (skyboxLight is null) return;

    if (game.Input.IsKeyPressed(Keys.Z)) skyboxIntensity = Math.Max(0f, skyboxIntensity - IntensityStep);
    if (game.Input.IsKeyPressed(Keys.X)) skyboxIntensity += IntensityStep;

    skyboxLight.Intensity = skyboxIntensity;
}

void AddInstructions()
{
    // The callback runs every frame the overlay draws, so the intensity shown stays live
    TextOverlay.GetOrCreate(game).AddSection("Materials", () =>
    [
        new("Z", "Dim the skybox light", Color.Gold),
        new("X", "Brighten the skybox light", Color.Gold),
        new(""),
        new($"Skybox light intensity: {skyboxIntensity:0.00}", Color.LightGreen),
        new(""),
        new("Back row: glossiness 0 to 1, metalness 0", Color.LightGray),
        new("Middle row: metalness 0 to 1, glossiness 0.65", Color.LightGray),
        new("Front row: the same numbers as a MaterialDescriptor", Color.LightGray),
        new("Every feature the engine has: E02_3D_Material_Gallery", Color.LightGray),
    ]);
}

/*
---example-metadata
slug: material
title:
  en: 3D Material
level: Beginner
category: Rendering
complexity: 2
order: 20
description:
  en: |-
    The four numbers of a material over three rows of cubes: a glossiness sweep from rough to mirror, a
    metalness sweep from dielectric to metal, and a front row that builds the same numbers as a
    MaterialDescriptor feature by feature - which is what CreateMaterial does. Each cube carries its
    number. Z and X change the skybox light while it runs, because a metal is almost entirely a
    reflection of its environment and goes dark without one.
concepts:
  - Creating a material from a colour, a glossiness and a metalness with CreateMaterial
  - What glossiness changes on screen, from rough to mirror
  - What metalness changes on screen, and why a metal has no diffuse colour
  - Building the same material from MaterialDescriptor feature by feature
  - Why the microfacet model's environment term matters in a code-only game
  - Labelling entities with screen-space EntityTextComponent
  - Adjusting the skybox light at runtime from keyboard input
  - "Using helpers: SceneSetup3D, AddSkybox, CreatePrimitive, CreateMaterial"
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
  - E02_2D_Material
  - E01_3D_BasicScene
  - E02_3D_Material_Gallery
  - E02_3D_MaterialPreview
  - E07_3D_ProceduralGeometry
enabled: true
created: 2025-03-09
---
*/