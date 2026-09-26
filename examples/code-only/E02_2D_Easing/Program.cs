using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Mathematics;
using Stride.CommunityToolkit.Rendering.Text;
using Stride.CommunityToolkit.Scripts.Utilities;
using Stride.CommunityToolkit.Shapes;
using Stride.CommunityToolkit.Windows;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;
using Stride.Input;

// An easing cheat sheet that runs. Every curve in EasingFunction gets a tile: its graph over a unit
// box, a dot travelling along the graph on a shared clock, and a slider under the box that moves
// with the eased value - so the shape and the motion it produces sit side by side. Click a tile, or
// press N and P, to see one curve large on the right with its formula, and a race between the eased
// slider and a linear one, which is where "ease-out feels snappy" and "elastic overshoots" become
// something you can watch rather than read.
//
// Keys: N / P select the next and previous curve, 1 / 2 / 3 dim everything but the ease-in, ease-out
// or in-out family, 0 shows all, Space pauses the clock, Left / Right change how long one run
// takes, R restarts the clock. Click a tile to select it.

WindowsDpiManager.EnablePerMonitorV2();

const int Columns = 6;
const float ViewHeight = 24f;
const float TileSize = 2.1f;
const float PitchX = 3.8f;
const float PitchY = 3f;
const float Hold = 0.5f;

// Six columns on the left; the big panel on the right, under the overlay block. The overlay is
// pixels and the sheet is world units, so the panel asks the overlay how tall its block is each
// frame and takes whatever height is left, which is why the window opens a step larger than usual
var tiles = Enum.GetValues<EasingFunction>()
    .Select((function, index) => new Tile(function, new Vector2(-21f + TileSize / 2f + index % Columns * PitchX, 6.2f - index / Columns * PitchY)))
    .ToArray();

var panelCentre = new Vector2(8f, -7f);
var panelSize = 6f;
const float MaxPanelSize = 9f;
const float TitleBand = 2.8f;   // the title and the race lanes, above the panel
const float NoteBand = 1.4f;    // the two-line note under it

var selected = 0;
var family = Family.All;
var paused = false;
var duration = 2f;
var clock = 0f;
var t = 0f;

ShapeBatch? shapes = null;
EntityTextComponent? title = null;
EntityTextComponent? note = null;
Entity? titleEntity = null;
Entity? noteEntity = null;

using var game = new Game();

// Full HD rather than the 1280 by 720 default: the help block is fixed pixels, so a taller window
// leaves more of the view to the panel
game.SetWindowSize(1920, 1080);

game.Run(start: Start, update: Update);

void Start(Scene rootScene)
{
    game.Window.AllowUserResizing = true;
    game.Window.Title = "Easing - Stride Community Toolkit";

    game.SetupBase2D(new Color(24, 26, 32));
    game.AddEntityTextRenderer();

    var camera = rootScene.GetCamera() ?? throw new InvalidOperationException("The 2D scene has no camera.");

    camera.OrthographicSize = ViewHeight;

    shapes = game.AddShapeBatch();

    // One label under every tile, and two beside the big panel
    foreach (var tile in tiles)
    {
        rootScene.Entities.Add(Label(ShortName(tile.Function), tile.Centre + new Vector2(0f, -TileSize / 2f - 0.4f), 10, Color.LightGray));
    }

    // Placed every frame by PlacePanel, once the overlay has drawn and knows its height
    titleEntity = Label("", Vector2.Zero, 24, Color.White);
    noteEntity = Label("", Vector2.Zero, 13, Color.LightGray);

    title = titleEntity.Get<EntityTextComponent>();
    note = noteEntity.Get<EntityTextComponent>();

    rootScene.Entities.Add(titleEntity);
    rootScene.Entities.Add(noteEntity);

    DebugOverlay.GetOrCreate(game).AddSection("Easing", BuildOverlayLines);
}

void Update(Scene scene, GameTime time)
{
    if (shapes is null) return;

    HandleInput();

    // One clock for every tile: a run of `duration` seconds, held at each end so the arrival reads
    if (!paused) clock += (float)time.Elapsed.TotalSeconds;

    var period = duration + 2f * Hold;
    var phase = clock % period;

    t = Math.Clamp((phase - Hold) / duration, 0f, 1f);

    foreach (var (index, tile) in tiles.Index())
    {
        DrawTile(shapes, tile, index == selected);
    }

    PlacePanel();
    DrawPanel(shapes, tiles[selected]);
}

