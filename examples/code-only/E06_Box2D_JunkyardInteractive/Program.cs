using Box2D.NET;
using Stride.CommunityToolkit.Box2D;
using Stride.CommunityToolkit.Box2D.Events;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Scripts;
using Stride.CommunityToolkit.Scripts.Utilities;
using Stride.CommunityToolkit.Shapes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Input;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Shapes;

// The playground sibling of E06_Box2D_Junkyard: the same walled yard and sweeping plow, but
// built the Stride way - every shape is an entity carrying ShapeComponent (the testbed
// look, no meshes or materials) and a Box2D body, so the component system, scripts and camera all
// join in. Shapes of different kinds fall and mix freely - pentagons, circles, capsules and boxes,
// switchable at runtime - something the instanced stress piles cannot do, because here rendering is
// one shader batch instead of one master model per shape.
//
// A wheat-coloured sensor gate hangs over the yard: anything passing through turns gold, driven by
// the library's sensor events. Pick a shape up with the mouse and throw it, press N to drop a new one,
// middle-click a shape and the camera follows it through the pile.

// --- the junkyard's yard, verbatim
const float GridSize = 1.0f;

// --- spawning
const int InitialCount = 600;
const int BatchPerRow = 25;
const float SpawnSpacing = 1.5f;
const float SpawnHeight = 35f;

// The testbed paints continuous-collision candidates salmon: bodies sweeping more than roughly half
// their extent in one step. Approximated for the mixed shape sizes here.
const float FastSpeed = 0.5f * 0.4f * 60f;

// --- the testbed palette, plus gold for shapes inside the sensor gate
var paleGreen = new Color(0x98, 0xFB, 0x98);
var pink = new Color(0xFF, 0xC0, 0xCB);
var salmon = new Color(0xFA, 0x80, 0x72);
var gray = new Color(0x80, 0x80, 0x80);
var royalBlue = new Color(0x41, 0x69, 0xE1);
var wheat = new Color(0xF5, 0xDE, 0xB3);
var gold = new Color(0xFF, 0xD7, 0x00);
var background = new Color(0.2f, 0.2f, 0.2f);

Box2DSimulation? simulation = null;
ShapeBatch? shapeBatch = null;
CameraComponent? camera = null;
Basic2DCameraController? cameraController = null;

var random = new Random(1);
List<SpawnedShape> shapes = [];
var sensorWatcher = new SensorWatcher();
List<DebugTextDropdown> menus = [];

// The shape catalogue: outline vertices plus the rounding radius the shader applies. A single
// vertex with a radius is a circle, two vertices with a radius a capsule - and the collider is
// attached from the same numbers, so physics and pixels always agree.
var catalogue = new ShapeDefinition[]
{
    new("Pentagon", FibonacciPentagon(0.4f), 0f),
    new("Circle", [Vector2.Zero], 0.35f),
    new("Capsule", [new(0, -0.25f), new(0, 0.25f)], 0.25f),
    new("Box", RectangleVertices(0.35f, 0.35f), 0f),
};
var currentShape = 0;

using var game = new Game();

game.Run(start: Start, update: Update);

simulation?.Dispose();

void Start(Scene rootScene)
{
    game.Window.AllowUserResizing = true;
    game.Window.Title = "Junkyard Playground Box2D Example - Stride Community Toolkit";

    game.SetupBase2D(clearColor: background);
    var cameraEntity = game.Add2DCameraController();
    cameraController = cameraEntity.Get<Basic2DCameraController>();
    game.AddProfiler();

    camera = rootScene.GetCamera() ?? throw new InvalidOperationException("Camera not found in scene");
    camera.Entity.Transform.Position = new Vector3(8, 25, 50);
    camera.OrthographicSize = 60;

    shapeBatch = game.AddShapeBatch();
    shapeBatch.BorderWidth = 1f;
    shapeBatch.Fill.Alpha = 0.4f;

    simulation = new Box2DSimulation();
    simulation.RegisterSensorEventHandler(sensorWatcher);

    // Pick shapes up and throw them with the left mouse button: a kinematic anchor and a motor joint
    // under the cursor, so the held shape still collides with the pile and the plow.
    cameraEntity.Add(new Grabber2DScript { Simulation = simulation });

    CreateGround(rootScene);
    CreateSensorGate(rootScene);
    CreatePusher(rootScene);
    SetupMenus();
    SpawnBatch(rootScene, InitialCount);
}

