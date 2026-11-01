using Box2D.NET;
using Stride.CommunityToolkit.Box2D;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.Overlay;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Rendering.Text;
using Stride.CommunityToolkit.Shapes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Input;

// What Box2D tells you about touching, and what you have to ask for. Shapes drop through a sensor
// zone onto a floor, and one handler listens to all three kinds of event the library delivers:
//
//   begin and end touch - a pair of shapes starts and stops touching;
//   hit                 - an impact, with the point, the normal and the approach speed;
//   sensor              - a shape enters or leaves a sensor, a shape that detects and never collides.
//
// Every one of them is opt-in on the shape: Box2D.NET's default shape definition has contact, hit and
// sensor events all off, so a shape that does not say enableContactEvents reports nothing however
// loudly it lands. The simulation has a switch for each kind as well, the second gate, and the keys
// here flip those to show which lines of the log each one owns. A shape inside the sensor turns gold;
// a shape that is hit flashes, brighter the faster it came in.

const float FloorHalfWidth = 22f;
const float SpawnHeight = 20f;
const int BatchSize = 8;
const int LogLines = 8;

var pink = new Color(0xFF, 0xC0, 0xCB);
var paleGreen = new Color(0x98, 0xFB, 0x98);
var royalBlue = new Color(0x41, 0x69, 0xE1);
var wheat = new Color(0xF5, 0xDE, 0xB3);
var gold = new Color(0xFF, 0xD7, 0x00);

var log = new EventLog(LogLines);
var shapes = new List<(Entity Entity, ShapeComponent Component, Color Colour)>();
var random = new Random(5);

using var game = new Game();
using var simulation = new Box2DSimulation();

var sceneSetup = new SceneSetup2D
{
    ClearColor = new Color(0.2f, 0.2f, 0.2f),
    Camera = CameraSetup.Orthographic(position: new Vector3(0f, 9f, 50f), orthographicSize: 30f).WithController(),
    Ground = null,
    WindowTitle = "Box2D Events - Stride Community Toolkit",
    Resizable = true,
};

game.Run(sceneSetup, start: Start, update: Update);

void Start(Scene scene)
{
    var batch = game.AddShapeBatch();

    batch.BorderWidth = 1.5f;
    batch.Fill.Alpha = 0.4f;

    simulation.Gravity = new Vector2(0f, -10f);

    // The handler hears every shape that opted in; one object serves both interfaces
    simulation.RegisterContactEventHandler(log);
    simulation.RegisterSensorEventHandler(log);

    // Left mouse picks a shape up and throws it, which is the quickest way to make a hit
    game.AddGrabber2D(simulation);

    BuildFloor(scene);
    BuildSensor(scene);
    Spawn(scene);

    TextOverlay.GetOrCreate(game).AddSection("Box2D events", BuildOverlayLines);
}

void Update(Scene scene, GameTime time)
{
    simulation.Update(time.Elapsed);

    var input = game.Input;

    if (input.IsKeyPressed(Keys.Space)) Spawn(scene);
    if (input.IsKeyPressed(Keys.C)) Clear();
    if (input.IsKeyPressed(Keys.E)) simulation.EnableContactEvents = !simulation.EnableContactEvents;
    if (input.IsKeyPressed(Keys.I)) simulation.EnableHitEvents = !simulation.EnableHitEvents;
    if (input.IsKeyPressed(Keys.N)) simulation.EnableSensorEvents = !simulation.EnableSensorEvents;

    log.Age((float)time.Elapsed.TotalSeconds);

    // Every shape's colour each frame: gold inside the sensor, a white flash fading after a hit, else its own
    foreach (var (entity, component, colour) in shapes)
    {
        var flash = log.FlashOf(entity);
        var resting = log.IsInsideSensor(entity) ? gold : colour;

        component.Color = flash > 0f ? Color.Lerp(resting, Color.White, flash) : resting;
    }
}

void BuildFloor(Scene scene)
{
    var size = new Vector2(FloorHalfWidth * 2f, 1f);
    var entity = new Entity("Floor") { new ShapeComponent { Vertices = [.. ShapeComponent.Box(size)], Color = paleGreen } };

    entity.Transform.Position = new Vector3(0f, -0.5f, 0f);
    entity.Scene = scene;

    var body = simulation.CreateStaticBody(entity, new Vector3(0f, -0.5f, 0f));

    // The floor reports too: a hit is between two shapes, and either may be the one that opted in
    ShapeFixtureBuilder.AttachShape(Primitive2D.Rectangle(size.X, size.Y), body, Reporting(ShapeFixtureBuilder.CreateDefaultShapeDef()));
}