// The panel sits under the overlay block, as large as the space between it and the bottom of the
// view allows, with room for the title and race lanes above it and the note below
void PlacePanel()
{
    var overlay = DebugOverlay.GetOrCreate(game);
    var pixelsPerUnit = game.GraphicsDevice.Presenter.BackBuffer.Height / ViewHeight;
    var overlayBottom = ViewHeight / 2f - overlay.BlockBounds.Bottom / pixelsPerUnit;

    var top = overlayBottom - TitleBand;

    panelSize = Math.Clamp(top - (-ViewHeight / 2f + NoteBand), 4f, MaxPanelSize);
    panelCentre = new Vector2(panelCentre.X, top - panelSize / 2f);

    if (titleEntity is not null) titleEntity.Transform.Position = new Vector3(panelCentre + new Vector2(0f, panelSize / 2f + TitleBand), 0f);
    if (noteEntity is not null) noteEntity.Transform.Position = new Vector3(panelCentre + new Vector2(0f, -panelSize / 2f - 0.2f), 0f);
}

void DrawTile(ShapeBatch shapes, Tile tile, bool isSelected)
{
    var inFamily = family == Family.All || FamilyOf(tile.Function) == family;
    var colour = ColourOf(tile.Function);
    var left = tile.Centre.X - TileSize / 2f;
    var bottom = tile.Centre.Y - TileSize / 2f;

    shapes.Opacity = inFamily ? 1f : 0.2f;

    // The unit box: time left to right, progress bottom to top. Overshoots leave it, and should
    shapes.BorderWidth = isSelected ? 2f : 1f;
    shapes.Fill.Set(new Color(32, 36, 46), 0.9f);

    if (isSelected) shapes.Glow.Set(10f, new Color(colour.R, colour.G, colour.B, (byte)140));

    // The box carries the tile as its tag, so a click can ask the batch which tile it hit
    shapes.Tag = tile;
    shapes.DrawRectangle(new Vector3(tile.Centre, 0f), Vector3.UnitX, Vector3.UnitY, new Vector2(TileSize), isSelected ? colour : new Color(70, 76, 90), cornerRadius: 0.12f);
    shapes.Tag = null;
    shapes.Glow.Clear();
    shapes.Fill.Set(null, 1f);

    // The graph, and the dot that rides it
    Span<Vector2> points = stackalloc Vector2[49];

    for (var i = 0; i < points.Length; i++)
    {
        var s = i / (float)(points.Length - 1);

        points[i] = new Vector2(left + s * TileSize, bottom + tile.Function.Ease(s) * TileSize);
    }

    shapes.DrawPixelPolyline(points, 2f, colour);
    shapes.DrawPixelDisc(new Vector3(left + t * TileSize, bottom + tile.Function.Ease(t) * TileSize, 0f), 4f, Color.White);

    // The slider under the box: where the eased value puts a thing that moves from the left edge to the right
    shapes.DrawPixelDisc(new Vector3(left + tile.Function.Ease(t) * TileSize, bottom - 0.22f, 0f), 3.5f, colour);

    shapes.Opacity = 1f;
}

