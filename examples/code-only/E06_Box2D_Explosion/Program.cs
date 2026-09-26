using Stride.CommunityToolkit.Box2D;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Rendering.Text;
using Stride.CommunityToolkit.Scripts.Utilities;
using Stride.CommunityToolkit.Shapes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Input;

// A grenade in one call: Box2D's explosion gives every shape within a radius an impulse away from
// the centre, and the impulse is per metre of the shape's perimeter facing the blast - so a broad
// flat body catches more of it than a small round one of the same mass, the way a pressure wave
// would. Space detonates at the cursor; the rings show the full-strength radius and the falloff
// band beyond it. Two bodies of the same mass stand side by side on the right, one a wide slab and
// one a ball, to make the perimeter rule visible. The grabber on the camera rebuilds the pyramid
// by hand between blasts, or R does it for you.

const float Radius = 5f;
const float Falloff = 3f;
const float RingSeconds = 0.6f;

var impulse = 12f;

Box2DSimulation? simulation = null;
ShapeBatch? shapeBatch = null;
CameraComponent? camera = null;

var pink = new Color(0xFF, 0xC0, 0xCB);
var paleGreen = new Color(0x98, 0xFB, 0x98);
var royalBlue = new Color(0x41, 0x69, 0xE1);
var gold = new Color(0xFF, 0xD7, 0x00);
var salmon = new Color(0xFA, 0x80, 0x72);
var background = new Color(0.2f, 0.2f, 0.2f);

List<Entity> spawned = [];
Vector2? lastBlast = null;
var ringAge = float.MaxValue;
var blasts = 0;
var sinceStart = 0f;

using var game = new Game();

game.Run(start: Start, update: Update);

simulation?.Dispose();

void Start(Scene rootScene)
{
    game.Window.AllowUserResizing = true;
    game.Window.Title = "Box2D Explosion - Stride Community Toolkit";

    game.SetupBase2D(clearColor: background);
    var cameraEntity = game.Add2DCameraController();
    game.AddProfiler();

    camera = rootScene.GetCamera() ?? throw new InvalidOperationException("Camera not found in scene");
    camera.Entity.Transform.Position = new Vector3(0, 7, 50);
    camera.OrthographicSize = 26;

    shapeBatch = game.AddShapeBatch();
    shapeBatch.BorderWidth = 1f;
    shapeBatch.Fill.Alpha = 0.4f;

    simulation = new Box2DSimulation();

    // Left mouse picks bodies up: rebuild the pyramid by hand, or throw something into the blast.
    cameraEntity.Add(new Grabber2DScript { Simulation = simulation });

    BuildScene(rootScene);
    AddInstructions();
}

void BuildScene(Scene scene)
{
    Ground(scene);

    // A pyramid of boxes to knock down.
    const int rows = 7;
    const float box = 1f;

    for (var row = 0; row < rows; row++)
    {
        var count = rows - row;
        var y = box / 2 + row * box;

        for (var i = 0; i < count; i++)
        {
            var x = -8 + (i - (count - 1) / 2f) * box * 1.05f;
            DynamicBox(scene, new Vector2(x, y), new Vector2(box, box), pink);
        }
    }

    // Balls to scatter.
    for (var i = 0; i < 5; i++)
        DynamicCircle(scene, new Vector2(2 + i * 1.6f, 0.5f), 0.45f, royalBlue);

    // The perimeter rule: a wide slab and a ball with the same mass, at the same distance from a
    // blast centred between them. The slab shows the wave more of its edge, so it flies harder.
    DynamicBox(scene, new Vector2(13, 2), new Vector2(4, 0.5f), gold, density: 1f);
    DynamicCircle(scene, new Vector2(19, 0.8f), 0.8f, gold, density: 4f / (MathF.PI * 0.8f * 0.8f));
}

void Update(Scene rootScene, GameTime time)
{
    simulation?.Update(time.Elapsed);

    var dt = (float)time.Elapsed.TotalSeconds;

    // One blast on its own a second in, at the pyramid, so the scene shows what it is about before a key is pressed.
    sinceStart += dt;

    if (blasts == 0 && sinceStart > 1f)
        Detonate(new Vector2(-8, 1));

    if (game.Input.IsKeyPressed(Keys.Space) && camera?.CalculateRayPlaneIntersectionPoint(game.Input.MousePosition) is { } point)
    {
        Detonate(point);
    }

    var change = (game.Input.IsKeyDown(Keys.K) ? 1 : 0) - (game.Input.IsKeyDown(Keys.J) ? 1 : 0);

    if (change != 0)
        impulse = Math.Clamp(impulse + change * 10 * dt, 1, 60);

    if (game.Input.IsKeyPressed(Keys.R))
        Reset(rootScene);

    ringAge += dt;
    DrawBlast();
}

void Detonate(Vector2 point)
{
    simulation!.Explode(point, Radius, impulse, Falloff);
    lastBlast = point;
    ringAge = 0;
    blasts++;
}

