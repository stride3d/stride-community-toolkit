using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Mathematics;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Scripts.Utilities;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Input;

// A scene with seven shapes to choose from would normally cost seven lines of on-screen instructions,
// permanently. DebugTextDropdown collapses them into one line until you press its key.
//
// The dropdown owns no input of its own: Update() reads the keyboard you give it, and GetLines()
// hands its current appearance to a DebugOverlay section. That is what puts it in the same block as
// the camera controller's help, sharing one screen position and one hide key, rather than being a
// second patch of text somewhere else that F3 could end up drawing on top of.
//
// Each entry names its own key and carries its own callback, so what a choice does is entirely up to
// the caller - here, dropping a shape into the scene.
//
// CloseOnSelect = false keeps the list up after a choice, which is what you want for something used
// repeatedly. Leave it at the default to have the list collapse as soon as a choice is made.

var random = new Random(1);
var parallelogramVertices = new Vector2[]
{
    new(-0.5f, -0.25f),
    new(0.5f, -0.25f),
    new(0.75f, 0.25f),
    new(-0.25f, 0.25f),
};

List<ShapeItem> shapes = [
    new(Primitive2DModelType.Circle),
    new(Primitive2DModelType.Capsule),
    new(Primitive2DModelType.Rectangle),
    new(Primitive2DModelType.Square),
    new(Primitive2DModelType.Polygon),
    new(Primitive2DModelType.Triangle),
    new(Primitive2DModelType.Polygon, parallelogramVertices, "Parallelogram")
];

Scene? scene = null;
DebugTextDropdown? spawnMenu = null;

using var game = new Game();

game.Run(start: Start, update: Update);

void Start(Scene rootScene)
{
    scene = rootScene;

    game.SetupBase2DScene();

    // The starting row, one of each shape
    foreach (var (index, shape) in shapes.Index())
    {
        Spawn(shape, new Vector3(0, 10 + index * 1.5f, 0));
    }

    spawnMenu = new DebugTextDropdown
    {
        Title = "Spawn",
        ToggleKey = Keys.C,
        TitleColor = Color.Yellow,

        // Stay open so shapes can be dropped one after another
        CloseOnSelect = false,

        // Entries are generated from the list above, so adding a shape there adds it to the menu.
        // Keys are yours to pick, which is why a menu is not limited to the ten digits
        Items = [.. shapes.Index().Select(pair => new DebugTextDropdownItem(
            Key: (Keys)(Keys.D1 + pair.Index),
            Text: pair.Item.Name ?? pair.Item.Type.ToString(),
            Action: () => Spawn(pair.Item, new Vector3((random.NextSingle() - 0.5f) * 6f, 14, 0))))]
    };

    // No order given, so it lands after the camera controller's help, which registers at -100
    DebugOverlay.GetOrCreate(game).AddSection("Spawn", () => spawnMenu.GetLines());
}

void Spawn(ShapeItem shape, Vector3 position)
{
    var entity = game.Create2DPrimitive(shape.Type, new()
    {
        Material = game.CreateFlatMaterial(random.NextColor()),
        Vertices = shape.Vertices,
    });

    entity.Transform.Position = position;
    entity.Scene = scene;
}

void Update(Scene rootScene, GameTime time)
{
    // Only the input. The overlay draws the menu for us, every frame, from GetLines()
    spawnMenu?.Update(game.Input);
}

/// <summary>One shape the menu can spawn. <paramref name="Name"/> overrides the label, so two
/// entries built from the same primitive type can still be told apart.</summary>
public record ShapeItem(Primitive2DModelType Type, Vector2[]? Vertices = null, string? Name = null);

/*
---example-metadata
title:
  en: 2D Spawn Menu
  cs: 2D nabídka pro přidávání tvarů
level: Intermediate
category: Shapes
complexity: 2
description:
  en: |
    Drive a scene from the keyboard without filling the screen with instructions. DebugTextDropdown
    shows a single collapsed line until its key is pressed, then expands into a list where every entry
    has its own key, label, colour and callback. Press C to open the menu and 1-7 to drop that shape
    into the 2D scene; the menu is configured to stay open so shapes can be added one after another.
    The dropdown reads no input by itself and does not draw itself either: the example feeds it the
    InputManager each frame and registers its lines as a DebugOverlay section, so it shares one screen
    position and one hide key with the camera controller's help instead of being drawn separately.
  cs: |
    Ovládejte scénu z klávesnice, aniž byste zaplnili obrazovku instrukcemi. DebugTextDropdown zobrazuje
    jediný sbalený řádek, dokud nestisknete jeho klávesu; poté se rozbalí do seznamu, kde má každá
    položka vlastní klávesu, popisek, barvu a akci. Klávesou C otevřete nabídku a klávesami 1-7 přidáte
    daný tvar do 2D scény. Nabídka zůstává otevřená, takže lze tvary přidávat jeden po druhém.
concepts:
    - Building a collapsible keyboard menu with DebugTextDropdown
    - Giving each entry its own key, label, colour and callback
    - Keeping a menu open for repeated use with CloseOnSelect
    - Sharing one on-screen block with the camera help through a DebugOverlay section
    - Spawning entities at runtime from keyboard input
    - Creating 2D primitives (Circle, Capsule, Rectangle, Square, Polygon, Triangle)
    - "Using helpers: SetupBase2DScene, Create2DPrimitive, CreateFlatMaterial"
related:
    - Example01_Basic2DScene_Primitives
    - Example01_Basic2DScene_FallingShapes
    - Example08_DebugShapes_Usage
tags:
    - 2D
    - Bepu
    - Shapes
    - Primitives
    - Debug Text
    - Input
    - Keyboard
    - Intermediate
Order: 56
enabled: true
created: 2026-08-17
---
*/