void DrawPanel(ShapeBatch shapes, Tile tile)
{
    var colour = ColourOf(tile.Function);
    var left = panelCentre.X - panelSize / 2f;
    var bottom = panelCentre.Y - panelSize / 2f;

    shapes.BorderWidth = 2f;
    shapes.Fill.Set(new Color(32, 36, 46), 0.9f);
    shapes.DrawRectangle(new Vector3(panelCentre, 0f), Vector3.UnitX, Vector3.UnitY, new Vector2(panelSize), new Color(90, 98, 115), cornerRadius: 0.2f);
    shapes.Fill.Set(null, 1f);

    // A dashed diagonal is the linear curve, the reference every other one is judged against
    shapes.Dash.Set(6f, 6f);
    shapes.DrawPixelLine(new Vector3(left, bottom, 0f), new Vector3(left + panelSize, bottom + panelSize, 0f), 1.5f, new Color(90, 98, 115));
    shapes.Dash.Clear();

    Span<Vector2> points = stackalloc Vector2[129];

    for (var i = 0; i < points.Length; i++)
    {
        var s = i / (float)(points.Length - 1);

        points[i] = new Vector2(left + s * panelSize, bottom + tile.Function.Ease(s) * panelSize);
    }

    shapes.Glow.Set(8f, new Color(colour.R, colour.G, colour.B, (byte)110));
    shapes.DrawPixelPolyline(points, 3f, colour);
    shapes.Glow.Clear();

    var eased = tile.Function.Ease(t);

    shapes.DrawPixelDisc(new Vector3(left + t * panelSize, bottom + eased * panelSize, 0f), 7f, Color.White);

    // The race above the panel: the eased mover against a linear one on the same clock
    var laneY = panelCentre.Y + panelSize / 2f + 1.5f;

    shapes.BorderWidth = 1f;
    shapes.DrawPixelLine(new Vector3(left, laneY, 0f), new Vector3(left + panelSize, laneY, 0f), 1f, new Color(70, 76, 90));
    shapes.DrawPixelLine(new Vector3(left, laneY - 0.7f, 0f), new Vector3(left + panelSize, laneY - 0.7f, 0f), 1f, new Color(70, 76, 90));
    shapes.Fill.Set(colour, 1f);
    shapes.DrawDisc(new Vector3(left + eased * panelSize, laneY, 0f), Vector3.UnitZ, 0.28f, colour);
    shapes.Fill.Set(new Color(110, 116, 130), 1f);
    shapes.DrawDisc(new Vector3(left + t * panelSize, laneY - 0.7f, 0f), Vector3.UnitZ, 0.2f, new Color(150, 156, 170));
    shapes.Fill.Set(null, 1f);

    if (title is not null) title.Text = tile.Function.ToString();
    if (note is not null) note.Text = Notes.Of(tile.Function);
}

void HandleInput()
{
    var input = game.Input;

    if (input.IsKeyPressed(Keys.N)) selected = (selected + 1) % tiles.Length;
    if (input.IsKeyPressed(Keys.P)) selected = (selected + tiles.Length - 1) % tiles.Length;
    if (input.IsKeyPressed(Keys.D0) || input.IsKeyPressed(Keys.NumPad0)) family = Family.All;
    if (input.IsKeyPressed(Keys.D1) || input.IsKeyPressed(Keys.NumPad1)) family = Family.In;
    if (input.IsKeyPressed(Keys.D2) || input.IsKeyPressed(Keys.NumPad2)) family = Family.Out;
    if (input.IsKeyPressed(Keys.D3) || input.IsKeyPressed(Keys.NumPad3)) family = Family.InOut;
    if (input.IsKeyPressed(Keys.Space)) paused = !paused;
    if (input.IsKeyPressed(Keys.R)) clock = 0f;
    if (input.IsKeyPressed(Keys.Left)) duration = MathF.Max(0.5f, duration - 0.5f);
    if (input.IsKeyPressed(Keys.Right)) duration = MathF.Min(8f, duration + 0.5f);

    // A click picks the tile under the mouse: the batch knows which box it drew there
    if (input.IsMouseButtonPressed(MouseButton.Left) && shapes is not null && shapes.TryPick(input.MousePosition, out var hit) && hit.Tag is Tile picked)
    {
        selected = Array.IndexOf(tiles, picked);
    }
}

IReadOnlyList<TextElement> BuildOverlayLines() =>
[
    new("N", "Next curve", Color.Gold),
    new("P", "Previous curve", Color.Gold),
    new("Click", "Select a tile", Color.Gold),
    new(["1", "2", "3"], "One family: in, out, in-out", Color.Gold),
    new("0", "Show every curve", Color.Gold),
    new("Space", "Pause the clock", Color.Gold),
    new("R", "Restart the clock", Color.Gold),
    new(["Left", "Right"], "Shorter or longer run", Color.Gold),
    new(""),
    // Lines stay under about 45 characters: the block is as wide as its widest line, and past that
    // it reaches the sixth tile column on a 150% display
    new($"{tiles.Length} curves, {duration:0.0} s per run, t = {t:0.00}" + (paused ? " paused" : ""), Color.LightGreen),
    new(family switch { Family.In => "Showing the ease-in family", Family.Out => "Showing the ease-out family", Family.InOut => "Showing the in-out family", _ => "Showing every family" }, Color.LightGreen),
    new("Dot: the graph at t.", Color.LightGray),
    new("Discs: eased vs grey linear.", Color.LightGray),
];

