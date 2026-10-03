using E03_2D_HUD_Basics;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Rendering.Text;
using Stride.CommunityToolkit.Scripts.Utilities;
using Stride.CommunityToolkit.Shapes;
using Stride.CommunityToolkit.Windows;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Input;
using System.Globalization;

// A simple HUD, built in eight steps: a panel, a health bar, an energy dial, a crosshair, labels,
// a warning light and a damage flash, and at the end the same HUD moved out into files of its own.
//
// This is the program of the tutorial "Build a simple HUD with ShapeBatch". Each #region below is
// one step of it, and the page shows that region. The number keys show the HUD as it stood after
// each step, so the page can be followed with the example running.
//
// Steps 1 to 7 are written here, in one file, as local functions. Step 8 draws the same HUD from
// SimpleHud.cs and HudStyle.cs.

WindowsDpiManager.EnablePerMonitorV2();

const int LastStep = 8;

string[] stepNames =
[
    "A panel in a corner",
    "A health bar",
    "An energy dial",
    "A crosshair",
    "Labels",
    "A warning light",
    "A damage flash",
    "The same HUD, from its own files",
];

#region Step1Values
// Sizes are in pixels on a 100% display
const float Margin = 24f;

var panelSize = new Vector2(300f, 96f);
var panelFill = new Color(12, 18, 32);
var panelEdge = new Color(120, 200, 255);
#endregion

#region Step2Values
const float LowHealth = 0.3f;

var barSize = new Vector2(252f, 20f);
var trackEdge = new Color(90, 110, 140);
var healthy = new Color(110, 200, 110);
var hurt = new Color(235, 100, 80);

var health = 0.7f;
#endregion

#region Step3Values
const float DialRadius = 40f;
const float DialWidth = 10f;

var dialTrack = new Color(60, 70, 90);
var energyColour = new Color(240, 150, 60);

var energy = 0.5f;
#endregion

#region Step7Values
// How strong the damage flash is, from 1 when a hit lands down to 0
var flash = 0f;
#endregion

// What the command line can set: the step to start at, and a health and a flash to hold, which
// is how the pictures on the tutorial page were taken
var step = (int)Argument("--step", LastStep);
var holdFlash = Argument("--flash", -1f);

health = Argument("--health", health);

ShapeBatch shapes = null!;                 // created in Start
EntityTextComponent healthLabel = null!;
EntityTextComponent energyLabel = null!;
SimpleHud hud = null!;

using var game = new Game();

game.Run(start: Start, update: Update);

void Start(Scene scene)
{
    game.Window.AllowUserResizing = true;
    game.Window.Title = "HUD basics - Stride Community Toolkit";

    #region Scene
    // Something for the HUD to sit over: the scene of E01_2D_FallingShapes
    game.SetupBase2DScene();
    AddFallingShapes(scene);
    #endregion

    #region Step1Create
    // After the post effects, so that the HUD's colours are drawn as written
    shapes = game.AddShapeBatch(afterPostEffects: true);
    #endregion

    #region Step5Create
    game.AddEntityTextRenderer();

    healthLabel = AddLabel(scene, TextAnchor.BottomLeft);
    energyLabel = AddLabel(scene, TextAnchor.MiddleCenter);
    #endregion

    #region Step8Create
    hud = new SimpleHud(game, scene, shapes, new HudStyle());
    #endregion

    DebugOverlay.GetOrCreate(game).AddSection("HUD basics", () =>
    [
        new("1 to 8", "The HUD after that step", Color.Gold),
        new("J", "Take a hit", Color.Gold),
        new("K", "Heal", Color.Gold),
        new(""),
        new($"Step {step}: {stepNames[step - 1]}", Color.LightGreen),
    ]);
}

void Update(Scene scene, GameTime time)
{
    var seconds = (float)time.Total.TotalSeconds;
    var elapsed = (float)time.Elapsed.TotalSeconds;

    for (var i = 0; i < LastStep; i++)
    {
        if (game.Input.IsKeyPressed(Keys.D1 + i) || game.Input.IsKeyPressed(Keys.NumPad1 + i)) step = i + 1;
    }

    #region Step2Update
    if (game.Input.IsKeyPressed(Keys.K)) health = 1f;
    if (game.Input.IsKeyPressed(Keys.J)) health = MathF.Max(health - 0.15f, 0f);
    #endregion

    #region Step3Update
    // Something that moves by itself, so the dial has a value to follow
    energy = 0.5f + 0.4f * MathF.Sin(seconds * 0.6f);
    #endregion

    #region Step7Update
    // A hit starts the flash, and it fades from full to nothing in half a second
    if (game.Input.IsKeyPressed(Keys.J)) flash = 1f;

    flash = MathF.Max(flash - elapsed * 2f, 0f);
    #endregion

    if (holdFlash >= 0f) flash = holdFlash;

    // Labels are entities, not draw calls: they stay until they are hidden
    healthLabel.Opacity = energyLabel.Opacity = step is >= 5 and < 8 ? 1f : 0f;
    hud.Visible = step == 8;

    if (step == 8)
    {
        #region Step8Update
        hud.Health = health;
        hud.Energy = energy;
        hud.Flash = flash;
        hud.Draw(seconds);
        #endregion

        return;
    }

    #region DrawOrder
    // From here on, positions are window pixels: (0, 0) is the top left corner and Y points down
    shapes.Screen = true;

    // Shapes are drawn in the order they are submitted, so what comes first lies underneath
    if (step >= 7) DrawDamageFlash();
    if (step >= 1) DrawPanel();
    if (step >= 2) DrawHealthBar();
    if (step >= 3) DrawEnergyDial();
    if (step >= 4) DrawCrosshair();
    if (step >= 5) UpdateLabels();
    if (step >= 6) DrawWarningLight(seconds);

    shapes.Screen = false;
    #endregion
}

