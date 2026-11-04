using Stride.BepuPhysics;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.BepuPhysics.Definitions.Contacts;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.Overlay;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Rendering.Text;
using Stride.CommunityToolkit.Shapes;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Input;
using Stride.Rendering;

// Dodgeball on four collision layers. Each team's players stand on a layer of their own, and each
// team's balls fly on a layer of their own; the matrix says that a team's balls pass through its own
// players and hit the other team's. A ball's contact handler scores the hit, so the rules of the game
// and the score both come from the layers and the contacts, with no code asking "is this one of mine"
// in the middle of a collision.
//
// Each team throws a ball every second at a random opponent. F turns friendly fire on: the matrix pairs
// a team's balls with its own players, and a ball that would have passed through a team-mate now hits
// them and scores for the other side. E05_3D_CollisionLayer is the example for the matrix itself.

const float CourtHalfLength = 7f;
const float CourtHalfWidth = 5f;
const int PlayersPerTeam = 5;
const float ThrowInterval = 1f;
const float ThrowSpeed = 13f;
const float BallRadius = 0.25f;
const float BallLife = 5f;

const CollisionLayer PlayersA = CollisionLayer.Layer1;
const CollisionLayer PlayersB = CollisionLayer.Layer2;
const CollisionLayer BallsA = CollisionLayer.Layer3;
const CollisionLayer BallsB = CollisionLayer.Layer4;

Color[] teamColours = [new(90, 170, 255), new(255, 120, 90)];
CollisionLayer[] playerLayers = [PlayersA, PlayersB];
CollisionLayer[] ballLayers = [BallsA, BallsB];

var scoreboard = new Scoreboard();
var handlers = new BallHits[] { new(0, scoreboard), new(1, scoreboard) };
var players = new List<Entity>[] { [], [] };
var balls = new List<(Entity Entity, float Age)>();
var random = new Random(21);
var untilThrow = ThrowInterval;
var friendlyFire = false;

BepuSimulation simulation = null!;
ShapeBatch shapes = null!;
Material[] ballMaterials = [];

using var game = new Game();

var sceneSetup = new SceneSetup3D
{
    Camera = CameraSetup.Perspective(position: new Vector3(0f, 9f, 16f), rotation: new Vector3(0f, -28f, 0f)).WithController(),
    WindowTitle = "Dodgeball - Stride Community Toolkit",
    Resizable = true,
    DpiAware = true,
}.UseBepu().UseSkybox();

game.Run(sceneSetup, start: Start, update: Update);

void Start(Scene scene)
{
    simulation = game.GetCameraEntity().GetSimulation();
    shapes = game.AddShapeBatch(depthTest: false);
    ballMaterials = [.. teamColours.Select(colour => game.CreateMaterial(colour, metalness: 0.1f, glossiness: 0.6f))];

    BuildCourt(scene);

    for (var team = 0; team < 2; team++)
    {
        AddTeam(scene, team);
    }

    ApplyMatrix();

    TextOverlay.GetOrCreate(game).AddSection("Dodgeball", BuildOverlayLines);
}

void Update(Scene scene, GameTime time)
{
    var elapsed = (float)time.Elapsed.TotalSeconds;
    var input = game.Input;

    if (input.IsKeyPressed(Keys.Space)) Volley(scene);

    if (input.IsKeyPressed(Keys.F))
    {
        friendlyFire = !friendlyFire;
        ApplyMatrix();
    }

    if (input.IsKeyPressed(Keys.R)) scoreboard.Reset();

    untilThrow -= elapsed;

    if (untilThrow <= 0f)
    {
        untilThrow += ThrowInterval;
        Throw(scene, 0);
        Throw(scene, 1);
    }

    AgeBalls(elapsed);
    scoreboard.DrawFlashes(shapes, elapsed);
}

// The whole game in four lines: a team's balls hit the other team, and its own team only with friendly fire on
void ApplyMatrix()
{
    simulation.CollisionMatrix.Set(BallsA, PlayersB, shouldCollide: true);
    simulation.CollisionMatrix.Set(BallsB, PlayersA, shouldCollide: true);
    simulation.CollisionMatrix.Set(BallsA, PlayersA, friendlyFire);
    simulation.CollisionMatrix.Set(BallsB, PlayersB, friendlyFire);
}

