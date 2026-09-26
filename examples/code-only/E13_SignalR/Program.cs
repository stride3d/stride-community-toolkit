using E13_SignalR.Net;
using E13_SignalR.SignalR;
using E13_SignalR.Station;
using E13_SignalR_Shared;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Scripts.Utilities;
using Stride.CommunityToolkit.Windows;
using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Input;

// An orbital cargo deck with two consoles: this window, and a web page. A hatch above the deck
// releases containers in three sizes and six rusty paints; they drop under physics, skid, stack,
// and sometimes slide off the open edge and are lost. Either console can release, clear, shake and
// recolour; the game reports every release, landing and loss back, plus a census once a second.
//
// The game's console is drawn in the scene: a station board over the deck with the census, bars by
// size and paint, and the scheme buttons - click one - plus a feed beside the deck, all ShapeBatch
// panels and world text in the scheme's colours. Landings, losses and hails show on the deck itself.
//
// The hub is optional. The game connects in the background and keeps trying; with no hub the
// keyboard does everything and the board says LINK OFFLINE. Start the Blazor host to see the
// other console: E13_SignalR_Blazor, then open its page.
//
// Keys: 1 2 3 release small, medium, large - SPACE random - B batch of ten - C clear - X shake -
// T scheme list, then 1-5, or click a scheme on the board. Right-drag and WASD fly the camera.

const float HeartbeatSeconds = 1f;

var console = new StationConsole();
var link = new StationLink(ResolveHubUrl(), new StrideLoggerAdapter<SignalRHubClient>(GlobalLogger.GetLogger("SignalR")));

WindowsDpiManager.EnablePerMonitorV2();

// Not `using`: the disposal order at the bottom is deliberate, and `using` would reverse it
var game = new Game();

var station = new StationScene(game);
var deck = new Deck(new ContainerFactory(game));
var commands = new StationCommands(deck, console);

// Built in Start, once there is a scene to put them in
StationBoard? board = null;
FeedBoard? feed = null;
DeckEffects? effects = null;

var uptime = 0f;
var untilHeartbeat = HeartbeatSeconds;

game.Run(start: Start, update: Update);

// Shutdown order is load-bearing, and so is the fact that neither line awaits.
//
// Game.Dispose tears down the graphics device, which has to happen on the thread that created it.
// Top-level statements run on the main thread only until their first await, and an awaited
// DisposeAsync resumes on a thread-pool thread - so `await using` here, or a `using var game`
// disposed after it, would tear the device down off the main thread and hang the process with the
// window still on screen. Blocking instead keeps both disposals on this thread, and closes the hub
// connection while the game's logger is still alive to report it.
link.DisposeAsync().AsTask().GetAwaiter().GetResult();

game.Dispose();

void Start(Scene scene)
{
    game.Window.Title = $"{Constants.StationName} - Stride + SignalR";
    game.Window.AllowUserResizing = true;

    var labels = new Labels(scene, game, console);

    station.Build(scene, labels, console);

    // The boards face the camera's starting point, so they are read square-on from there
    board = new StationBoard(labels, console, StationScene.BoardCenter, StationScene.CameraPosition - StationScene.BoardCenter);
    feed = new FeedBoard(labels, console, StationScene.FeedCenter, StationScene.CameraPosition - StationScene.FeedCenter);
    effects = new DeckEffects(labels, console);

    // The deck tells the effects, the feed and the hub what happened; it does not know any exists
    deck.Released += container =>
    {
        station.Pulse();

        if (deck.Find(container.Id) is { } live) effects.OnReleased(live, console);

        console.Note($"Released {Describe(container)}", LogKind.Released);
        link.ReportReleased(container);
    };

    deck.Landed += container =>
    {
        effects.OnLanded(container, console);
        console.Note($"Landed {Describe(container)} · {container.AirTime:0.0} s", LogKind.Landed);
        link.ReportLanded(container);
    };

    deck.Lost += container =>
    {
        effects.OnLost(container);
        console.Note($"Lost {Describe(container)}", LogKind.Lost);
        link.ReportLost(container);
    };

    deck.Cleared += removed =>
    {
        effects.OnCleared();
        console.Note($"Deck cleared, {removed} removed", LogKind.Cleared);
        link.ReportCleared(removed);
    };

    // Raised for a choice made here or one that came from the web; reporting it either way is what
    // keeps every open browser tab in the same scheme as the game
    console.SchemeChanged += scheme =>
    {
        labels.Restyle();
        console.Note($"Scheme {scheme.Name}");
        link.ReportScheme(scheme.Name);
    };

    console.Hailed += text =>
    {
        effects.OnHail(console);
        console.Note($"Hail: {text}", LogKind.Hail);
    };

    // The overlay keeps only what is genuinely keyboard help; everything else is on the boards.
    // Bottom-left is the one corner with nothing behind it.
    var overlay = DebugOverlay.GetOrCreate(game);
    overlay.SectionGap = 0;
    overlay.AddSection("Station", OverlayLines);

    // A screenshot of an empty deck shows nothing. When the capture harness is driving, drop a
    // batch at once so there is cargo on the deck by the time it takes its frame.
    if (Environment.GetEnvironmentVariable(ScreenshotCapture.OutputPathVariable) is not null)
    {
        deck.ReleaseBatch(24, CommandOrigin.Game);
    }

    link.BeginConnect();
}