/// <summary>
/// The junkyard's yard: one static body carrying 161 slightly overlapping floor squares and 50 wall
/// squares up each side at x = -80 and x = +80 - every square also an entity with a shape component.
/// </summary>
void CreateGround(Scene scene)
{
    var groundId = simulation!.CreateStaticBody(Vector3.Zero);
    var shapeDef = ShapeFixtureBuilder.CreateDefaultShapeDef();

    var floorSquare = RectangleVertices(0.55f * GridSize, 0.5f * GridSize);
    var wallSquare = RectangleVertices(0.5f * GridSize, 0.55f * GridSize);

    var y = 0.0f;
    var x = -80.0f * GridSize;

    for (var i = 0; i < 161; ++i)
    {
        var box = b2MakeOffsetBox(0.55f * GridSize, 0.5f * GridSize, new B2Vec2(x, y), b2Rot_identity);
        b2CreatePolygonShape(groundId, in shapeDef, in box);
        AddStaticSquare(scene, floorSquare, x, y);
        x += GridSize;
    }

    y = GridSize;

    for (var i = 0; i < 50; ++i)
    {
        var leftBox = b2MakeOffsetBox(0.5f * GridSize, 0.55f * GridSize, new B2Vec2(-80.0f * GridSize, y), b2Rot_identity);
        b2CreatePolygonShape(groundId, in shapeDef, in leftBox);
        AddStaticSquare(scene, wallSquare, -80.0f * GridSize, y);

        var rightBox = b2MakeOffsetBox(0.5f * GridSize, 0.55f * GridSize, new B2Vec2(80.0f * GridSize, y), b2Rot_identity);
        b2CreatePolygonShape(groundId, in shapeDef, in rightBox);
        AddStaticSquare(scene, wallSquare, 80.0f * GridSize, y);

        y += GridSize;
    }
}

void AddStaticSquare(Scene scene, Vector2[] vertices, float x, float y)
{
    var entity = new Entity("StaticSquare")
    {
        new ShapeComponent { Vertices = [.. vertices], Color = paleGreen }
    };
    entity.Transform.Position = new Vector3(x, y, 0);
    entity.Scene = scene;
}

/// <summary>
/// A sensor gate hanging over the yard: a static sensor fixture that reports overlaps through the
/// library's sensor events, so anything inside it turns gold. Sensors detect but never collide.
/// </summary>
void CreateSensorGate(Scene scene)
{
    var gateVertices = RectangleVertices(8f, 3f);

    var entity = new Entity("SensorGate")
    {
        new ShapeComponent { Vertices = [.. gateVertices], Color = wheat }
    };
    entity.Transform.Position = new Vector3(0, 22, 0);
    entity.Scene = scene;

    var bodyId = simulation!.CreateStaticBody(entity, new Vector3(0, 22, 0));

    var sensorDef = ShapeFixtureBuilder.CreateCustomShapeDef(1f, 0.6f, 0f, isSensor: true);
    sensorDef.enableSensorEvents = true;

    ShapeFixtureBuilder.AttachPolygon(gateVertices, bodyId, sensorDef);
}

/// <summary>
/// The junkyard's kinematic plow, sweeping x = 60*sin(0.2t) - here it is also just an entity with a
/// shape component, carried by the body-to-entity transform sync.
/// </summary>
void CreatePusher(Scene scene)
{
    var entity = new Entity("Pusher")
    {
        new ShapeComponent { Vertices = [new(-2, 0), new(2, 0), new(2, 8), new(-2, 8)], Color = royalBlue }
    };
    entity.Scene = scene;

    var pusherId = simulation!.CreateKinematicBody(entity, Vector3.Zero);

    var shapeDef = ShapeFixtureBuilder.CreateDefaultShapeDef();
    var pusherBox = b2MakeOffsetBox(2.0f, 4.0f, new B2Vec2(0.0f, 4.0f), b2Rot_identity);
    b2CreatePolygonShape(pusherId, in shapeDef, in pusherBox);

    simulation.RegisterSimulationUpdate(new PusherDriver(pusherId));
}

