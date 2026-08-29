using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.Lines;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;

// Playground for the chart helpers that are being grown here before moving into the toolkit.
// The Lines/ folder is already in its final namespace (Stride.CommunityToolkit.Rendering.Lines),
// so extracting it later is a move, not a rewrite.
//
// What this shows: lines with real thickness and glow. Hardware lines are always one pixel wide,
// so every line here is a ribbon mesh built by PolylineMeshBuilder; the glow comes from an emissive
// intensity above 1 hitting the bloom that SetupBase3DScene's compositor already has enabled.

using var game = new Game();

game.Run(start: Start);

void Start(Scene rootScene)
{
    game.SetupBase3DScene();
    game.AddSkybox();

    // The chart is drawn in the XY plane, 3 units up so it stands clear of the ground
    var chart = new Entity("Chart") { Transform = { Position = new Vector3(0, 3, 0) } };
    chart.Scene = rootScene;

    const float extent = 5f;

    // Axes: thin, no glow
    chart.AddChild(game.CreatePolyline(
        [new Vector3(-extent, 0, 0), new Vector3(extent, 0, 0)],
        new PolylineOptions { Width = 0.03f, Color = Color.Red },
        name: "X axis"));

    chart.AddChild(game.CreatePolyline(
        [new Vector3(0, -extent, 0), new Vector3(0, extent, 0)],
        new PolylineOptions { Width = 0.03f, Color = Color.LimeGreen },
        name: "Y axis"));

    // Three curves with different widths and glow strengths
    chart.AddChild(game.CreatePolyline(
        PolylineSampling.Function(x => 2f * MathF.Sin(x), -extent, extent),
        new PolylineOptions { Width = 0.08f, Color = Color.Cyan, EmissiveIntensity = 3f },
        name: "sin"));

    chart.AddChild(game.CreatePolyline(
        PolylineSampling.Function(x => 0.15f * x * x - 3f, -extent, extent),
        new PolylineOptions { Width = 0.06f, Color = Color.Orange, EmissiveIntensity = 2f },
        name: "parabola"));

    chart.AddChild(game.CreatePolyline(
        PolylineSampling.Parametric(t => new Vector3(1.5f * MathF.Cos(t), 1.5f * MathF.Sin(t), 0f), 0f, MathUtil.TwoPi, 96),
        new PolylineOptions { Width = 0.05f, Color = Color.Magenta, EmissiveIntensity = 4f, Closed = true },
        name: "circle"));
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
    A sandbox for chart and plotting helpers: axes and function curves drawn as glowing lines with
    real thickness. Hardware lines are one pixel wide, so each line is a ribbon mesh built by
    PolylineMeshBuilder from sampled points, given an emissive material that the default compositor's
    bloom turns into a glow. The helpers live in their final toolkit namespace and will move into the
    library once their shape settles.
concepts:
  - Building a ribbon mesh from a list of points
  - Sampling y = f(x) and parametric curves into points
  - "Emissive intensity above 1 plus bloom: glowing lines"
  - Double-sided materials with CullMode.None
  - Grouping entities under a parent so a chart moves as one
  - "Using helpers: SetupBase3DScene, AddSkybox"
tags:
  - 3D
  - Geometry
  - Mesh
  - Line
  - Chart
  - Emissive
  - Bloom
*/