#region Step1
/// <summary>The middle of the panel: a margin in from the bottom left corner of the window.</summary>
Vector2 PanelCentre()
    => shapes.Corner(ScreenCorner.BottomLeft) + new Vector2(Margin + panelSize.X * 0.5f, -Margin - panelSize.Y * 0.5f);

/// <summary>A rectangle on the screen: flat, upright, given by its middle and its size.</summary>
void Rectangle(Vector2 centre, Vector2 size, Color colour, float cornerRadius = 0f)
    => shapes.DrawRectangle(new Vector3(centre, 0f), Vector3.UnitX, Vector3.UnitY, size, colour, cornerRadius);

void DrawPanel()
{
    // Every shape is an outline and a fill. The outline takes the colour of the draw call and
    // the width set here; the fill is set here too
    shapes.BorderWidth = 2f;
    shapes.Fill.Set(panelFill, 0.8f);

    Rectangle(PanelCentre(), panelSize, panelEdge, cornerRadius: 14f);
}
#endregion

#region Step2
/// <summary>The state for an outline with nothing inside it.</summary>
void Outline(float width = 2f)
{
    shapes.BorderWidth = width;
    shapes.Fill.Set(null, 0f);
}

/// <summary>The state for a shape with no outline, filled with the colour of its draw call.</summary>
void Solid()
{
    shapes.BorderWidth = 0f;
    shapes.Fill.Set(null, 1f);
}

void DrawHealthBar()
{
    // The left end of the bar, in the lower half of the panel
    var left = PanelCentre() + new Vector2(-barSize.X * 0.5f, 16f);

    // The track
    Outline();
    Rectangle(left + new Vector2(barSize.X * 0.5f, 0f), barSize, trackEdge, cornerRadius: 5f);

    // The bar: as wide as the health says, a little inside the track, red when the health is low
    var colour = health > LowHealth ? healthy : hurt;
    var size = new Vector2((barSize.X - 8f) * health, barSize.Y - 8f);

    Solid();
    Rectangle(left + new Vector2(4f + size.X * 0.5f, 0f), size, colour, cornerRadius: 2f);
}
#endregion

#region Step3
/// <summary>The middle of the dial: a margin in from the bottom right corner of the window.</summary>
Vector2 DialCentre()
    => shapes.Corner(ScreenCorner.BottomRight) + new Vector2(-Margin - DialRadius - DialWidth, -Margin - DialRadius - DialWidth);

void DrawEnergyDial()
{
    // An arc is a band with a width, filled like any other shape. Each piece sets the state it
    // needs and does not rely on what the piece before it left behind
    Solid();

    // The whole circle, dim
    shapes.DrawArc(DialCentre(), DialRadius, 0f, MathF.Tau, dialTrack, DialWidth);

    // The part that is full. An angle of zero points right and a positive angle turns clockwise,
    // so this starts at the top and runs round to the right
    shapes.DrawArc(DialCentre(), DialRadius, -MathF.PI * 0.5f, MathF.Tau * energy, energyColour, DialWidth);
}
#endregion

#region Step4
void DrawCrosshair()
{
    var centre = shapes.ScreenSize * 0.5f;

    Outline();
    shapes.DrawRing(new Vector3(centre, 0f), Vector3.UnitZ, 14f, Color.White);

    // Four ticks around it, each from 20 to 32 pixels out
    foreach (var direction in (Vector2[])[new(1f, 0f), new(-1f, 0f), new(0f, 1f), new(0f, -1f)])
    {
        shapes.DrawPixelLine(new Vector3(centre + direction * 20f, 0f), new Vector3(centre + direction * 32f, 0f), 2f, Color.White);
    }

    Solid();
    shapes.DrawPixelDisc(new Vector3(centre, 0f), 2f, Color.White);
}
#endregion

#region Step5
/// <summary>A piece of text on the screen. It is made once and then only moved and rewritten.</summary>
EntityTextComponent AddLabel(Scene scene, TextAnchor anchor)
{
    var label = new EntityTextComponent
    {
        Text = "",
        FontSize = 16,
        Anchor = anchor,
        PositionMode = TextPositionMode.Screen,
        EnableShadow = true,
    };

    scene.Entities.Add(new Entity("HUD label") { label });

    return label;
}