void SetupMenus()
{
    menus =
    [
        new DebugTextDropdown
        {
            Title = "Shape",
            ToggleKey = Keys.C,
            TitleColor = Color.Yellow,
            SelectedIndex = currentShape,
            Items = [.. catalogue.Select((definition, index) => new DebugTextDropdownItem(
                (Keys)(Keys.D1 + index), definition.Name, () => currentShape = index))]
        },
    ];

    DebugOverlay.GetOrCreate(game).AddSection("Playground", BuildOverlayLines);
}

void SpawnBatch(Scene scene, int count)
{
    var rows = Math.Max(1, count / BatchPerRow);

    for (var i = 0; i < rows; i++)
    {
        for (var j = 0; j < BatchPerRow; j++)
        {
            var position = new Vector2(
                (j - BatchPerRow / 2f) * SpawnSpacing + Jitter(),
                SpawnHeight + i * SpawnSpacing + Jitter());

            Spawn(scene, catalogue[currentShape], position);
        }
    }

    float Jitter() => (random.NextSingle() - 0.5f) * 0.1f;
}

/// <summary>
/// One shape = one entity: the component draws it, the body moves it, and the same numbers build
/// both the visual outline and the collider.
/// </summary>
void Spawn(Scene scene, ShapeDefinition definition, Vector2 position)
{
    var component = new ShapeComponent { Vertices = [.. definition.Vertices], Radius = definition.Radius, Color = pink };
    var entity = new Entity("Shape") { component };

    var bodyId = simulation!.CreateDynamicBody(entity, new Vector3(position.X, position.Y, 0));

    var shapeDef = ShapeFixtureBuilder.CreateDefaultShapeDef();
    shapeDef.enableSensorEvents = true;

    AttachCollider(definition, bodyId, shapeDef);

    entity.Add(new Box2DBodyComponent { BodyId = bodyId });
    entity.Transform.Position = new Vector3(position.X, position.Y, 0);
    entity.Scene = scene;

    shapes.Add(new SpawnedShape(entity, bodyId, component));
}

void AttachCollider(ShapeDefinition definition, B2BodyId bodyId, B2ShapeDef shapeDef)
{
    if (definition.Vertices.Length == 1)
    {
        // A circle: the rounding radius is the whole shape
        ShapeFixtureBuilder.AttachShape(Primitive2DModelType.Circle, new Vector2(definition.Radius, definition.Radius), bodyId, shapeDef);
    }
    else if (definition.Vertices.Length == 2)
    {
        // A capsule: two segment endpoints plus the radius; total height = segment + both caps
        var halfSegment = MathF.Abs(definition.Vertices[1].Y - definition.Vertices[0].Y) * 0.5f;
        ShapeFixtureBuilder.AttachShape(Primitive2DModelType.Capsule, new Vector2(definition.Radius, 2f * (halfSegment + definition.Radius)), bodyId, shapeDef);
    }
    else
    {
        ShapeFixtureBuilder.AttachPolygon(definition.Vertices, bodyId, shapeDef);
    }
}

void Update(Scene rootScene, GameTime time)
{
    simulation?.Update(time.Elapsed);

    UpdateShapeColors();
    HandleInput(rootScene);
}

/// <summary>
/// The testbed's body colouring, driven straight into each entity's component: gold inside the
/// sensor gate, gray asleep, salmon fast, pink otherwise.
/// </summary>
void UpdateShapeColors()
{
    foreach (var shape in shapes)
    {
        Color color;

        if (sensorWatcher.Contains(shape.Entity))
        {
            color = gold;
        }
        else if (!b2Body_IsAwake(shape.BodyId))
        {
            color = gray;
        }
        else
        {
            var velocity = b2Body_GetLinearVelocity(shape.BodyId);
            var fast = velocity.X * velocity.X + velocity.Y * velocity.Y > FastSpeed * FastSpeed;

            color = fast ? salmon : pink;
        }

        shape.Component.Color = color;
    }
}

