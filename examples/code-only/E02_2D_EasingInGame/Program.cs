using Stride.BepuPhysics;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Mathematics;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Rendering.Text;
using Stride.CommunityToolkit.Scripts;
using Stride.CommunityToolkit.Scripts.Utilities;
using Stride.CommunityToolkit.Windows;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Input;

// Easing doing real work in a 2D physics scene, each piece a Tween. A kinematic lift carries a
// stack of boxes up and down on a sine in-out curve, and because a body owned by Bepu ignores
// writes to its transform, the eased value is a target the body chases with a velocity. Coins pop
// into the scene on a back ease-out, fly to the score on a quadratic ease-in when collected, and
// leave a "+10" that rises on a quadratic ease-out while it fades. A landing shakes the camera on
// an elastic ease-out. Every effect is the same three lines: start a tween, feed it the frame
// time, read a value.
//
// Keys: C pops the next coin in, Space collects every coin on screen, X shakes the camera,
// L pauses and resumes the lift, R drops the boxes back onto it. Not S: the camera controller
// owns W A S D.
//
// New to Tween? E02_2D_EasingBasics builds the same motion by hand first, then with a tween.

WindowsDpiManager.EnablePerMonitorV2();

const float LiftLow = -2.6f;
const float LiftHigh = 2.4f;

var liftHome = new Vector3(-5f, LiftLow, 0f);
var scoreSpot = new Vector3(-9f, 3.2f, 0f);
Vector2[] coinSpots = [new(2f, -1f), new(4f, 1.5f), new(6f, -0.5f), new(8f, 2f), new(3f, 3.5f)];

var lift = Tween.Run(3f, EasingFunction.SineEaseInOut, TweenLoop.PingPong);
var shake = new Tween(0.7f, EasingFunction.ElasticEaseOut);

BodyComponent? liftBody = null;
BodyComponent[] boxes = [];
Vector3[] boxHomes = [];
Coin[] coins = [];
List<Popup> popups = [];
var liftTargetBefore = liftHome;
var nextCoin = 0;
var score = 0;
var shakeBase = Vector3.Zero;

using var game = new Game();

game.Run(start: Start, update: Update);

void Start(Scene rootScene)
{
    game.Window.AllowUserResizing = true;
    game.Window.Title = "Easing in a 2D game - Stride Community Toolkit";

    game.SetupBase2DScene();
    game.AddEntityTextRenderer();

    // Closer than the default view, so the lift and the coins fill the window
    var camera = game.GetCameraEntity();

    camera.Get<CameraComponent>().OrthographicSize = 14f;
    camera.Get<Basic2DCameraController>().OrthographicSizeDefault = 14f;

    // The lift: a kinematic body, so it carries what lands on it and nothing pushes it back
    var platform = game.Create2DPrimitive(Primitive2DModelType.Rectangle, new Bepu2DPhysicsOptions
    {
        EntityName = "Lift",
        Material = game.CreateFlatMaterial(new Color(70, 140, 200)),
        Size = new Vector2(4f, 0.4f),
        Component = new Body2DComponent { Kinematic = true, Collider = new CompoundCollider() },
    });

    platform.Transform.Position = liftHome;
    platform.Scene = rootScene;
    liftBody = platform.Get<BodyComponent>();

    // The boxes it carries: ordinary dynamic bodies
    boxHomes = [liftHome + new Vector3(-1f, 0.8f, 0f), liftHome + new Vector3(0.9f, 0.8f, 0f), liftHome + new Vector3(0f, 1.7f, 0f)];
    boxes = new BodyComponent[boxHomes.Length];

    for (var i = 0; i < boxHomes.Length; i++)
    {
        var box = game.Create2DPrimitive(Primitive2DModelType.Square, new Bepu2DPhysicsOptions
        {
            EntityName = $"Box {i + 1}",
            Material = game.CreateFlatMaterial(i == 2 ? new Color(235, 180, 60) : new Color(220, 90, 70)),
            Size = new Vector2(0.7f, 0.7f),
        });

        box.Transform.Position = boxHomes[i];
        box.Scene = rootScene;
        boxes[i] = box.Get<BodyComponent>();
    }

    // The coins: visuals with no collider, hidden until they pop
    coins = new Coin[coinSpots.Length];

    for (var i = 0; i < coinSpots.Length; i++)
    {
        var coin = game.Create2DPrimitive(Primitive2DModelType.Circle, new Bepu2DPhysicsOptions
        {
            EntityName = $"Coin {i + 1}",
            Material = game.CreateFlatMaterial(new Color(250, 210, 60)),
            Size = new Vector2(0.4f, 0.4f),
            IncludeCollider = false,
        });

        coin.Transform.Position = new Vector3(coinSpots[i], 0f);
        coin.Transform.Scale = Vector3.Zero;
        coin.Scene = rootScene;
        coins[i] = new Coin(coin, new Vector3(coinSpots[i], 0f), new Tween(0.5f, EasingFunction.BackEaseOut), new Tween(0.6f, EasingFunction.QuadraticEaseIn));
    }

    // The score, as a label pinned to a point in the world
    var scoreEntity = new Entity("Score") { new EntityTextComponent { Text = "Score 0", FontSize = 22, TextColor = Color.White, Anchor = TextAnchor.MiddleLeft } };

    scoreEntity.Transform.Position = scoreSpot;
    rootScene.Entities.Add(scoreEntity);

    // Two coins to start with, so the scene is not empty
    PopNextCoin();
    PopNextCoin();

    DebugOverlay.GetOrCreate(game).AddSection("Easing", () =>
    [
        new("C", "Pop the next coin in", Color.Gold),
        new("Space", "Collect every coin on screen", Color.Gold),
        new("X", "Shake the camera", Color.Gold),
        new("L", "Pause and resume the lift", Color.Gold),
        new("R", "Drop the boxes back onto the lift", Color.Gold),
        new(""),
        new($"Score {score}", Color.LightGreen),
        new($"Lift {(lift.IsRunning ? "running" : "paused")}, sine in-out ping-pong at {lift.Progress:0.00}", Color.LightGreen),
        new("Lift: a kinematic body chasing an eased", Color.LightGray),
        new("target with a velocity.", Color.LightGray),
        new("Coins, popups, camera: transforms from tweens.", Color.LightGray),
    ]);

    scoreEntity.Get<EntityTextComponent>().Text = $"Score {score}";
}