void BuildCourt(Scene scene)
{
    // The centre line, a marker with no body
    var line = game.CreatePrimitive(Primitive3D.Box(new Vector3(0.1f, 0.02f, CourtHalfWidth * 2f)), new BepuPhysicsOptions
    {
        EntityName = "Centre line",
        Body = BodyMode.None,
        Material = game.CreateMaterial(Color.White),
        Position = new Vector3(0f, 0.01f, 0f),
    });

    line.Scene = scene;
}

/// <summary>A row of players standing on their side of the court, static capsules on the team's layer.</summary>
void AddTeam(Scene scene, int team)
{
    var material = game.CreateMaterial(teamColours[team], metalness: 0f, glossiness: 0.4f);
    var side = team == 0 ? -1f : 1f;

    for (var i = 0; i < PlayersPerTeam; i++)
    {
        var z = (i - (PlayersPerTeam - 1) / 2f) * (CourtHalfWidth * 2f / PlayersPerTeam);
        var x = side * (CourtHalfLength * 0.6f + random.NextSingle() * CourtHalfLength * 0.35f);

        var player = game.CreatePrimitive(Primitive3D.Capsule(0.35f, 0.6f), new BepuPhysicsOptions
        {
            EntityName = $"Team {(team == 0 ? "A" : "B")} player {i + 1}",
            Body = BodyMode.Static,
            Material = material,
            ShareModel = true,
            Position = new Vector3(x, 0.65f, z),
        });

        player.Get<StaticComponent>().CollisionLayer = playerLayers[team];
        player.Scene = scene;
        players[team].Add(player);
        scoreboard.Register(player, team);
    }
}

/// <summary>One ball from a random player of the team, lobbed at a random player of the other.</summary>
void Throw(Scene scene, int team)
{
    var thrower = players[team][random.Next(players[team].Count)];
    var target = players[1 - team][random.Next(players[1 - team].Count)];
    var from = thrower.Transform.Position + new Vector3(0f, 0.9f, 0f);
    var to = target.Transform.Position + new Vector3(0f, 0.6f, 0f);
    var delta = to - from;

    // Aimed a little above the target, so the ball arcs; the same lob every time, and still a miss now and then
    var direction = Vector3.Normalize(delta + Vector3.UnitY * (delta.Length() * 0.12f));

    var ball = game.CreatePrimitive(Primitive3D.Sphere(BallRadius), new BepuPhysicsOptions
    {
        EntityName = $"Ball of team {(team == 0 ? "A" : "B")}",
        Material = ballMaterials[team],
        ShareModel = true,
        Component = new BodyComponent { Collider = new CompoundCollider(), ContactEventHandler = handlers[team] },
        Position = from + direction * 0.6f,
    });

    var body = ball.Get<BodyComponent>();

    body.CollisionLayer = ballLayers[team];
    ball.Scene = scene;
    body.LinearVelocity = direction * ThrowSpeed;

    balls.Add((ball, 0f));
}

void Volley(Scene scene)
{
    for (var i = 0; i < 3; i++)
    {
        Throw(scene, 0);
        Throw(scene, 1);
    }
}

void AgeBalls(float elapsed)
{
    for (var i = balls.Count - 1; i >= 0; i--)
    {
        var (entity, age) = balls[i];

        age += elapsed;

        if (age >= BallLife || entity.Transform.Position.Y < -5f)
        {
            entity.Scene = null;
            balls.RemoveAt(i);
            continue;
        }

        balls[i] = (entity, age);
    }
}

IReadOnlyList<TextElement> BuildOverlayLines() =>
[
    new("Space", "A volley from both teams", Color.Gold),
    new("F", friendlyFire ? "Friendly fire: on, a team's balls hit its own players too" : "Friendly fire: off, a team's balls pass through its own players", friendlyFire ? Color.OrangeRed : Color.LightGreen),
    new("R", "Reset the score", Color.Gold),
    new(""),
    new($"Team A: {scoreboard.Scores[0]} hits", teamColours[0]),
    new($"Team B: {scoreboard.Scores[1]} hits", teamColours[1]),
    new($"Balls in the air: {balls.Count}", Color.White),
];