static Entity Label(string text, Vector2 position, float fontSize, Color colour)
{
    var entity = new Entity($"Label {text}")
    {
        new EntityTextComponent
        {
            Text = text,
            FontSize = fontSize,
            TextColor = colour,
            Anchor = TextAnchor.TopCenter,
            Alignment = TextAlignment.Center,
        },
    };

    entity.Transform.Position = new Vector3(position, 0f);

    return entity;
}

static string ShortName(EasingFunction function)
    => function.ToString().Replace("EaseInOut", " InOut", StringComparison.Ordinal).Replace("EaseIn", " In", StringComparison.Ordinal).Replace("EaseOut", " Out", StringComparison.Ordinal);

static Family FamilyOf(EasingFunction function)
{
    var name = function.ToString();

    if (name.EndsWith("InOut", StringComparison.Ordinal)) return Family.InOut;
    if (name.EndsWith("EaseIn", StringComparison.Ordinal)) return Family.In;
    if (name.EndsWith("EaseOut", StringComparison.Ordinal)) return Family.Out;

    return Family.Blend;
}

static Color ColourOf(EasingFunction function) => FamilyOf(function) switch
{
    Family.In => new Color(255, 170, 60),
    Family.Out => new Color(90, 200, 255),
    Family.InOut => new Color(150, 230, 120),
    _ => new Color(235, 235, 235),
};

/// <summary>One curve and where its tile sits.</summary>
sealed record Tile(EasingFunction Function, Vector2 Centre);

/// <summary>The three shapes a curve can take, plus the symmetric blends that are none of them.</summary>
enum Family
{
    All,
    In,
    Out,
    InOut,
    Blend,
}

/// <summary>What each curve is, in two lines: the formula, then the feel.</summary>
static class Notes
{
    private static readonly Dictionary<EasingFunction, (string Formula, string Feel)> Text = new()
    {
        [EasingFunction.Linear] = ("y = x.", "No easing: constant speed, the reference the others are judged against."),
        [EasingFunction.SmoothStep] = ("y = x²(3 - 2x).", "The Hermite blend shader code calls smoothstep: gentle at both ends."),
        [EasingFunction.SmootherStep] = ("y = x³(x(6x - 15) + 10).", "Perlin's quintic: flat at both ends, no kink even in acceleration."),
        [EasingFunction.QuadraticEaseIn] = ("y = x².", "Starts slowly and accelerates; the lightest ease-in."),
        [EasingFunction.QuadraticEaseOut] = ("y = -x² + 2x.", "Starts fast and decelerates; the lightest ease-out, and the everyday choice for UI."),
        [EasingFunction.QuadraticEaseInOut] = ("", "Two parabolas back to back: slow, fast, slow."),
        [EasingFunction.CubicEaseIn] = ("y = x³.", "A slower start than quadratic; things that gather themselves before moving."),
        [EasingFunction.CubicEaseOut] = ("y = (x - 1)³ + 1.", "A firmer stop than quadratic; panels sliding into place."),
        [EasingFunction.CubicEaseInOut] = ("", "Two cubics back to back: the standard \"smooth\" for camera moves and transitions."),
        [EasingFunction.QuarticEaseIn] = ("y = x⁴.", "Barely moves for the first third, then rushes."),
        [EasingFunction.QuarticEaseOut] = ("y = 1 - (x - 1)⁴.", "Rushes, then a long soft tail into the end."),
        [EasingFunction.QuarticEaseInOut] = ("", "Two quartics: a longer pause at each end than cubic."),
        [EasingFunction.QuinticEaseIn] = ("y = x⁵.", "The slowest polynomial start; almost a delay before the move."),
        [EasingFunction.QuinticEaseOut] = ("y = (x - 1)⁵ + 1.", "Arrives in a flash and creeps the last few percent."),
        [EasingFunction.QuinticEaseInOut] = ("", "Two quintics: a near-stop at both ends and a dash between."),
        [EasingFunction.SineEaseIn] = ("y = sin((x - 1)π/2) + 1.", "A quarter sine wave: the softest ease-in there is."),
        [EasingFunction.SineEaseOut] = ("y = sin(xπ/2).", "A quarter sine wave: the softest ease-out, close to linear."),
        [EasingFunction.SineEaseInOut] = ("y = (1 - cos(xπ))/2.", "Half a sine wave: the gentlest in-out, good for breathing and idling."),
        [EasingFunction.CircularEaseIn] = ("y = 1 - √(1 - x²).", "A quarter circle: almost nothing, then a vertical rush at the end."),
        [EasingFunction.CircularEaseOut] = ("y = √((2 - x)x).", "A quarter circle: a vertical start, then almost nothing."),
        [EasingFunction.CircularEaseInOut] = ("", "Two quarter circles meeting at the middle with a vertical tangent."),
        [EasingFunction.ExponentialEaseIn] = ("y = 2^(10(x - 1)).", "Still, then explosive: a rocket lifting off."),
        [EasingFunction.ExponentialEaseOut] = ("y = 1 - 2^(-10x).", "Explosive, then still: the snappiest ease-out."),
        [EasingFunction.ExponentialEaseInOut] = ("", "Still, a burst through the middle, still."),
        [EasingFunction.ElasticEaseIn] = ("", "A damped sine wave: winds up with growing wobbles below 0, then snaps to the end."),
        [EasingFunction.ElasticEaseOut] = ("", "A damped sine wave: snaps past 1 and wobbles back onto it; a plucked string."),
        [EasingFunction.ElasticEaseInOut] = ("", "Wobbles out of the start and into the end."),
        [EasingFunction.BackEaseIn] = ("y = x³ - x sin(xπ).", "Pulls back below 0 first, like a wind-up before a throw."),
        [EasingFunction.BackEaseOut] = ("y = 1 - ((1 - x)³ - (1 - x) sin((1 - x)π)).", "Overshoots past 1 and settles: a popup landing."),
        [EasingFunction.BackEaseInOut] = ("", "Pulls back at the start and overshoots at the end."),
        [EasingFunction.BounceEaseIn] = ("", "A dropped ball played backwards: bounces off the start, then flies."),
        [EasingFunction.BounceEaseOut] = ("", "A dropped ball: lands and bounces with diminishing height; four parabolas."),
        [EasingFunction.BounceEaseInOut] = ("", "Bounces off the start and lands with bounces."),
    };