void Update(Scene scene, GameTime time)
{
    HandleInput(scene);

    var dt = (float)time.Elapsed.TotalSeconds;

    // 1. The lift: the tween says where it should be; the body is given the speed to get there
    lift.Update(time);

    var liftTarget = lift.Lerp(liftHome, liftHome with { Y = LiftHigh });

    if (liftBody is not null && dt > 0f)
    {
        var feedForward = (liftTarget - liftTargetBefore) / dt;
        var correction = (liftTarget - liftBody.Position) * 8f;

        liftBody.LinearVelocity = feedForward + correction;
        liftBody.Awake = true;
    }

    liftTargetBefore = liftTarget;

    // 2. The coins: a pop by scale, then a flight by position, then gone
    foreach (var coin in coins)
    {
        coin.Pop.Update(time);
        coin.Fly.Update(time);

        if (coin.Fly.IsRunning || coin.Fly.IsComplete)
        {
            coin.Entity.Transform.Position = coin.Fly.Lerp(coin.Spot, scoreSpot);
            coin.Entity.Transform.Scale = new Vector3(coin.Fly.Lerp(1f, 0.3f));

            if (coin.Fly.IsComplete && !coin.Counted)
            {
                coin.Counted = true;
                coin.Entity.Transform.Scale = Vector3.Zero;
                score += 10;
                scene.Entities.First(e => e.Name == "Score").Get<EntityTextComponent>().Text = $"Score {score}";
                popups.Add(new Popup(Label("+10", scoreSpot + new Vector3(2.4f, 0f, 0f), scene), scoreSpot + new Vector3(2.4f, 0f, 0f), Tween.Run(0.9f, EasingFunction.QuadraticEaseOut)));
            }
        }
        else
        {
            coin.Entity.Transform.Scale = new Vector3(coin.Pop.Lerp(0f, 1f));
        }
    }

    // 3. The popups: rise and fade on one tween, then leave the scene
    foreach (var popup in popups)
    {
        popup.Rise.Update(time);
        popup.Entity.Transform.Position = popup.From + new Vector3(0f, popup.Rise.Lerp(0f, 1.2f), 0f);
        popup.Entity.Get<EntityTextComponent>().Opacity = popup.Rise.Lerp(1f, 0f);

        if (popup.Rise.IsComplete) scene.Entities.Remove(popup.Entity);
    }

    popups.RemoveAll(popup => popup.Rise.IsComplete);

    // 4. The camera: an elastic curve is already a shake; 1 - value is the amplitude that dies away
    shake.Update(time);

    if (shake.IsRunning)
    {
        game.GetCameraEntity().Transform.Position = shakeBase + new Vector3((1f - shake.Value) * 0.6f, 0f, 0f);
    }
}