/// <summary>A sensor across the middle of the drop: it detects what passes and stops nothing.</summary>
void BuildSensor(Scene scene)
{
    var size = new Vector2(16f, 5f);
    var centre = new Vector3(0f, 9f, 0f);
    var entity = new Entity("Sensor") { new ShapeComponent { Vertices = [.. ShapeComponent.Box(size)], Color = wheat, FillAlpha = 0.12f } };

    entity.Transform.Position = centre;
    entity.Scene = scene;

    var body = simulation.CreateStaticBody(entity, centre);
    var definition = ShapeFixtureBuilder.CreateCustomShapeDef(1f, 0.6f, 0f, isSensor: true);

    // A sensor needs its own flag on, and so does every shape it is to notice
    definition.enableSensorEvents = true;

    ShapeFixtureBuilder.AttachShape(Primitive2D.Rectangle(size.X, size.Y), body, definition);
}

/// <summary>A row of boxes and discs above the sensor, each a little off the one beside it so they land at different times.</summary>
void Spawn(Scene scene)
{
    for (var i = 0; i < BatchSize; i++)
    {
        var x = (i - (BatchSize - 1) / 2f) * 2.4f + (random.NextSingle() - 0.5f);
        var position = new Vector3(x, SpawnHeight + random.NextSingle() * 4f, 0f);

        if (i % 2 == 0)
        {
            AddBox(scene, position, new Vector2(1.2f + random.NextSingle(), 1.2f + random.NextSingle()), i % 4 == 0 ? pink : royalBlue);
        }
        else
        {
            AddDisc(scene, position, 0.6f + random.NextSingle() * 0.5f, i % 4 == 1 ? royalBlue : pink);
        }
    }
}

void AddBox(Scene scene, Vector3 position, Vector2 size, Color colour)
{
    var component = new ShapeComponent { Vertices = [.. ShapeComponent.Box(size)], Color = colour };
    var entity = new Entity("Box") { component };

    entity.Transform.Position = position;
    entity.Scene = scene;

    var body = simulation.CreateDynamicBody(entity, position, random.NextSingle());

    ShapeFixtureBuilder.AttachShape(Primitive2D.Rectangle(size.X, size.Y), body, Reporting(ShapeFixtureBuilder.CreateDefaultShapeDef()));
    entity.Add(new Box2DBodyComponent { BodyId = body });
    shapes.Add((entity, component, colour));
}

void AddDisc(Scene scene, Vector3 position, float radius, Color colour)
{
    var component = new ShapeComponent { Vertices = [Vector2.Zero], Radius = radius, Color = colour };
    var entity = new Entity("Disc") { component };

    entity.Transform.Position = position;
    entity.Scene = scene;

    var body = simulation.CreateDynamicBody(entity, position);

    ShapeFixtureBuilder.AttachShape(Primitive2D.Circle(radius), body, Reporting(ShapeFixtureBuilder.CreateDefaultShapeDef()));
    entity.Add(new Box2DBodyComponent { BodyId = body });
    shapes.Add((entity, component, colour));
}

// The three opt-ins on a shape. Without them the shape is silent: the library's defaults are all off
static B2ShapeDef Reporting(B2ShapeDef definition)
{
    definition.enableContactEvents = true;
    definition.enableHitEvents = true;
    definition.enableSensorEvents = true;

    return definition;
}

void Clear()
{
    foreach (var (entity, _, _) in shapes)
    {
        simulation.RemoveBody(entity);
        entity.Scene = null;
    }

    shapes.Clear();
    log.Forget();
}

IReadOnlyList<TextElement> BuildOverlayLines()
{
    var lines = new List<TextElement>
    {
        new("Space", "Drop a batch", Color.Gold),
        new("C", "Clear the shapes", Color.Gold),
        new("Left mouse", "Grab and throw a shape", Color.Gold),
        new("E", simulation.EnableContactEvents ? "Begin and end touch events: on" : "Begin and end touch events: off", simulation.EnableContactEvents ? Color.LightGreen : Color.OrangeRed),
        new("I", simulation.EnableHitEvents ? "Hit events: on" : "Hit events: off", simulation.EnableHitEvents ? Color.LightGreen : Color.OrangeRed),
        new("N", simulation.EnableSensorEvents ? "Sensor events: on" : "Sensor events: off", simulation.EnableSensorEvents ? Color.LightGreen : Color.OrangeRed),
        new(""),
        new($"Touches begun {log.Begins}, ended {log.Ends}, hits {log.Hits}, inside the sensor {log.InsideCount}", Color.White),
        new(""),
    };

    foreach (var line in log.Lines) lines.Add(new(line, Color.LightGray));

    return lines;
}

