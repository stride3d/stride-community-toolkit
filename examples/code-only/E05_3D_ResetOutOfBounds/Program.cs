using Stride.BepuPhysics;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.Overlay;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Rendering.Text;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Input;

// A body that rolls off the edge of the world falls for as long as the game runs: nothing stops it and
// nothing brings it back, so the scene is one ball poorer. Here a row of balls drops onto a tilted plank
// with no ground under it, rolls off its low end and falls. The update looks at every ball once a frame,
// and a ball below the red line is put back above the high end of the plank with TeleportAtRest, still
// and awake, so it rolls again. The overlay counts the trips.
//
// Teleport alone would move the ball and nothing else: it keeps the speed it fell with and the spin it
// picked up on the plank, and arrives at the top already moving. TeleportAtRest zeroes both and wakes the
// body from a fresh sleep, so every trip starts the same way.

const float FloorLine = -4f;
const float PlankTilt = 12f;
const float PlankLength = 8f;
const float PlankWidth = 6f;
const float BallRadius = 0.3f;
const int BallCount = 5;

var balls = new BodyComponent[BallCount];
var trips = new int[BallCount];

using var game = new Game();

var sceneSetup = new SceneSetup3D
{
    // No ground: a ball that leaves the plank has nothing to land on, which is the point
    Ground = null,
    Camera = CameraSetup.Perspective(position: new Vector3(0f, 2.5f, 13f), rotation: new Vector3(0f, -12f, 0f)).WithController(),
    WindowTitle = "Reset out of bounds - Stride Community Toolkit",
    Resizable = true,
    DpiAware = true,
}.UseBepu().UseSkybox();

game.Run(sceneSetup, start: Start, update: Update);

void Start(Scene scene)
{
    // The plank: a static box, tilted before it joins the scene so a ball set down on its left end rolls off the right
    var plank = game.CreatePrimitive(Primitive3D.Box(new Vector3(PlankLength, 0.2f, PlankWidth)), new BepuPhysicsOptions
    {
        EntityName = "Plank",
        Body = BodyMode.Static,
        Material = game.CreateMaterial(new Color(150, 110, 70), metalness: 0f, glossiness: 0.3f),
    });

    plank.Transform.Rotation = Quaternion.RotationZ(MathUtil.DegreesToRadians(-PlankTilt));
    plank.Scene = scene;

    // The line: a thin slab with no body, so it marks the threshold and stops nothing
    var line = game.CreatePrimitive(Primitive3D.Box(new Vector3(18f, 0.02f, 8f)), new BepuPhysicsOptions
    {
        EntityName = "Floor line",
        Body = BodyMode.None,
        Material = game.CreateMaterial(new Color(220, 60, 60), metalness: 0f, glossiness: 0.2f),
        Position = new Vector3(0f, FloorLine, 0f),
    });

    line.Scene = scene;

    var ballMaterial = game.CreateMaterial(new Color(80, 170, 255), metalness: 0f, glossiness: 0.6f);

    for (var i = 0; i < BallCount; i++)
    {
        var ball = game.CreatePrimitive(Primitive3D.Sphere(BallRadius), new BepuPhysicsOptions
        {
            EntityName = $"Ball {i + 1}",
            Material = ballMaterial,
            Position = StartPosition(i),
        });

        ball.Scene = scene;
        balls[i] = ball.Get<BodyComponent>();
    }

    TextOverlay.GetOrCreate(game).AddSection("Reset out of bounds", BuildOverlayLines);
}

void Update(Scene scene, GameTime time)
{
    if (game.Input.IsKeyPressed(Keys.R))
    {
        for (var i = 0; i < balls.Length; i++)
        {
            Reset(i);
        }
    }

    // Below the line a ball is falling for good, so it goes back to the start
    for (var i = 0; i < balls.Length; i++)
    {
        if (balls[i].Entity.Transform.Position.Y < FloorLine)
        {
            trips[i]++;
            Reset(i);
        }
    }
}

// The rotation defaults to identity, so a ball that tumbled off the plank comes back the way up it started
void Reset(int index) => balls[index].TeleportAtRest(StartPosition(index));

// Above the high end of the plank, the row centred across its width, each ball a little further along the slope
Vector3 StartPosition(int index) => new(-PlankLength / 2f + 0.8f + index * 0.5f, 2.2f, (index - (BallCount - 1) / 2f) * 0.7f);

IReadOnlyList<TextElement> BuildOverlayLines() =>
[
    new("R", "Put every ball back on the plank now", Color.Gold),
    new(""),
    new($"Trips so far: {string.Join(", ", trips)}", Color.White),
];

/*
---example-metadata
slug: reset-out-of-bounds
title:
  en: Reset Out of Bounds
level: Beginner
category: Physics
complexity: 2
order: 44
description:
  en: |-
    A row of balls drops onto a tilted plank with no ground under it, rolls off its low end and falls. The
    update reads every ball's position once a frame, and a ball below the red line goes back above the high
    end of the plank with TeleportAtRest, still and awake, so it rolls again; the overlay counts the trips.
    Teleport alone would keep the speed and the spin the ball arrived with.
concepts:
  - A threshold under the scene, read from each body's entity position once a frame
  - TeleportAtRest puts a body back still and awake, where Teleport alone keeps its speed and spin
  - A static body tilted through its transform before it joins the scene
  - A model with no body as a marker, with Body = BodyMode.None
  - "Using helpers: SceneSetup3D, UseBepu, UseSkybox, CreatePrimitive, CreateMaterial, TeleportAtRest, TextOverlay"
tags:
  - 3D
  - Physics
  - Bepu
  - Reset
  - Teleport
related:
  - E05_3D_CubeFountain
  - E05_3D_TimeControl
screenshotFrame: 150
enabled: true
created: 2026-10-31
---
*/