void HandleInput(Scene scene)
{
    var input = game.Input;

    if (input.IsKeyPressed(Keys.C)) PopNextCoin();

    if (input.IsKeyPressed(Keys.Space))
    {
        foreach (var coin in coins)
        {
            if (coin.Pop.IsComplete && !coin.Fly.IsRunning && !coin.Counted) coin.Fly.Start();
        }
    }

    if (input.IsKeyPressed(Keys.X))
    {
        shakeBase = game.GetCameraEntity().Transform.Position;
        shake.Start();
    }

    if (input.IsKeyPressed(Keys.L))
    {
        if (lift.IsRunning) lift.Stop();
        else lift.Resume();
    }

    if (input.IsKeyPressed(Keys.R))
    {
        for (var i = 0; i < boxes.Length; i++)
        {
            boxes[i].Teleport(boxHomes[i] with { Y = boxHomes[i].Y + 3f }, Quaternion.Identity);
            boxes[i].LinearVelocity = Vector3.Zero;
            boxes[i].AngularVelocity = Vector3.Zero;
            boxes[i].Awake = true;
        }
    }
}

// Coins come back round once every spot has been used, so C always does something
void PopNextCoin()
{
    var coin = coins[nextCoin % coins.Length];

    nextCoin++;
    coin.Counted = false;
    coin.Fly.Reset();
    coin.Entity.Transform.Position = coin.Spot;
    coin.Pop.Start();
}

static Entity Label(string text, Vector3 position, Scene scene)
{
    var entity = new Entity($"Popup {text}") { new EntityTextComponent { Text = text, FontSize = 18, TextColor = new Color(250, 210, 60), Anchor = TextAnchor.MiddleCenter } };

    entity.Transform.Position = position;
    entity.Scene = scene;

    return entity;
}

/// <summary>A coin: its entity, where it lives, the tween that brings it in and the tween that takes it to the score.</summary>
internal sealed class Coin
{
    internal Coin(Entity entity, Vector3 spot, Tween pop, Tween fly)
    {
        Entity = entity;
        Spot = spot;
        Pop = pop;
        Fly = fly;
    }

    internal Entity Entity { get; }

    internal Vector3 Spot { get; }

    internal Tween Pop { get; }

    internal Tween Fly { get; }

    /// <summary>Whether this coin's flight has been added to the score, so it counts once.</summary>
    internal bool Counted { get; set; }
}

/// <summary>A "+10" on its way up: the label, where it started and the tween that lifts and fades it.</summary>
sealed record Popup(Entity Entity, Vector3 From, Tween Rise);

/*
---example-metadata
slug: easing-2d-game
title:
  en: Easing in a 2D Game
  cs: Easing ve 2D hře
level: Beginner
category: Mathematics
complexity: 3
order: 67
description:
  en: |-
    Easing doing real work in a 2D physics scene, each piece a Tween: a kinematic lift carries a
    stack of boxes up and down on a sine curve, coins pop in with an overshoot and fly to the score
    when collected, a "+10" rises and fades, and a landing shakes the camera on an elastic curve.
    The lift shows how an eased value drives a Bepu body: as a target the body chases with a
    velocity, never as a transform write.
  cs: |-
    Easing při skutečné práci ve 2D fyzikální scéně, každý kousek jako Tween: kinematický výtah
    vozí hromádku krabic nahoru a dolů po sinusové křivce, mince vyskočí s přestřelením a po
    sebrání odletí ke skóre, „+10" stoupá a mizí a přistání zatřese kamerou po elastické křivce.
    Výtah ukazuje, jak zjemněná hodnota řídí těleso Bepu: jako cíl, za kterým těleso jede
    rychlostí, nikdy zápisem do transformace.
concepts:
  - Tween - start, feed the frame time, read Lerp
  - Driving a kinematic Bepu 2D body towards an eased target with feed-forward velocity plus a correction
  - A pop-in by scale, a flight by position, a rise-and-fade on one tween
  - An elastic ease-out as a camera shake
  - Visual-only primitives with IncludeCollider off, next to physics bodies
tags:
  - 2D
  - Mathematics
  - Easing
  - Animation
  - Physics
related:
  - E02_2D_Easing
  - E02_2D_EasingBasics
  - E02_3D_EasingInGame
  - E20_3D_CubeCollapse
enabled: true
created: 2026-09-14
---
*/