void UpdateLabels()
{
    // The positions are worked out every frame, like the shapes', so they follow a resized window
    healthLabel.Text = string.Create(CultureInfo.InvariantCulture, $"HEALTH {health * 100f:0}%");
    healthLabel.ScreenPosition = PanelCentre() + new Vector2(-barSize.X * 0.5f, -6f);

    energyLabel.Text = string.Create(CultureInfo.InvariantCulture, $"{energy * 100f:0}%");
    energyLabel.ScreenPosition = DialCentre();
}
#endregion

#region Step6
void DrawWarningLight(float seconds)
{
    if (health > LowHealth) return;

    var centre = new Vector2(shapes.ScreenSize.X * 0.5f, Margin + 16f);

    // A soft halo outside the shape, and an opacity that rises and falls twice a second
    Solid();
    shapes.Glow.Set(18f);
    shapes.Glow.Strength = 0.5f;
    shapes.Opacity = 0.4f + 0.6f * MathF.Abs(MathF.Sin(seconds * MathF.Tau));

    shapes.DrawDisc(new Vector3(centre, 0f), Vector3.UnitZ, 10f, hurt);

    // The state stays as it is set. Put back what the other shapes expect
    shapes.Glow.Clear();
    shapes.Opacity = 1f;
}
#endregion

#region Step7
void DrawDamageFlash()
{
    if (flash <= 0f) return;

    // The whole window, in red, mostly transparent
    Solid();
    shapes.Opacity = flash * 0.35f;

    Rectangle(shapes.ScreenSize * 0.5f, shapes.ScreenSize, hurt);

    shapes.Opacity = 1f;
}
#endregion

/// <summary>Thirty shapes of five kinds dropped in a column, as in E01_2D_FallingShapes.</summary>
void AddFallingShapes(Scene scene)
{
    (Primitive2DModelType Type, Color Colour)[] kinds =
    [
        (Primitive2DModelType.Square, new Color(30, 70, 170)),
        (Primitive2DModelType.Rectangle, new Color(240, 150, 60)),
        (Primitive2DModelType.Circle, new Color(235, 100, 80)),
        (Primitive2DModelType.Capsule, new Color(110, 200, 110)),
        (Primitive2DModelType.Triangle, new Color(190, 130, 230)),
    ];

    for (var i = 0; i < 30; i++)
    {
        var (type, colour) = kinds[i % kinds.Length];
        var entity = game.Create2DPrimitive(type, new() { Material = game.CreateFlatMaterial(colour) });

        entity.Transform.Position = new Vector3(0.1f * (i % 3 - 1), 8 + i * 1.5f, 0);
        entity.Scene = scene;
    }
}

/// <summary>The number after <paramref name="name"/> on the command line, or the fallback.</summary>
static float Argument(string name, float fallback)
{
    var arguments = Environment.GetCommandLineArgs();
    var index = Array.IndexOf(arguments, name);

    return index >= 0 && index + 1 < arguments.Length && float.TryParse(arguments[index + 1], CultureInfo.InvariantCulture, out var value)
        ? value
        : fallback;
}

/*
---example-metadata
slug: hud-basics
title:
  en: HUD Basics (Step by Step)
  cs: Základy HUD (krok za krokem)
level: Beginner
category: Shapes
complexity: 2
order: 74
description:
  en: |-
    A simple HUD built in eight steps with ShapeBatch: a panel in a corner, a health bar, an energy
    dial, a crosshair, labels, a warning light and a damage flash, and at the end the same HUD moved
    into files of its own. The number keys show the HUD as it stood after each step. It is the
    program of the tutorial "Build a simple HUD with ShapeBatch", which walks through it a step at
    a time.
  cs: |-
    Jednoduchý HUD postavený v osmi krocích pomocí ShapeBatch: panel v rohu, ukazatel zdraví,
    kruhový ukazatel energie, zaměřovač, popisky, výstražné světlo a záblesk při zásahu, a nakonec
    tentýž HUD přesunutý do vlastních souborů. Číselné klávesy ukazují HUD ve stavu po každém kroku.
    Je to program návodu "Build a simple HUD with ShapeBatch", který jím provází krok za krokem.
concepts:
  - Drawing on the screen in pixels with ShapeBatch.Screen, Corner and ScreenSize
  - Outline and fill - BorderWidth and Fill as the state each draw call takes
  - Immediate mode - a bar and a dial that are redrawn from a value every frame
  - Arcs and angles, rings, pixel lines
  - Screen-space text with EntityTextComponent, since ShapeBatch does not draw text
  - Glow and Opacity, and putting the state back afterwards
  - Draw order - what is submitted first lies underneath
  - Moving a HUD out of Program.cs into a class and a style
tags:
  - 2D
  - HUD
  - ShapeBatch
  - Shapes
  - Tutorial
  - Screen Space
  - Text
related:
  - E03_2D_HUD
  - E11_3D_ShapeBatch_Gallery
  - E01_2D_FallingShapes
screenshotFrame: 150
enabled: true
created: 2026-10-03
---
*/