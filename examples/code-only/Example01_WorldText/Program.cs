using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Games;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Rendering.Text;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;

// A gallery of everything WorldTextComponent can do. Each station on the ground demonstrates one
// setting, and the text itself names the setting that produced it.
//
// WorldTextComponent is IN-SCENE text: it is positioned by its entity's transform, shrinks with
// distance like everything else in the world, and - by default - is hidden by geometry in front of
// it. For text that must always be readable at the same size, such as a HUD, see
// Example01_EntityText.

using var game = new Game();

game.Run(start: Start);

void Start(Scene scene)
{
    game.SetupBase3DScene();
    game.AddSkybox();

    // One call, however much text the scene has. Without the renderer, world text simply never
    // appears - there is no error for a missing renderer.
    game.AddWorldTextRenderer();

    // --- Row 1: orientation --------------------------------------------------------------------

    AddText(scene, new Vector3(-4f, 1.2f, -2), text =>
    {
        // The default: turns to face the camera, but swivels about the world Y axis only, so it
        // stays standing like a signpost and never rolls when the camera tilts
        text.Text = "Billboard (default)\nfly around me!";
    });

    AddText(scene, new Vector3(0f, 1.2f, -2), text =>
    {
        // Faces the camera squarely from any angle, including from directly above
        text.Text = "KeepUpright = false";
        text.KeepUpright = false;
    });

    AddText(scene, new Vector3(4f, 1.2f, -2), text =>
    {
        // No billboarding: the text keeps its entity's rotation, so it foreshortens and disappears
        // edge-on like any other surface in the scene
        text.Text = "Billboard = false\n(fixed in place)";
        text.Billboard = false;
    });

    // Lying flat on the ground, like a road marking: no billboard, rotated face-up
    var floor = AddText(scene, new Vector3(0f, 0.02f, 1.5f), text =>
    {
        text.Text = "FLAT ON THE GROUND";
        text.Billboard = false;
        text.Height = 0.6f;
        text.TextColor = Color.Cyan;
    });

    floor.Transform.Rotation = Quaternion.RotationX(MathUtil.DegreesToRadians(-90));

    // --- Row 2: size ---------------------------------------------------------------------------

    AddText(scene, new Vector3(-4f, 0.6f, 4), text =>
    {
        // Height is world units - this text is a quarter metre tall and always will be. FontSize is
        // only sharpness: raise it if text viewed close up looks soft.
        text.Text = "Height = 0.25";
        text.Height = 0.25f;
    });

    AddText(scene, new Vector3(0f, 1.0f, 4), text =>
    {
        text.Text = "Height = 1";
        text.Height = 1f;
        text.TextColor = Color.Orange;
    });

    // --- Row 3: depth --------------------------------------------------------------------------

    // A wall with text behind it, twice: depth-tested text is hidden by the wall until you fly
    // around it, while DepthTest = false text shows through everything
    var wall = game.Create3DPrimitive(PrimitiveModelType.Cube, new Primitive3DEntityOptions
    {
        EntityName = "Wall",
        Size = new Vector3(3f, 2f, 0.2f),
    });

    wall.Transform.Position = new Vector3(6f, 1f, 4f);
    wall.Scene = scene;

    AddText(scene, new Vector3(6f, 1.3f, 6f), text =>
    {
        text.Text = "DepthTest = true\n(hidden by the wall)";
        text.Height = 0.35f;
    });

    AddText(scene, new Vector3(6f, 0.5f, 6f), text =>
    {
        text.Text = "DepthTest = false\n(drawn through the wall)";
        text.Height = 0.35f;
        text.TextColor = Color.Lime;
        text.DepthTest = false;
    });

    // --- Row 4: distance -----------------------------------------------------------------------

    AddText(scene, new Vector3(-4f, 1.0f, 8), text =>
    {
        text.Text = "Fades from 10, gone at 18\n(walk backwards!)";
        text.Height = 0.4f;
        text.FadeStartDistance = 10;
        text.MaxDistance = 18;
    });
}

// Creates one gallery station: an entity carrying a world text, handed over for its own settings.
Entity AddText(Scene scene, Vector3 position, Action<WorldTextComponent> configure)
{
    var component = new WorldTextComponent
    {
        Text = string.Empty,
        Height = 0.35f,
        Alignment = Stride.Graphics.TextAlignment.Center,
    };

    configure(component);

    var entity = new Entity("WorldText") { component };

    entity.Transform.Position = position;
    entity.Scene = scene;

    return entity;
}

/*
---example-metadata
slug: world-text
title:
  en: World Text (In-Scene)
  cs: Text ve světě (ve scéně)
level: Beginner
category: Text
complexity: 1
order: 70
description:
  en: |-
    A gallery of everything WorldTextComponent can do, one setting per station: billboarding that
    stays upright, free billboarding, text fixed in place, text lying flat on the ground, world-unit
    sizing, depth-tested text hidden behind a wall next to text drawn through it, and distance fading.
    World text lives inside the scene - it shrinks with distance and geometry can hide it.
  cs: |-
    Galerie všeho, co WorldTextComponent umí, jedno nastavení na stanoviště: billboard držící se
    vzpřímeně, volný billboard, pevně umístěný text, text ležící na zemi, velikost ve světových
    jednotkách, text skrytý za zdí vedle textu kresleného skrz ni a mizení s vzdáleností.
    Text ve světě žije uvnitř scény - zmenšuje se s vzdáleností a geometrie ho může zakrýt.
concepts:
  - "Registering the text renderer once: AddWorldTextRenderer"
  - "Billboarding: KeepUpright versus facing the camera freely"
  - Text fixed to a surface or lying flat with Billboard = false
  - "Height in world units versus FontSize as sharpness"
  - Depth-tested text hidden by geometry, and DepthTest = false to draw through
  - Distance fading with FadeStartDistance and MaxDistance
tags:
  - 3D
  - Text
  - Text Rendering
  - World Space
  - Billboard
  - Labels
related:
  - Example01_EntityText
  - Example05_SimpleGeometry
enabled: true
created: 2026-08-22
---
*/