    internal static string Of(EasingFunction function) => Text.TryGetValue(function, out var text) ? (text.Formula.Length == 0 ? text.Feel : text.Formula + "\n" + text.Feel) : "";
}

/*
---example-metadata
slug: easing
title:
  en: Easing Cheat Sheet
  cs: Tahák easingových funkcí
level: Beginner
category: Mathematics
complexity: 2
order: 65
description:
  en: |-
    Every easing curve in the toolkit on one screen: a tile per curve with its graph, a dot riding
    the graph on a shared clock, and a slider that moves the way a thing eased by that curve would.
    Click a tile to see the curve large with its formula and a race against a linear mover, and
    filter by ease-in, ease-out and in-out families. The cheat sheet that runs.
  cs: |-
    Všechny easingové křivky toolkitu na jedné obrazovce: dlaždice pro každou křivku s jejím
    grafem, bod jedoucí po grafu na společných hodinách a jezdec, který se pohybuje tak, jak by se
    pohybovala věc touto křivkou zjemněná. Kliknutím na dlaždici se křivka zobrazí velká se svým
    vzorcem a závodem s lineárním pohybem; rodiny ease-in, ease-out a in-out lze filtrovat. Tahák,
    který běží.
concepts:
  - EasingFunction and Easing - every curve by name and by direct call
  - The fluent extensions - function.Ease(t) and function.Interpolate(start, end, t)
  - Why the dispatcher clamps time and the raw curves do not
  - Ease-in, ease-out and in-out families, and what overshoot looks like in motion
  - A 2D ShapeBatch scene with pixel polylines, discs and a dashed reference line
  - Screen-space labels with EntityTextComponent, and DebugOverlay for the keys
tags:
  - 2D
  - Mathematics
  - Easing
  - Animation
  - ShapeBatch
related:
  - E02_2D_EasingBasics
  - E11_3D_ShapeBatch
  - E20_3D_CubeCollapse
enabled: true
created: 2026-09-14
---
*/