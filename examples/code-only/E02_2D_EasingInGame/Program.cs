using Stride.BepuPhysics;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Mathematics;
using Stride.CommunityToolkit.Rendering;
using Stride.CommunityToolkit.Rendering.Overlay;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Rendering.Text;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Input;

// Easing doing real work in a 2D physics scene, each piece a Tween. A kinematic lift carries a
// stack of boxes up and down on a sine in-out curve, and because a body owned by Bepu ignores
// writes to its transform, the eased value is a target the body chases with a velocity. Coins pop
// into the scene on a back ease-out, fly to the score on a quadratic ease-in when collected, and
// leave a "+10" that rises on a quadratic ease-out while it fades. X shakes the camera on an
// elastic ease-out. Every effect is the same three lines: start a tween, feed it the frame
// time, read a value.
//
// Keys: C pops the next coin in, Space collects every coin on screen, X shakes the camera,
// L pauses and resumes the lift, R drops the boxes back onto it.
//
// New to Tween? E02_2D_EasingBasics builds the same motion by hand first, then with a tween.

const float LiftLow = -2.6f;
const float LiftHigh = 2.4f;

// How hard the lift is pulled back onto its eased target when it has drifted off it
const float LiftCorrectionGain = 8f;

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
EntityTextComponent? scoreText = null;
var liftTargetBefore = liftHome;
var nextCoin = 0;
var score = 0;
var shakeBase = Vector3.Zero;

using var game = new Game();

// Closer than the default view, so the lift and the coins fill the window; the controller reads the
// size on its first frame and returns to it when H resets the camera
game.Run(new SceneSetup2D
{
    Camera = CameraSetup.Orthographic(orthographicSize: 14f).WithController(),
    WindowTitle = "Easing in a 2D game - Stride Community Toolkit",
    Resizable = true,
    DpiAware = true,
}.UseBepu(), start: Start, update: Update);