void HandleInput(Scene rootScene)
{
    if (!game.Input.HasKeyboard) return;

    foreach (var menu in menus)
    {
        if (menu.Update(game.Input)) return;
    }

    if (game.Input.IsKeyPressed(Keys.Space)) SpawnBatch(rootScene, InitialCount / 2);
    if (game.Input.IsKeyPressed(Keys.X)) ClearShapes();
    if (game.Input.IsKeyPressed(Keys.Escape) && cameraController != null) cameraController.FollowTarget = null;

    if (!game.Input.HasMouse || camera is null) return;

    // The left button belongs to the grabber on the camera: pick a shape up, carry it, throw it.
    // N drops a new one of the selected kind at the cursor.
    if (game.Input.IsKeyPressed(Keys.N) && camera.CalculateRayPlaneIntersectionPoint(game.Input.MousePosition) is { } dropPoint)
    {
        Spawn(rootScene, catalogue[currentShape], dropPoint);
    }

    if (game.Input.IsMouseButtonPressed(MouseButton.Middle))
    {
        var world = camera.CalculateRayPlaneIntersectionPoint(game.Input.MousePosition);

        if (world is { } point && simulation!.OverlapPoint(point) is { } bodyId && cameraController != null)
        {
            // The camera controller does the rest: it tracks the entity until Escape releases it
            cameraController.FollowTarget = simulation.GetEntity(bodyId);
        }
    }
}

void ClearShapes()
{
    foreach (var shape in shapes)
    {
        simulation?.RemoveBody(shape.Entity);
        shape.Entity.Scene = null;
    }

    shapes.Clear();
    sensorWatcher.Clear();
}

IReadOnlyList<TextElement> BuildOverlayLines()
{
    var asleep = 0;

    foreach (var shape in shapes)
    {
        if (!b2Body_IsAwake(shape.BodyId)) asleep++;
    }

    List<TextElement> lines =
    [
        new($"{shapes.Count:N0} shapes as entities, one shader batch", Color.LightGreen),
        new($"{shapes.Count - asleep:N0} awake / {asleep:N0} asleep / {sensorWatcher.Count} in the gate", Color.MediumSeaGreen),
        new(string.Empty),
    ];

    foreach (var menu in menus)
    {
        lines.AddRange(menu.GetLines());
    }

    lines.Add(new(string.Empty));
    lines.Add(new("Left mouse - pick a shape up, carry it, throw it     N - drop a new one at the cursor", Color.Yellow));
    lines.Add(new("Middle Click - follow a shape     ESC - stop following", Color.Yellow));
    lines.Add(new($"SPACE - spawn {InitialCount / 2} more     X - clear", Color.Yellow));

    return lines;
}

static Vector2[] RectangleVertices(float halfWidth, float halfHeight) =>
[
    new(-halfWidth, -halfHeight),
    new(halfWidth, -halfHeight),
    new(halfWidth, halfHeight),
    new(-halfWidth, halfHeight),
];

/// <summary>
/// The junkyard's rock outline: five points on a circle by the Fibonacci sphere algorithm, sorted
/// by angle so the convex hull and the shader's SDF see them in CCW order.
/// </summary>
static Vector2[] FibonacciPentagon(float radius)
{
    var phi = MathF.PI * (MathF.Sqrt(5.0f) - 1.0f);
    var points = new Vector2[5];

    for (var i = 0; i < 5; ++i)
    {
        var theta = phi * i;
        points[i] = new Vector2(radius * MathF.Cos(theta), radius * MathF.Sin(theta));
    }

    return [.. points.OrderBy(p => MathF.Atan2(p.Y, p.X))];
}

/// <summary>One entry of the shape catalogue: a name for the menu, the outline, and the rounding radius.</summary>
public sealed record ShapeDefinition(string Name, Vector2[] Vertices, float Radius);

/// <summary>One spawned shape: the entity, its body, and the component whose colour tracks the body state.</summary>
public sealed record SpawnedShape(Entity Entity, B2BodyId BodyId, ShapeComponent Component);

