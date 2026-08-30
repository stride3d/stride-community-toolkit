using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Charts;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.Compositing;
using Stride.CommunityToolkit.Rendering.Lines;
using Stride.CommunityToolkit.Scripts.Utilities;
using Stride.CommunityToolkit.Skyboxes;
using Stride.CommunityToolkit.Windows;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;
using Stride.Input;

// Playground for the chart helpers that are being grown here before moving into the toolkit.
// Lines/ and Charts/ are already in their final namespaces (Stride.CommunityToolkit.Rendering.Lines
// and Stride.CommunityToolkit.Charts), so extracting them later is a move, not a rewrite.
//
// Two looks, one chart API:
//   flat 2D  - orthographic camera, light background, no lighting or glow, major and minor grid,
//              labels that keep their pixel size while zooming. The compositor is created by hand
//              rather than through SetupBase2DScene so it can enable MSAA (thin lines flicker without
//              it) and a paper-like clear colour, and skip the physics ground a chart does not need.
//   glow 3D  - the default 3D scene with skybox and bloom; emissive intensity above 1 makes lines glow.
//
// Controls: G toggles the grid. The key is listed in the DebugOverlay section so it shares one
// screen block with the camera help (F2 collapses it, F3 moves it, F4 hides it).

// Without this a scaled-up 4K desktop hands the game a scaled, blurred window. A no-op off Windows.
WindowsDpiManager.EnablePerMonitorV2();

// Run with "--3d" for the glowing 3D look; a runtime switch rather than a const so neither branch is
// dead code to the compiler
var use3DScene = args.Any(a => a.Equals("--3d", StringComparison.OrdinalIgnoreCase));

using var game = new Game();

Chart? chart = null;

game.Run(start: Start, update: Update);

void Start(Scene rootScene)
{
    game.Window.AllowUserResizing = true;
    game.Window.Title = "Charts Playground";

    if (use3DScene)
    {
        game.SetupBase3DScene();
        game.AddSkybox();
    }
    else
    {
        // What SetupBase2DScene does, minus the ground, plus MSAA and a light background
        game.Add2DGraphicsCompositor(clearColor: new Color(250, 250, 250), msaa: MultisampleCount.X4).AddUIStage();
        game.Add2DCamera();
        game.Add2DCameraController();
    }

    var options = use3DScene ? ChartOptions.Glow3D() : ChartOptions.Light2D();
    options.XMin = -5f;
    options.XMax = 5f;
    options.YMin = -4f;
    options.YMax = 4f;

    chart = Chart.Create(game, options);

    // In 3D the chart stands in the XY plane, lifted so most of it is above the ground; in 2D the
    // orthographic camera already looks at the origin
    chart.Root.Transform.Position = use3DScene ? new Vector3(0, 3f, 0) : Vector3.Zero;
    chart.Root.Scene = rootScene;

    // Curves added without options take the preset's width, glow and next palette colour
    chart.Plot(x => 2f * MathF.Sin(x), name: "sin");
    chart.Plot(x => 0.15f * x * x - 3f, name: "parabola");
    chart.PlotParametric(
        t => new Vector3(1.5f * MathF.Cos(t), 1.5f * MathF.Sin(t), 0f), 0f, MathUtil.TwoPi,
        new PolylineOptions { Width = options.CurveWidth, Color = options.CurvePalette[2], EmissiveIntensity = options.CurveEmissiveIntensity, Closed = true },
        samples: 96,
        name: "circle");

    // The overlay draws itself and is shared with the camera controller's help; the lambda is read
    // every frame, so the grid state it shows is always current
    var overlay = DebugOverlay.GetOrCreate(game);

    // Debug text is 16 pixels tall at scale 1, which is tiny on a high-DPI display. Scale the whole
    // overlay by the monitor's DPI factor (2 on a 4K screen at 200%); the font is rasterised at the
    // resulting size, so any factor stays sharp
    overlay.Scale = MathF.Max(1f, WindowsDpiManager.GetPrimaryScale() ?? 1f);

    // The default box is Stride's 49% black, tuned for dark scenes; on paper white it needs to be darker
    if (!use3DScene)
    {
        overlay.BackgroundColor = new Color(0, 0, 0, 200);
        overlay.FontSize = 16;
        overlay.LineSpacing = 1;
    }

    overlay.AddSection("Chart", () =>
    [
        new("CHART"),
        new($"Press G to toggle the grid ({(chart.GridVisible ? "on" : "off")})", Color.Yellow),
    ]);
}

void Update(Scene scene, GameTime time)
{
    if (chart != null && game.Input.IsKeyPressed(Keys.G))
    {
        chart.GridVisible = !chart.GridVisible;
    }
}

/*
---example-metadata
slug: charts-playground
title:
  en: Charts Playground
level: Intermediate
category: Geometry
complexity: 3
order: 35
description:
  en: |-
    A sandbox for chart and plotting helpers: a chart with axes, tick marks, labels, a major and minor
    grid, and function curves drawn as lines with real thickness. Two presets share one API - a flat,
    paper-like 2D chart under an orthographic camera with MSAA and pixel-sized labels, and a glowing
    3D chart in a lit scene with bloom. Hardware lines are one pixel wide, so each line is a ribbon
    mesh built by PolylineMeshBuilder from sampled points. The helpers live in their final toolkit
    namespaces and will move into the library once their shape settles.
concepts:
  - Building a ribbon mesh from a list of points, and one mesh from many segments
  - Sampling y = f(x) and parametric curves into points
  - "Emissive intensity above 1 plus bloom: glowing lines"
  - Why thin geometry flickers without MSAA, and enabling it on the 2D compositor
  - Screen-sized tick labels with EntityTextComponent versus world-sized ones with WorldTextComponent
  - Composing a 2D scene by hand instead of SetupBase2DScene
  - Toggling a ModelComponent with a key listed in a DebugOverlay section
  - "Using helpers: Add2DGraphicsCompositor, Add2DCamera, Add2DCameraController, SetupBase3DScene, AddSkybox, DebugOverlay"
tags:
  - 2D
  - 3D
  - Geometry
  - Mesh
  - Line
  - Chart
  - MSAA
  - Emissive
  - Bloom
*/
