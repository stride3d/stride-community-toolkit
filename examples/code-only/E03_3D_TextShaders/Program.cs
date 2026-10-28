using E03_3D_TextShaders;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering;
using Stride.CommunityToolkit.Rendering.Overlay;
using Stride.CommunityToolkit.Rendering.Text.Meshes;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;

// Four materials on the same solid lettering. A text mesh is geometry like any other, with normals and
// texture coordinates (across the text block on the caps, along the outline and through the depth on
// the walls), so everything the material system can do to a sphere it can do to a word:
//
//   1. Gradient: a one-pixel-wide texture read through the caps' texture coordinates, top to bottom.
//      No shader of this example's own; a texture node in the diffuse slot.
//   2. Rim: an emissive colour from a shader class of twenty lines (TextRim.sdsl), bright where the
//      surface turns away from the eye. A node of your own in a slot.
//   3. Wave: the vertex stage lifts every vertex by a wave running along the word, on the engine's
//      own clock (TextWave.sdsl through WaveFeature). A feature of your own in a stage.
//   4. Dissolve: the pixel stage discards by noise over the texture coordinates and glows at the edge,
//      with the amount driven every frame (TextDissolve.sdsl through DissolveFeature).
//
// The shader classes are in Effects and compiled by the asset compiler at build, like any shader; the
// keys classes they produce (TextWaveKeys, TextDissolveKeys, TextRimKeys) are generated with them.
// E02_3D_Material_Gallery has the same ideas on primitives, and more of them.

Material? dissolve = null;

using var game = new Game();

var sceneSetup = new SceneSetup3D
{
    // Straight in front of the words and a little to the right, so they sit left of the overlay
    Camera = CameraSetup.Perspective(position: new Vector3(1.4f, 2.3f, 6.2f), rotation: new Vector3(0f, -4f, 0f)).WithController(),
    Profiler = true,
    WindowTitle = "3D text shaders - Stride Community Toolkit",
    Resizable = true,
    DpiAware = true,
}.UseSkybox();

game.Run(sceneSetup, start: Start, update: Update);

void Start(Scene scene)
{
    // Six lights down the world axes, so the lettering reads from every side
    game.AddAllDirectionLighting(intensity: 20f, showLightGizmo: false);

    dissolve = DissolveMaterial();

    AddText(scene, "gradient", 3.9f, GradientMaterial());
    AddText(scene, "rim light", 2.8f, RimMaterial());
    AddText(scene, "wave", 1.7f, WaveMaterial());
    AddText(scene, "dissolve", 0.6f, dissolve);

    TextOverlay.GetOrCreate(game).AddSection("Text shaders", static () =>
    [
        new("Gradient: a texture through the cap coordinates", Color.LightGray),
        new("Rim: TextRim.sdsl, an emissive colour node", Color.LightGray),
        new("Wave: TextWave.sdsl, a vertex-stage feature", Color.LightGray),
        new("Dissolve: TextDissolve.sdsl, a pixel-stage feature", Color.LightGray),
    ]);
}

void Update(Scene scene, GameTime time)
{
    // The dissolve breathes: its amount is a pass parameter, set every frame through the generated key
    var amount = 0.3f + 0.25f * MathF.Sin((float)time.Total.TotalSeconds * 0.8f);

    dissolve?.Passes[0].Parameters.Set(TextDissolveKeys.DissolveAmount, amount);
}

/// <summary>One word, bevelled, at a height, with a material.</summary>
void AddText(Scene scene, string word, float y, Material material)
{
    var options = new TextMeshOptions { Size = 0.75f, Depth = 0.25f, Anchor = Anchor.MiddleCenter, Bevel = new TextBevel(size: 0.015f, thickness: 0.03f, segments: 3) };

    var entity = new Entity($"Text {word}")
    {
        new ModelComponent { Model = TextMesh.CreateModel(game.GraphicsDevice, material, MeshFont.Default, word, options) },
    };

    entity.Transform.Position = new Vector3(0f, y, 0f);
    entity.Scene = scene;
}

// Every material here starts from the toolkit's PBR descriptor - a colour, a glossiness, a metalness,
// Lambert and the microfacet model - and changes one slot. The slot is the lesson.

