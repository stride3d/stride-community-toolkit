using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Mathematics;
using Stride.CommunityToolkit.Rendering.Overlay;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Input;

// A scene with seven shapes to choose from would normally cost seven lines of on-screen instructions,
// permanently. KeyMenu collapses them into one line until you press its key.
//
// The key menu owns no input of its own: Update() reads the keyboard you give it, and GetLines()
// hands its current appearance to a TextOverlay section. That is what puts it in the same block as
// the camera controller's help, sharing one screen position and one hide key, rather than being a
// second patch of text somewhere else that F3 could end up drawing on top of.
//
// Each entry names its own key and carries its own callback, so what a choice does is entirely up to
// the caller - here, dropping a shape into the scene.
//
// CloseOnSelect = false keeps the list up after a choice; the default collapses it.

var random = new Random(1);

Vector2[] parallelogram = [new(-0.5f, -0.25f), new(0.5f, -0.25f), new(0.75f, 0.25f), new(-0.25f, 0.25f)];

ShapeItem[] shapes =
[
    new(Primitive2D.Circle()),
    new(Primitive2D.Capsule()),
    new(Primitive2D.Rectangle()),
    new(Primitive2D.Square()),
    // The pentagon is the big one, so its collider gets a depth to match
    new(Primitive2D.RegularPolygon(1, 5, depth: 4)),
    new(Primitive2D.Triangle()),
    new(Primitive2D.Polygon(parallelogram), "Parallelogram")
];

Scene? scene = null;
KeyMenu? spawnMenu = null;

using var game = new Game();

game.Run(new SceneSetup2D().UseBepu(), start: Start, update: Update);

void Start(Scene rootScene)
{
    // Left mouse grabs any body, drags it and throws it on release
    game.AddGrabber();

    scene = rootScene;

    // The starting column, one of each shape
    foreach (var (index, shape) in shapes.Index())
    {
        Spawn(shape, new Vector3(0, 10 + index * 1.5f, 0));
    }

    spawnMenu = new KeyMenu
    {
        Title = "Spawn",
        ToggleKey = Keys.C,
        TitleColor = Color.Yellow,

        // Stay open so shapes can be dropped one after another
        CloseOnSelect = false,

        // One entry per shape, so adding a shape adds it to the menu. Any key works, not just digits
        Items = [.. shapes.Index().Select(pair => new KeyMenuItem(
            Key: (Keys)(Keys.D1 + pair.Index),
            Text: pair.Item.Name ?? pair.Item.Type.Name,
            Action: () => Spawn(pair.Item, random.NextVector3(new Vector3(-7, 6, 0), new Vector3(7, 20, 0))))
)]
    };
    // No order given, so it lands after the camera controller's help, which registers at -100
    var overlay = TextOverlay.GetOrCreate(game);
    overlay.SectionGap = 0;
    overlay.AddSection("Spawn", () => [new("Left mouse", "Grab a shape, drag it, let go to throw", Color.Gold), .. spawnMenu.GetLines()]);
}

void Spawn(ShapeItem shape, Vector3 position)
{
    var entity = game.CreatePrimitive(shape.Type, new() { Material = game.CreateFlatMaterial(random.NextColor()), Position = position });

    entity.Scene = scene;
}

void Update(Scene rootScene, GameTime time)
{
    // Only the input. The overlay draws the menu for us, every frame, from GetLines()
    spawnMenu?.Update(game.Input);
}

/// <summary>One shape the menu can spawn. <paramref name="Name"/> overrides the label, so two
/// entries built from the same primitive type can still be told apart.</summary>
public record ShapeItem(Primitive2D Type, string? Name = null);

/*
---example-metadata
slug: spawn-menu-2d
title:
  en: 2D Spawn Menu
  cs: 2D nabídka pro přidávání tvarů
level: Intermediate
category: Input
complexity: 2
order: 190
description:
  en: |-
    Drive a scene from the keyboard without filling the screen with instructions. KeyMenu
    shows a single collapsed line until its key is pressed, then expands into a list where every entry
    has its own key, label, colour and callback. Press C to open the menu and 1-7 to drop that shape
    into the 2D scene; the menu is configured to stay open so shapes can be added one after another.
    The key menu reads no input by itself and does not draw itself either: the example feeds it the
    InputManager each frame and registers its lines as a TextOverlay section, so it shares one screen
    position and one hide key with the camera controller's help instead of being drawn separately.
  cs: |-
    Ovládejte scénu z klávesnice, aniž byste zaplnili obrazovku instrukcemi. KeyMenu zobrazuje
    jediný sbalený řádek, dokud nestisknete jeho klávesu; poté se rozbalí do seznamu, kde má každá
    položka vlastní klávesu, popisek, barvu a akci. Klávesou C otevřete nabídku a klávesami 1-7 přidáte
    daný tvar do 2D scény. Nabídka zůstává otevřená, takže lze tvary přidávat jeden po druhém.
concepts:
  - Building a collapsible keyboard menu with KeyMenu
  - Giving each entry its own key, label, colour and callback
  - Keeping a menu open for repeated use with CloseOnSelect
  - Sharing one on-screen block with the camera help through a TextOverlay section
  - Spawning entities at runtime from keyboard input
  - Creating 2D primitives (Circle, Capsule, Rectangle, Square, RegularPolygon, Triangle, Polygon)
  - "Using helpers: SceneSetup2D, CreatePrimitive, CreateFlatMaterial, AddGrabber"
tags:
  - 2D
  - Bepu
  - Shapes
  - Primitives

  - Input
  - Keyboard
related:
  - E02_2D_Primitives
  - E05_2D_FallingShapes
  - E08_3D_DebugShapes_QuickStart
enabled: true
created: 2026-08-17
---
*/