/// <summary>Who is on which team, the hits each team has landed, and a flash where every hit happened.</summary>
public sealed class Scoreboard
{
    private const float FlashLife = 0.5f;

    private readonly Dictionary<Entity, int> _teams = [];
    private readonly (Vector3 Point, float Age)[] _flashes = new (Vector3, float)[16];
    private int _nextFlash;

    public int[] Scores { get; } = new int[2];

    public Scoreboard()
    {
        for (var i = 0; i < _flashes.Length; i++) _flashes[i].Age = FlashLife;
    }

    public void Register(Entity player, int team) => _teams[player] = team;

    /// <summary>The team the entity plays for, or -1 for a ball, the ground or anything else.</summary>
    public int TeamOf(Entity entity) => _teams.TryGetValue(entity, out var team) ? team : -1;

    /// <summary>A hit by a ball of one team on a player of another: a point to the thrower, and a flash on the player.</summary>
    public void Hit(int throwingTeam, int hitTeam, Vector3 point)
    {
        // With friendly fire on a team can hit itself; the point goes to the other side
        Scores[hitTeam == throwingTeam ? 1 - throwingTeam : throwingTeam]++;

        _flashes[_nextFlash] = (point, 0f);
        _nextFlash = (_nextFlash + 1) % _flashes.Length;
    }

    public void Reset() => Array.Clear(Scores);

    public void DrawFlashes(ShapeBatch shapes, float elapsed)
    {
        shapes.StrokeWidth = 2f;

        for (var i = 0; i < _flashes.Length; i++)
        {
            ref var flash = ref _flashes[i];

            if (flash.Age >= FlashLife) continue;

            flash.Age += elapsed;

            var t = flash.Age / FlashLife;

            shapes.Opacity = 1f - t;
            shapes.DrawBillboardCircle(flash.Point, 0.3f + t * 0.9f, Color.White);
        }

        shapes.Opacity = 1f;
    }
}

/// <summary>A team's ball handler: every first touch with a player of any team is a hit for the scoreboard to judge.</summary>
public sealed class BallHits(int team, Scoreboard scoreboard) : IContactHandler
{
    public bool NoContactResponse => false;

    void IContactHandler.OnStartedTouching<TManifold>(Contacts<TManifold> contacts)
    {
        var hitTeam = scoreboard.TeamOf(contacts.Other.Entity);

        if (hitTeam < 0) return;

        var contact = contacts.GetEnumerator();

        scoreboard.Hit(team, hitTeam, contact.MoveNext() ? contact.Current.Point : contacts.Other.Entity.Transform.Position);
    }
}

/*
---example-metadata
slug: dodgeball
title:
  en: Dodgeball
level: Intermediate
category: Physics
complexity: 2
order: 84
description:
  en: |-
    Dodgeball on four collision layers: each team's players and each team's balls have a layer of
    their own, and the matrix says a team's balls pass through its own players and hit the other
    team's. Each ball's contact handler scores the hit, so the rules and the score both come from
    the layers and the contacts. The teams throw by themselves; F turns friendly fire on by pairing a
    team's balls with its own players in the matrix.
concepts:
  - Four layers and the matrix pairs that make the rules of a game
  - A contact handler on a projectile that scores off the entity it touches
  - Friendly fire as one matrix pair flipped at runtime
  - One shared model for every ball and every player, through ShareModel
  - "Using helpers: SceneSetup3D, UseBepu, UseSkybox, CreatePrimitive, CreateMaterial, GetSimulation, ShapeBatch, TextOverlay"
tags:
  - 3D
  - Physics
  - Bepu
  - Collision filtering
  - Contacts
related:
  - E05_3D_CollisionLayer
  - E05_3D_DebrisFiltering
  - E05_3D_CollisionGroup
screenshotFrame: 200
enabled: true
created: 2026-11-01
---
*/