/// <summary>
/// A gradient texture, one pixel wide and 64 tall, in the diffuse slot: the caps' texture coordinates
/// run 0..1 top to bottom across the text block, so every letter shades from the top colour to the
/// bottom one; the walls run their v through the depth, so they take the whole gradient front to back.
/// </summary>
Material GradientMaterial()
{
    const int Height = 64;
    var pixels = new Color[Height];
    var top = new Color(255, 215, 90);
    var bottom = new Color(220, 70, 60);

    for (var i = 0; i < Height; i++)
    {
        pixels[i] = Color.Lerp(top, bottom, i / (float)(Height - 1));
    }

    var gradient = Texture.New2D(game.GraphicsDevice, 1, Height, PixelFormat.R8G8B8A8_UNorm_SRgb, pixels);
    var descriptor = MaterialDescriptors.Pbr(Color.White, metalness: 0f, glossiness: 0.55f);

    descriptor.Attributes.Diffuse = new MaterialDiffuseMapFeature(new ComputeTextureColor(gradient) { AddressModeU = TextureAddressMode.Clamp, AddressModeV = TextureAddressMode.Clamp });

    return game.CreateMaterial(descriptor);
}

/// <summary>A dark surface with a rim of light where it turns away from the eye: a shader class as the emissive colour.</summary>
Material RimMaterial()
{
    var descriptor = MaterialDescriptors.Pbr(new Color(18, 24, 40), metalness: 0.2f, glossiness: 0.7f);

    descriptor.Attributes.Emissive = new MaterialEmissiveMapFeature(new ComputeShaderClassColor { MixinReference = "TextRim" }) { Intensity = new ComputeFloat(1f), UseAlpha = false };

    var material = game.CreateMaterial(descriptor);

    // The shader's cbuffer became keys; set once on the pass
    material.Passes[0].Parameters.Set(TextRimKeys.RimColor, new Vector3(0.3f, 1.6f, 2.4f));
    material.Passes[0].Parameters.Set(TextRimKeys.RimPower, 2.5f);

    return material;
}

/// <summary>A plain surface whose vertices move: the feature in the displacement slot does it.</summary>
Material WaveMaterial()
{
    var descriptor = MaterialDescriptors.Pbr(new Color(120, 200, 150), metalness: 0f, glossiness: 0.5f);

    descriptor.Attributes.Displacement = new WaveFeature();

    return game.CreateMaterial(descriptor);
}

/// <summary>A surface that dissolves with a glowing edge: the feature in the transparency slot, and an emissive slot for the edge to write into.</summary>
Material DissolveMaterial()
{
    var descriptor = MaterialDescriptors.Pbr(new Color(200, 205, 215), metalness: 0f, glossiness: 0.4f);

    descriptor.Attributes.Emissive = new MaterialEmissiveMapFeature(new ComputeColor(Color.Black)) { Intensity = new ComputeFloat(1f) };
    descriptor.Attributes.Transparency = new DissolveFeature();

    return game.CreateMaterial(descriptor);
}

/*
---example-metadata
slug: text-shaders
title:
  en: 3D Text Shaders
  cs: Shadery na 3D textu
level: Advanced
category: Text
complexity: 5
order: 52
description:
  en: |-
    Four materials on the same solid lettering, which is geometry like any other with normals and
    texture coordinates: a gradient texture read through the caps' coordinates, a rim light from a
    shader class of twenty lines as an emissive node, a wave that moves the vertices along the word on
    the engine's clock through a feature in the vertex stage, and a dissolve with a glowing edge
    through a feature in the pixel stage, its amount driven every frame. Each shader is in the
    example's Effects folder and compiled at build like any other.
  cs: |-
    Čtyři materiály na stejném plném písmu, které je geometrií jako každá jiná, s normálami a
    texturovými souřadnicemi: přechodová textura čtená přes souřadnice ploch, lem světla ze
    dvacetiřádkové třídy shaderu jako emisivní uzel, vlna, která hýbe vrcholy podél slova na hodinách
    enginu přes feature ve vertex stage, a rozpouštění se žhnoucí hranou přes feature v pixel stage,
    jehož míra se řídí každý snímek. Každý shader je ve složce Effects příkladu a kompiluje se při
    buildu jako kterýkoli jiný.
concepts:
  - A text mesh's texture coordinates - caps across the block, walls along the outline and through the depth
  - A texture node in the diffuse slot - ComputeTextureColor with clamped addressing
  - A shader class as a colour node - ComputeShaderClassColor with MixinReference, keys set on the pass
  - A material feature in the vertex stage - IMaterialDisplacementFeature and SetStreamFinalModifier, Global.Time
  - A material feature in the pixel stage - IMaterialTransparencyFeature with a discard, driven per frame
  - "Using helpers: SceneSetup3D, AddAllDirectionLighting, TextMesh, TextBevel, TextOverlay"
tags:
  - 3D
  - Text
  - Shaders
  - Materials
  - Font
related:
  - E03_3D_MeshText
  - E02_3D_Material_Gallery
  - E09_3D_MeshOutline
screenshotFrame: 90
enabled: true
created: 2026-10-16
---
*/