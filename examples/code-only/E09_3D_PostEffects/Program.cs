using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Rendering.Overlay;
using Stride.CommunityToolkit.Rendering.Text;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Input;
using Stride.Rendering;
using Stride.Rendering.Images;
using Stride.Rendering.Images.Dither;

// Every post effect Stride ships, one key each, on a scene built to show them.
//
// The default compositor has all of them switched off and only tone mapping in its colour
// transforms. Bloom, ambient occlusion, depth of field and the rest are objects that already exist
// on the compositor and just need Enabled = true - which is what ConfigurePostEffects below does for
// a first set, and each key does for one effect at a time. The three colour transforms - vignette,
// film grain, dither - are different: they are not there at all until added to the colour-transform
// group, which fuses them into the tone-mapping pass, so they cost nothing extra once added.
//
// The scene is built for the effects, not the other way round: three over-bright lamps for bloom,
// light streaks and lens flare; a glossy floor for screen-space reflections; a corridor of pillars
// receding into the distance for depth of field and fog; a tight cluster of cubes for ambient
// occlusion to darken the creases of.

const float LampIntensity = 6f;   // emissive above 1 is what bloom and streaks feed on

Dictionary<Keys, (string Name, Func<bool> IsOn, Action Toggle)> effects = [];

using var game = new Game();

var sceneSetup = new SceneSetup3D
{
    Ground = null,
    // On the lit side of the default directional light, looking along +Z; from the other side every
    // face turned to the camera is in shadow and the whole scene reads black
    Camera = CameraSetup.Perspective(position: new Vector3(0, 4.5f, -14), rotation: new Vector3(180, -14, 0)).WithController(),
    Profiler = true
}.UseBepu().UseSkybox();

game.Run(sceneSetup, start: Start, update: Update);

void Start(Scene scene)
{
    game.ConfigurePostEffects(fx =>
    {
        // A first set, switched on the way any game would do it. Every key below flips one flag.
        fx.Bloom.Enabled = true;
        fx.Bloom.Amount = 0.6f;

        fx.AmbientOcclusion.Enabled = true;

        fx.Fog.Enabled = true;
        fx.Fog.Density = 0.06f;
        fx.Fog.Color = new Color3(0.55f, 0.6f, 0.7f);

        // Tuning for effects that start off.
        fx.DepthOfField.AutoFocus = true;
        fx.LightStreak.Amount = 0.35f;
        fx.LensFlare.Amount = 0.4f;

        // The colour transforms have to be added, not enabled; only the vignette starts on.
        var vignette = new Vignetting { Amount = 0.35f, Radius = 0.8f };
        var filmGrain = new FilmGrain { Enabled = false, Amount = 0.6f, GrainSize = 2f, Animate = true };
        var dither = new Dither { Enabled = false };

        fx.ColorTransforms.Transforms.Add(vignette);
        fx.ColorTransforms.Transforms.Add(filmGrain);
        fx.ColorTransforms.Transforms.Add(dither);

        effects[Keys.D1] = ("Bloom", () => fx.Bloom.Enabled, () => fx.Bloom.Enabled ^= true);
        effects[Keys.D2] = ("Ambient occlusion", () => fx.AmbientOcclusion.Enabled, () => fx.AmbientOcclusion.Enabled ^= true);
        effects[Keys.D3] = ("Screen-space reflections", () => fx.LocalReflections.Enabled, () => fx.LocalReflections.Enabled ^= true);
        effects[Keys.D4] = ("Depth of field", () => fx.DepthOfField.Enabled, () => fx.DepthOfField.Enabled ^= true);
        effects[Keys.D5] = ("Light streaks", () => fx.LightStreak.Enabled, () => fx.LightStreak.Enabled ^= true);
        effects[Keys.D6] = ("Lens flare", () => fx.LensFlare.Enabled, () => fx.LensFlare.Enabled ^= true);
        effects[Keys.D7] = ("Fog", () => fx.Fog.Enabled, () => fx.Fog.Enabled ^= true);
        effects[Keys.D8] = ("Outline", () => fx.Outline.Enabled, () => fx.Outline.Enabled ^= true);
        effects[Keys.D9] = ("FXAA", () => fx.Antialiasing.Enabled, () => fx.Antialiasing.Enabled ^= true);
        effects[Keys.N] = ("Vignette", () => vignette.Enabled, () => vignette.Enabled ^= true);
        effects[Keys.G] = ("Film grain", () => filmGrain.Enabled, () => filmGrain.Enabled ^= true);
        effects[Keys.T] = ("Dither", () => dither.Enabled, () => dither.Enabled ^= true);
    });

    AddInstructions();
    BuildScene(scene);
}

void Update(Scene scene, GameTime time)
{
    foreach (var (key, effect) in effects)
    {
        if (game.Input.IsKeyPressed(key)) effect.Toggle();
    }

    if (game.Input.IsKeyPressed(Keys.R))
    {
        foreach (var effect in effects.Values)
        {
            if (effect.IsOn()) effect.Toggle();
        }
    }
}