void Start(Scene rootScene)
{
    // The lift: a kinematic body, so it carries what lands on it and nothing pushes it back
    var platform = game.CreatePrimitive(Primitive2D.Rectangle(4f, 0.4f), new BepuPhysicsOptions
    {
        EntityName = "Lift",
        Material = game.CreateFlatMaterial(new Color(70, 140, 200)),
        Body = BodyMode.Kinematic,
    });

    platform.Transform.Position = liftHome;
    platform.Scene = rootScene;
    liftBody = platform.Get<BodyComponent>();

    // The boxes it carries: ordinary dynamic bodies
    boxHomes = [liftHome + new Vector3(-1f, 0.8f, 0f), liftHome + new Vector3(0.9f, 0.8f, 0f), liftHome + new Vector3(0f, 1.7f, 0f)];
    boxes = new BodyComponent[boxHomes.Length];

    for (var i = 0; i < boxHomes.Length; i++)
    {
        var box = game.CreatePrimitive(Primitive2D.Rectangle(0.7f, 0.7f), new BepuPhysicsOptions
        {
            EntityName = $"Box {i + 1}",
            Material = game.CreateFlatMaterial(i == 2 ? new Color(235, 180, 60) : new Color(220, 90, 70)),
        });

        box.Transform.Position = boxHomes[i];
        box.Scene = rootScene;
        boxes[i] = box.Get<BodyComponent>();
    }

    // The coins: visuals with no collider, hidden until they pop
    coins = new Coin[coinSpots.Length];

    for (var i = 0; i < coinSpots.Length; i++)
    {
        var spot = new Vector3(coinSpots[i], 0f);
        var coin = game.CreatePrimitive(Primitive2D.Circle(0.4f), new BepuPhysicsOptions
        {
            EntityName = $"Coin {i + 1}",
            Material = game.CreateFlatMaterial(new Color(250, 210, 60)),
            Body = BodyMode.None,
            Position = spot,
        });

        coin.Transform.Scale = Vector3.Zero;
        coin.Scene = rootScene;
        coins[i] = new Coin(coin, spot, new Tween(0.5f, EasingFunction.BackEaseOut), new Tween(0.6f, EasingFunction.QuadraticEaseIn));
    }

    // The score, as a label pinned to a point in the world
    scoreText = new EntityTextComponent { Text = "Score 0", FontSize = 22, TextColor = Color.White, Anchor = Anchor.MiddleLeft };

    var scoreEntity = new Entity("Score") { scoreText };

    scoreEntity.Transform.Position = scoreSpot;
    rootScene.Entities.Add(scoreEntity);

    // Two coins to start with, so the scene is not empty
    PopNextCoin();
    PopNextCoin();

    TextOverlay.GetOrCreate(game).AddSection("Easing", () =>
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
        var correction = (liftTarget - liftBody.Position) * LiftCorrectionGain;

        liftBody.LinearVelocity = feedForward + correction;
        liftBody.Awake = true;
    }

    liftTargetBefore = liftTarget;

    // 2. The coins: a pop by scale, then a flight by position, then gone
    foreach (var coin in coins)
    {
        coin.Pop.Update(time);
        coin.Fly.Update(time);

        // A counted coin is gone. Its flight stays complete, so without this it would be drawn
        // again at the end of the flight, small, on the score
        if (coin.Counted) continue;

        if (coin.Fly.IsRunning || coin.Fly.IsComplete)
        {
            coin.Entity.Transform.Position = coin.Fly.Lerp(coin.Spot, scoreSpot);
            coin.Entity.Transform.Scale = new Vector3(coin.Fly.Lerp(1f, 0.3f));

            if (coin.Fly.IsComplete)
            {
                var at = scoreSpot + new Vector3(2.4f, 0f, 0f);

                coin.Counted = true;
                coin.Entity.Transform.Scale = Vector3.Zero;
                score += 10;
                scoreText!.Text = $"Score {score}";
                popups.Add(new Popup(Label("+10", at, scene), at, Tween.Run(0.9f, EasingFunction.QuadraticEaseOut)));
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
        if (lift.IsRunning) lift.Stop(); else lift.Resume();
    }

    if (input.IsKeyPressed(Keys.R))
    {
        for (var i = 0; i < boxes.Length; i++)
        {
            boxes[i].TeleportAtRest(boxHomes[i] with { Y = boxHomes[i].Y + 3f });
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
    var entity = new Entity($"Popup {text}") { new EntityTextComponent { Text = text, FontSize = 18, TextColor = new Color(250, 210, 60), Anchor = Anchor.MiddleCenter } };

    entity.Transform.Position = position;
    entity.Scene = scene;

    return entity;
}

/// <summary>A coin: its entity, where it lives, the tween that brings it in and the tween that takes it to the score.</summary>
sealed record Coin(Entity Entity, Vector3 Spot, Tween Pop, Tween Fly)
{
    /// <summary>Whether this coin's flight has been added to the score, so it counts once.</summary>
    public bool Counted { get; set; }
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
    when collected, a "+10" rises and fades, and X shakes the camera on an elastic curve.
    The lift shows how an eased value drives a Bepu body: as a target the body chases with a
    velocity, never as a transform write.
  cs: |-
    Easing při skutečné práci ve 2D fyzikální scéně, každý kousek jako Tween: kinematický výtah
    vozí hromádku krabic nahoru a dolů po sinusové křivce, mince vyskočí s přestřelením a po
    sebrání odletí ke skóre, „+10" stoupá a mizí a klávesa X zatřese kamerou po elastické křivce.
    Výtah ukazuje, jak zjemněná hodnota řídí těleso Bepu: jako cíl, za kterým těleso jede
    rychlostí, nikdy zápisem do transformace.
concepts:
  - Tween - start, feed the frame time, read Lerp
  - Driving a kinematic Bepu 2D body towards an eased target with feed-forward velocity plus a correction
  - A pop-in by scale, a flight by position, a rise-and-fade on one tween
  - An elastic ease-out as a camera shake
  - Visual-only primitives with Body = BodyMode.None, next to physics bodies
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