/// <summary>
/// Tracks which entities are inside the sensor gate through the library's sensor events. Begin adds
/// the visitor, End removes it; overlapping multiple sensor shapes is counted, not toggled.
/// </summary>
public sealed class SensorWatcher : ISensorEventHandler
{
    private readonly Dictionary<Entity, int> _inside = [];

    public int Count => _inside.Count;

    public bool Contains(Entity entity) => _inside.ContainsKey(entity);

    public void Clear() => _inside.Clear();

    public void OnSensorEvent(SensorEventData eventData)
    {
        if (eventData.Type == SensorEventType.BeginTouch)
        {
            _inside[eventData.VisitorEntity] = _inside.GetValueOrDefault(eventData.VisitorEntity) + 1;
        }
        else if (_inside.TryGetValue(eventData.VisitorEntity, out var depth))
        {
            if (depth <= 1) _inside.Remove(eventData.VisitorEntity);
            else _inside[eventData.VisitorEntity] = depth - 1;
        }
    }
}

/// <summary>
/// Drives the kinematic pusher along the junkyard's sweep, x = 60*sin(0.2t), by setting its target
/// transform once per fixed step - Box2D derives the kinematic velocity from target and time step.
/// </summary>
public sealed class PusherDriver : IBox2DSimulationUpdate
{
    private readonly B2BodyId _pusherId;
    private int _stepCount;

    public PusherDriver(B2BodyId pusherId) => _pusherId = pusherId;

    public void SimulationUpdate(Box2DSimulation simulation, float deltaTime)
    {
        var time = deltaTime * _stepCount;
        _stepCount++;

        var cosSin = b2ComputeCosSin(0.2f * time);
        var target = new B2Transform(new B2Vec2(60.0f * cosSin.sine, 0.0f), b2Rot_identity);

        b2Body_SetTargetTransform(_pusherId, in target, deltaTime, true);
    }

    public void AfterSimulationUpdate(Box2DSimulation simulation, float deltaTime)
    {
    }
}

/*
---example-metadata
slug: junkyard-playground-box2d
title:
  en: Junkyard Playground (Box2D)
  cs: Hřiště na vrakovišti (Box2D)
level: Advanced
category: Physics
complexity: 4
order: 63
description:
  en: |-
    The playground sibling of the Junkyard replica: the same walled yard and sweeping plow, built
    the Stride way - every shape is an entity carrying ShapeComponent and a Box2D body, so
    components, scripts, events and the camera all join in. Pentagons, circles, capsules and boxes
    fall and mix freely, switchable at runtime. A sensor gate turns anything passing through gold via
    the library's sensor events; the mouse picks shapes up and throws them through Grabber2DScript,
    N drops a new one at the cursor; middle-click makes the camera follow one through the pile.
  cs: |-
    Hravý sourozenec repliky vrakoviště: stejný ohrazený dvůr a radlice, ale postavené po stridím
    způsobu - každý tvar je entita s ShapeComponent a tělesem Box2D, takže se zapojují
    komponenty, skripty, události i kamera. Pětiúhelníky, kruhy, kapsle a krabice padají a míchají
    se dohromady; senzorová brána vše, co jí proletí, obarví zlatě.
concepts:
  - One entity per shape with ShapeComponent - the testbed look through the component system
  - Mixing shape kinds freely in one scene, impossible with per-model instanced masters
  - Circles and capsules through the SDF shader's rounding radius
  - Building the collider from the same vertices as the visual, so they always agree
  - Sensor fixtures and the library's sensor events driving gameplay colour
  - Picking shapes up and throwing them with Grabber2DScript, a kinematic anchor plus a motor joint
  - Camera follow through Basic2DCameraController.FollowTarget
tags:
  - 2D
  - Box2D
  - Physics
  - Shader
  - Events
  - Interaction
  - Third Party
related:
  - E06_Box2D_Junkyard
  - E10_2D_StressPile_Box2D
enabled: true
screenshotFrame: 400
created: 2026-08-31
---
*/