/// <summary>
/// One listener for both kinds of event: it keeps the last few as text, counts them, tracks who is inside a
/// sensor, and times a flash for every shape that was hit, scaled by how fast it came in.
/// </summary>
public sealed class EventLog(int capacity) : IContactEventHandler, ISensorEventHandler
{
    private const float FlashSeconds = 0.4f;
    private const float LoudSpeed = 12f;

    private readonly Queue<string> _lines = new();
    private readonly Dictionary<Entity, int> _inside = [];
    private readonly Dictionary<Entity, float> _flashes = [];
    private readonly List<Entity> _faded = [];

    public int Begins { get; private set; }
    public int Ends { get; private set; }
    public int Hits { get; private set; }
    public int InsideCount => _inside.Count;
    public IEnumerable<string> Lines => _lines;

    public bool IsInsideSensor(Entity entity) => _inside.ContainsKey(entity);

    /// <summary>How bright the entity's flash is now, 1 at the hit and 0 once it has faded.</summary>
    public float FlashOf(Entity entity) => _flashes.TryGetValue(entity, out var flash) ? flash : 0f;

    public void OnContactEvent(ContactEventData data)
    {
        switch (data.Type)
        {
            case ContactEventType.BeginTouch:
                Begins++;
                Add($"{data.EntityA.Name} touches {data.EntityB.Name}");
                break;

            case ContactEventType.EndTouch:
                Ends++;
                Add($"{data.EntityA.Name} leaves {data.EntityB.Name}");
                break;

            case ContactEventType.Hit:
                Hits++;
                Add($"{data.EntityA.Name} hits {data.EntityB.Name} at {data.ApproachSpeed:0.0} m/s");

                // The flash goes on whichever of the pair is a dropped shape; a static floor keeps its colour anyway
                var brightness = Math.Clamp(data.ApproachSpeed / LoudSpeed, 0.3f, 1f);

                _flashes[data.EntityA] = brightness;
                _flashes[data.EntityB] = brightness;
                break;
        }
    }

    public void OnSensorEvent(SensorEventData data)
    {
        // Counted, not toggled: a shape that overlaps two sensor shapes leaves when it has left both
        if (data.Type == SensorEventType.BeginTouch)
        {
            _inside[data.VisitorEntity] = _inside.GetValueOrDefault(data.VisitorEntity) + 1;
            Add($"{data.VisitorEntity.Name} enters the sensor");
        }
        else if (_inside.TryGetValue(data.VisitorEntity, out var depth))
        {
            if (depth <= 1) _inside.Remove(data.VisitorEntity); else _inside[data.VisitorEntity] = depth - 1;

            Add($"{data.VisitorEntity.Name} leaves the sensor");
        }
    }

    /// <summary>Fades every flash by the frame's time.</summary>
    public void Age(float elapsed)
    {
        _faded.Clear();

        foreach (var (entity, flash) in _flashes)
        {
            var next = flash - elapsed / FlashSeconds;

            if (next <= 0f) _faded.Add(entity); else _flashes[entity] = next;
        }

        foreach (var entity in _faded) _flashes.Remove(entity);
    }

    public void Forget()
    {
        _inside.Clear();
        _flashes.Clear();
    }

    private void Add(string line)
    {
        _lines.Enqueue(line);

        while (_lines.Count > capacity) _lines.Dequeue();
    }
}

/*
---example-metadata
slug: box2d-events
title:
  en: Box2D Events
level: Beginner
category: Physics
complexity: 2
order: 91
description:
  en: |-
    Shapes drop through a sensor zone onto a floor and one handler listens to everything Box2D
    reports: begin and end touch, hits with their approach speed, and a sensor's visitors. Every
    kind is opt-in on the shape definition, where the library's defaults are all off, and switched
    on the simulation as well; the keys flip the simulation's switches to show which lines of the
    log each one owns. A shape inside the sensor turns gold and a hit shape flashes.
concepts:
  - "IContactEventHandler and ISensorEventHandler registered on the Box2DSimulation"
  - "enableContactEvents, enableHitEvents and enableSensorEvents on the shape definition, all off by default"
  - A sensor shape that detects and never collides
  - The hit event's approach speed as a measure of the impact
  - "Using helpers: SceneSetup2D, Box2DSimulation, ShapeFixtureBuilder, ShapeComponent, AddGrabber2D, TextOverlay"
tags:
  - 2D
  - Physics
  - Box2D
  - Events
  - Sensor
related:
  - E06_Box2D
  - E06_Box2D_JunkyardInteractive
  - E05_3D_TriggerZones
screenshotFrame: 150
enabled: true
created: 2026-11-01
---
*/