void Update(Scene scene, GameTime time)
{
    var deltaSeconds = (float)time.Elapsed.TotalSeconds;

    uptime += deltaSeconds;

    // Web requests first, on this thread, before anything reads the deck this frame
    link.Drain(commands);

    console.Update(game.Input, uptime);

    HandleKeys();

    if (station.Shapes is { } picking && board!.Pick(picking, game.Input) is { } clicked)
    {
        console.Select(clicked);
    }

    deck.Update(deltaSeconds);
    effects!.Update(deltaSeconds, uptime);

    var snapshot = deck.Snapshot(console.Scheme.Name, uptime);

    station.Draw(console, deltaSeconds, uptime);

    if (station.Shapes is { } shapes)
    {
        board!.Draw(shapes, console, snapshot, deck.PendingCount, link.IsConnected, uptime, uptime);
        feed!.Draw(shapes, console, uptime);
        effects.Draw(shapes, console);
    }

    untilHeartbeat -= deltaSeconds;

    if (untilHeartbeat <= 0f)
    {
        untilHeartbeat = HeartbeatSeconds;

        link.ReportDeck(snapshot);
    }
}

void HandleKeys()
{
    var input = game.Input;

    // While the scheme list is open the digits belong to it
    if (!console.IsMenuOpen)
    {
        if (input.IsKeyPressed(Keys.D1)) deck.Release(new ReleaseRequest(ContainerSize.Small), CommandOrigin.Game);
        if (input.IsKeyPressed(Keys.D2)) deck.Release(new ReleaseRequest(ContainerSize.Medium), CommandOrigin.Game);
        if (input.IsKeyPressed(Keys.D3)) deck.Release(new ReleaseRequest(ContainerSize.Large), CommandOrigin.Game);
    }

    if (input.IsKeyPressed(Keys.Space)) deck.Release(new ReleaseRequest(), CommandOrigin.Game);
    if (input.IsKeyPressed(Keys.B)) deck.ReleaseBatch(Constants.BatchSize, CommandOrigin.Game);
    if (input.IsKeyPressed(Keys.C)) deck.Clear();
    if (input.IsKeyPressed(Keys.X)) deck.Shake();
}

IReadOnlyList<TextElement> OverlayLines()
{
    List<TextElement> lines = [];
    lines.AddRange(console.MenuLines());
    lines.AddRange(
    [
        new(["1 - 3"], "Drop a container of that size", Color.Gold),
        new("Space", "Drop a random one", Color.Gold),
        new("B", "Drop a batch", Color.Gold),
        new("C", "Clear the deck", Color.Gold),
        new("X", "Shake", Color.Gold),
        new("Click", "Pick a scheme", Color.Gold),
    ]);

    return lines;
}

static string Describe(ContainerEvent container) => $"#{container.Id} {container.Size.ToString().ToLowerInvariant()} {container.Paint.ToString().ToLowerInvariant()}";

/// <summary>The hub URL: the shared default, unless the environment says otherwise.</summary>
static Uri ResolveHubUrl()
{
    var overridden = Environment.GetEnvironmentVariable(Constants.HubUrlEnvironmentVariable);

    return Uri.TryCreate(overridden, UriKind.Absolute, out var url) ? url : new Uri(Constants.HubBaseUrl, Constants.HubPath);
}

/*
---example-metadata
slug: stride-signalr
title:
  en: Stride + SignalR - Orbital Cargo Deck
level: Advanced
category: Networking
complexity: 4
order: 160
description:
  en: |-
    A Stride game and a Blazor web page as two consoles of the same orbital cargo deck, talking both
    ways over a SignalR hub. Either console releases rusty cargo containers in three sizes through a
    hatch, clears the deck, shakes it, or switches the colour scheme, and the game reports every
    release, landing and loss back to the page along with a census once a second. The game's own
    console is drawn in the scene - a station board with the census and clickable scheme buttons, a
    feed beside the deck, landings and losses marked on the deck - all ShapeBatch panels and world
    text. The hub is optional: the game connects in the background, keeps retrying, and does
    everything from the keyboard when there is no hub at all. Two processes and a server make this
    the most involved example in the toolkit to run - start the Blazor app first so the hub exists,
    then the game.
concepts:
  - Connecting a Stride game to a SignalR hub as an optional feature that retries in the background
  - Typed contracts shared by game, hub and page, with nameof on every method name
  - "Marshalling hub callbacks onto the game thread: handlers enqueue, the update loop drains"
  - One ordered background queue for everything the game sends
  - A periodic snapshot so a page that opens late is complete within a second
  - "Two-way state: a colour scheme changed on either side changes the other"
  - An in-scene console from ShapeBatch panels and world text, laid out in board coordinates
  - "Clickable world-space buttons: a pick ray intersected with the board's plane"
  - Detecting landings and losses from body velocity and position, without contact handlers
  - Shutting a game down cleanly with a live network connection
tags:
  - 3D
  - Networking
  - SignalR
  - Blazor
  - Real Time
  - Multi Project
  - ShapeBatch
  - WorldText
  - EntityText
related:
  - E13_SignalR_Blazor
tocName: Stride + SignalR
screenshotFrame: 480
enabled: true
created: 2025-05-04
---
*/