// The blast is a moment; the rings are how it is seen: the full-strength radius inside, the
// falloff band outside, fading over a fraction of a second.
void DrawBlast()
{
    if (shapeBatch is null || lastBlast is not { } centre || ringAge > RingSeconds) return;

    var alpha = 1 - ringAge / RingSeconds;
    var ring = salmon;
    ring.A = (byte)(255 * alpha);
    var band = gold;
    band.A = (byte)(120 * alpha);

    shapeBatch.DrawAnnulus(centre, Radius + Falloff, Radius, band);
    shapeBatch.DrawAnnulus(centre, Radius, Radius - 0.15f, ring);
}

void Reset(Scene scene)
{
    foreach (var entity in spawned)
    {
        simulation!.RemoveBody(entity);
        entity.Scene = null;
    }

    spawned.Clear();
    lastBlast = null;
    BuildScene(scene);
}

void Ground(Scene scene)
{
    var entity = new Entity("Ground") { new ShapeComponent { Vertices = [.. Rectangle(26, 0.5f)], Color = paleGreen } };
    entity.Transform.Position = new Vector3(0, -0.5f, 0);
    entity.Scene = scene;
    spawned.Add(entity);

    var body = simulation!.CreateStaticBody(entity, new Vector3(0, -0.5f, 0));
    ShapeFixtureBuilder.AttachShape(Primitive2DModelType.Rectangle, new Vector2(52, 1), body);
}

void DynamicBox(Scene scene, Vector2 centre, Vector2 size, Color color, float density = 1f)
{
    var entity = new Entity("Box") { new ShapeComponent { Vertices = [.. Rectangle(size.X / 2, size.Y / 2)], Color = color } };
    entity.Transform.Position = new Vector3(centre, 0);
    entity.Scene = scene;
    spawned.Add(entity);

    var body = simulation!.CreateDynamicBody(entity, new Vector3(centre, 0));
    ShapeFixtureBuilder.AttachShape(Primitive2DModelType.Rectangle, size, body, ShapeFixtureBuilder.CreateCustomShapeDef(density, 0.6f, 0.1f));
    entity.Add(new Box2DBodyComponent { BodyId = body });
}

void DynamicCircle(Scene scene, Vector2 centre, float radius, Color color, float density = 1f)
{
    var entity = new Entity("Circle") { new ShapeComponent { Vertices = [Vector2.Zero], Radius = radius, Color = color } };
    entity.Transform.Position = new Vector3(centre, 0);
    entity.Scene = scene;
    spawned.Add(entity);

    var body = simulation!.CreateDynamicBody(entity, new Vector3(centre, 0));
    ShapeFixtureBuilder.AttachShape(Primitive2DModelType.Circle, new Vector2(radius, radius), body, ShapeFixtureBuilder.CreateCustomShapeDef(density, 0.6f, 0.1f));
    entity.Add(new Box2DBodyComponent { BodyId = body });
}

void AddInstructions()
{
    var overlay = DebugOverlay.GetOrCreate(game);

    overlay.Position = DisplayPosition.BottomLeft;

    overlay.AddSection("Explosion", () =>
    [
        new("Space  detonate at the cursor", Color.Yellow),
        new($"J / K  impulse per metre of perimeter  {impulse,5:0.0}    radius {Radius} m, falloff {Falloff} m"),
        new("Left mouse  pick a body up and throw it     R  rebuild"),
        new($"blasts {blasts}   gold slab and ball on the right: same mass, same distance - the slab flies harder", Color.Gray),
    ]);
}

static Vector2[] Rectangle(float halfWidth, float halfHeight) =>
[
    new(-halfWidth, -halfHeight),
    new(halfWidth, -halfHeight),
    new(halfWidth, halfHeight),
    new(-halfWidth, halfHeight),
];

/*
---example-metadata
slug: box2d-explosion
title:
  en: Box2D Explosion
level: Beginner
category: Physics
complexity: 3
order: 93
description:
  en: |-
    A grenade in one call: Explode gives every shape within a radius an impulse away from the
    centre, per metre of perimeter facing the blast, so a wide slab flies harder than a ball of the
    same mass. Space detonates at the cursor, rings show the radius and the falloff band, J and K
    set the impulse, and the grabber on the camera rebuilds the pyramid by hand between blasts.
concepts:
  - Radial impulses with Box2DSimulation.Explode - radius, falloff and impulse per length
  - Why the impulse is per metre of perimeter, shown with a slab and a ball of equal mass
  - Drawing a transient effect with ShapeBatch rings that fade
  - Picking bodies up with Grabber2DScript
  - "Using helpers: SetupBase2D, Add2DCameraController, AddShapeBatch, ShapeComponent"
tags:
  - 2D
  - Box2D
  - Physics
  - Explosion
  - Interaction
  - Third Party
related:
  - E06_Box2D_Joints
  - E06_Box2D
  - E06_Box2D_JunkyardInteractive
screenshotFrame: 90
enabled: true
created: 2026-09-06
---
*/