void AddInstructions()
{
    // The lines are rebuilt whenever the overlay draws, so each shows the live state.
    TextOverlay.GetOrCreate(game).AddSection("Post effects", () =>
    {
        List<TextElement> lines = [];

        foreach (var (key, effect) in effects)
        {
            var on = effect.IsOn();
            lines.Add(new(KeyLabel(key), $"{effect.Name} {(on ? "on" : "off")}", on ? Color.Yellow : null));
        }

        lines.Add(new("R", "All off"));
        lines.Add(new(""));
        lines.Add(new("Each key toggles one effect", Color.LightGray));

        return lines;
    });
}

static string KeyLabel(Keys key) => key switch
{
    >= Keys.D0 and <= Keys.D9 => ((char)('0' + (key - Keys.D0))).ToString(),
    _ => key.ToString()
};

void BuildScene(Scene scene)
{
    // A glossy floor: what screen-space reflections have to work with. Glossy, not mirror-like -
    // at metalness 1 the floor turns metallic and simply shows the dark underside of the skybox.
    game.Add3DGround(new Vector2(40), new()
    {
        Material = game.CreateMaterial(new Color(95, 100, 110), metalness: 0.5f, glossiness: 0.85f),
    });

    // A corridor of pillars receding into the distance: depth of field blurs the far ones, fog swallows them.
    for (var i = 0; i < 12; i++)
    {
        var z = -4 + i * 3f;
        var shade = (byte)(170 - i * 8);

        Place(Primitive3D.Cylinder(0.6f, 2), new Vector3(-4, 1, z), game.CreateMaterial(new Color(shade, shade, (byte)(shade + 20))));
        Place(Primitive3D.Cylinder(0.6f, 2), new Vector3(4, 1, z), game.CreateMaterial(new Color(shade, shade, (byte)(shade + 20))));
    }

    // Three lamps brighter than white: bloom haloes them, light streaks and lens flare stretch them.
    Place(Primitive3D.Sphere(0.6f), new Vector3(2.5f, 1.2f, -5), Emissive(new Color(255, 140, 60)));
    Place(Primitive3D.Sphere(0.7f), new Vector3(0.8f, 0.7f, -2), Emissive(new Color(120, 200, 255)));
    Place(Primitive3D.Sphere(0.5f), new Vector3(-1.5f, 1.4f, 2), Emissive(new Color(255, 255, 200)));

    // A tight cluster of cubes: creases and contact corners that ambient occlusion darkens.
    // Negative X is screen-right for a camera looking along +Z.
    var cluster = new Vector3(-2.2f, 0, -3);
    ReadOnlySpan<Vector3> offsets = [new(-0.55f, 0.5f, 0), new(0.55f, 0.5f, 0), new(0, 0.5f, 0.9f), new(0, 1.5f, 0.35f), new(1.4f, 0.5f, 0.6f)];

    foreach (var offset in offsets)
    {
        Place(Primitive3D.Cube(), cluster + offset, game.CreateMaterial(new Color(200, 200, 205)));
    }

    void Place(Primitive3D shape, Vector3 position, Material material)
    {
        // Static, so the scene holds still: this example is about the picture, not the physics.
        var entity = game.CreatePrimitive(shape, new BepuPhysicsOptions
        {
            Material = material,
            Body = BodyMode.Static,
            Position = position,
        });

        entity.Scene = scene;
    }
}

// Diffuse for the shaded look plus an emissive term far above 1: the HDR overshoot is what the
// bloom, streak and flare passes pick out of the frame. MaterialDescriptors.Emissive is the bag behind it.
Material Emissive(Color color) => game.CreateEmissiveMaterial(color, LampIntensity);

/*
---example-metadata
slug: post-effects
title:
  en: Post Effects
level: Beginner
category: Rendering
complexity: 3
order: 25
description:
  en: |-
    Every post effect Stride ships, one key each: bloom, ambient occlusion, screen-space reflections,
    depth of field, light streaks, lens flare, fog, outline, FXAA, and the vignette, film-grain and
    dither colour transforms. The default compositor has all of them switched off, so the example is
    the answer to "how do I turn bloom on" - a first set is enabled through ConfigurePostEffects, the
    rest toggle at runtime - and to the less obvious rule that colour transforms must be added, not
    enabled. The scene is built for the effects: over-bright lamps, a glossy floor, a receding corridor
    and a cluster of cubes.
concepts:
  - Enabling post effects with ConfigurePostEffects, and toggling them at runtime by flipping Enabled on the same objects
  - Which effects exist on the compositor and start disabled
  - Adding Vignetting, FilmGrain and Dither to the colour-transform group, where they cost nothing extra
  - Building an emissive material above intensity 1 so bloom has something to bloom
  - Showing live effect state as a TextOverlay section
  - "Using helpers: SceneSetup3D, Add3DGround, CreatePrimitive, CreateMaterial"
tags:
  - 3D
  - Rendering
  - Post Effects
  - Bloom
  - Fog
  - Depth of Field
  - Compositor
related:
  - E09_3D_MeshOutline
  - E09_3D_SceneRenderer
screenshotFrame: 90
enabled: true
created: 2026-09-03
---
*/