using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Charts;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.Lines;
using Stride.CommunityToolkit.Scripts.Utilities;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Input;

// Playground for the chart helpers that are being grown here before moving into the toolkit.
// Lines/ and Charts/ are already in their final namespaces (Stride.CommunityToolkit.Rendering.Lines
// and Stride.CommunityToolkit.Charts), so extracting them later is a move, not a rewrite.
//
// What this shows: a chart with axes, ticks, labels and a grid, and curves drawn as lines with real
// thickness and glow. Hardware lines are always one pixel wide, so every line here is a ribbon mesh
// built by PolylineMeshBuilder; the glow comes from an emissive intensity above 1 hitting the bloom
// that SetupBase3DScene's compositor already has enabled.
//
// Controls: G toggles the grid. The key is listed in the DebugOverlay section so it shares one
// screen block with the camera help (F2 collapses it, F3 moves it, F4 hides it).

using var game = new Game();

Chart? chart = null;

game.Run(start: Start, update: Update);

void Start(Scene rootScene)
{
    game.SetupBase2DScene();
    //game.SetupBase3DScene();
    //game.AddSkybox();

    chart = Chart.Create(game, new ChartOptions
    {
        XMin = -5f,
        XMax = 5f,
        YMin = -4f,
        YMax = 4f,
        TickStep = 1f,
    });

    // The chart is drawn in the XY plane, lifted so most of it stands clear of the ground
    chart.Root.Transform.Position = new Vector3(0, 3f, 0);
    chart.Root.Scene = rootScene;

    // Explicit options: pick the width, colour and glow strength yourself
    chart.Plot(x => 2f * MathF.Sin(x), new PolylineOptions { Width = 0.08f, Color = Color.Cyan, EmissiveIntensity = 3f }, name: "sin");

    // No options: the chart picks the next palette colour with a medium glow
    chart.Plot(x => 0.15f * x * x - 3f, name: "parabola");

    chart.PlotParametric(
        t => new Vector3(1.5f * MathF.Cos(t), 1.5f * MathF.Sin(t), 0f), 0f, MathUtil.TwoPi,
        new PolylineOptions { Width = 0.05f, Color = Color.Magenta, EmissiveIntensity = 4f, Closed = true },
        samples: 96,
        name: "circle");

    // The overlay draws itself and is shared with the camera controller's help; the lambda is read
    // every frame, so the grid state it shows is always current
    DebugOverlay.GetOrCreate(game).AddSection("Chart", () =>
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
    A sandbox for chart and plotting helpers: a chart with axes, tick marks, labels and a toggleable
    grid, and function curves drawn as glowing lines with real thickness. Hardware lines are one pixel
    wide, so each line is a ribbon mesh built by PolylineMeshBuilder from sampled points, given an
    emissive material that the default compositor's bloom turns into a glow. Tick labels are
    WorldTextComponents, so they face the camera. The helpers live in their final toolkit namespaces
    and will move into the library once their shape settles.
concepts:
  - Building a ribbon mesh from a list of points, and one mesh from many segments
  - Sampling y = f(x) and parametric curves into points
  - "Emissive intensity above 1 plus bloom: glowing lines"
  - Tick labels with WorldTextComponent
  - Grouping entities under a parent so a chart moves as one
  - Toggling a ModelComponent with a key listed in a DebugOverlay section
  - "Using helpers: SetupBase3DScene, AddSkybox, AddWorldTextRenderer, DebugOverlay"
tags:
  - 3D
  - Geometry
  - Mesh
  - Line
  - Chart
  - Emissive
  - Bloom
  - World Text
*/
