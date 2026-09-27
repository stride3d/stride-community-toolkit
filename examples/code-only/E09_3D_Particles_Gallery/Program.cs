using E09_3D_Particles_Gallery;
using Example.Common;
using Example.Common.Galleries;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Scripts.Utilities;
using Stride.CommunityToolkit.Skyboxes;
using Stride.CommunityToolkit.Windows;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Input;

// A gallery of the particle system, built entirely from code. Every station is one static method
// in Stations.*.cs that builds a particle system in its station's own coordinates and knows nothing
// about where it stands; the ring, the ground, the labels, the index board and the camera flights
// come from the shared gallery frame in Example.Common. Most stations have a few variations of
// their own parameter - press V - so "what does this setting do" is a keypress, not a rebuild.
//
// The first twenty-three stations are the building blocks one at a time: spawners, shapes,
// initializers, updaters, materials. The last eight put several together - a campfire, fireworks
// with child emitters, a tornado, a swarm driven by an updater of our own, lasers, rain that
// splashes, a portal from an initializer of our own, a rocket engine - because that is what the
// system is for.
//
// Keys: N and P fly to the next and previous station, Home flies home to the index board, Tab
// shows one station at a time, L widens the labels, V cycles the current station's variation,
// Space restarts the current station's simulation. "--station 25" starts at a station;
// "--station 25 --variation 2" at its third variation - handy for screenshots.

var startStation = args.Length >= 2 && args[0] == "--station" && int.TryParse(args[1], out var number) ? number : 0;
var startVariation = args.Length >= 4 && args[2] == "--variation" && int.TryParse(args[3], out var variation) ? variation : 0;

Gallery<ParticleStation>? gallery = null;
ParticleTextures? textures = null;

WindowsDpiManager.EnablePerMonitorV2();

using var game = new Game();

game.Run(start: Start, update: Update);

void Start(Scene rootScene)
{
    game.Window.AllowUserResizing = true;
    game.Window.Title = "Particle Gallery - Stride Community Toolkit";

    game.SetupBase3D();
    game.Add3DCameraController();
    game.AddSkybox();
    game.AddProfiler();
    game.AddParticleRenderer();

    // The text renderers, appended after the camera renderer, so labels land on top of everything
    game.AddWorldTextRenderer();
    game.AddEntityTextRenderer();

    textures = ParticleTextures.Load(game.GraphicsDevice);

    // The frame comes from Example.Common; what a particle station needs on top of it is the textures
    var loaded = textures;

    gallery = new Gallery<ParticleStation>(game, rootScene, Stations.All, configure: station =>
    {
        station.Textures = loaded;
        if (station.Number == startStation) station.Variation = startVariation;
    });
    gallery.UpdateLabels();

    if (startStation > 0) gallery.GoTo(startStation - 1, instant: true);
    else gallery.GoHome(instant: true);

    DebugOverlay.GetOrCreate(game).AddSection("Gallery", BuildOverlayLines);
}

void Update(Scene scene, GameTime gameTime)
{
    if (gallery is null) return;

    HandleInput(gallery);
    gallery.Update(gameTime);

    // One station at a time means one simulation at a time: the others pause where they are
    foreach (var station in gallery.Stations)
    {
        if (station.Particles is { } particles)
        {
            particles.Enabled = !gallery.Solo || station.IsCurrent;
        }
    }
}

void HandleInput(Gallery<ParticleStation> gallery)
{
    if (game.Input.IsKeyPressed(Keys.N)) gallery.Step(+1);
    if (game.Input.IsKeyPressed(Keys.P)) gallery.Step(-1);
    if (game.Input.IsKeyPressed(Keys.Home)) gallery.GoHome();
    if (game.Input.IsKeyPressed(Keys.Tab)) gallery.Solo = !gallery.Solo;

    if (game.Input.IsKeyPressed(Keys.L))
    {
        gallery.LabelDetail = (gallery.LabelDetail + 1) % 3;
        gallery.UpdateLabels();
    }

    if (gallery.CurrentStation is not { } station) return;

    // A variation is the same setup method with a different branch taken: bump and rebuild
    if (game.Input.IsKeyPressed(Keys.V) && station.VariationNames.Count > 1)
    {
        station.Variation++;
        Gallery<ParticleStation>.Guarded(station, () => Stations.All[gallery.Current].Setup?.Invoke(station));
    }

    if (game.Input.IsKeyPressed(Keys.Space)) station.Restart();
}

IReadOnlyList<TextElement> BuildOverlayLines()
{
    if (gallery is null) return [];

    var living = gallery.Stations.Sum(s => s.LivingParticles);

    List<TextElement> lines =
    [
        new("N", "Next station", Color.Gold),
        new("P", "Previous station", Color.Gold),
        new("Home", "Home", Color.Gold),
        new("Tab", gallery.Solo ? "One station at a time" : "Every station", Color.Gold),
        new("L", gallery.LabelDetail switch { 0 => "Labels: the number", 1 => "Labels: the number and the method", _ => "Labels: everything" }, Color.Gold),
        new("Space", "Restart the station", Color.Gold),
        new(""),
        new($"{living:N0} particles alive over {gallery.Stations.Count} stations", Color.LightGreen),
    ];

    if (gallery.CurrentStation is not { } station)
    {
        lines.Add(gallery.Stations.Count == 0
            ? new("No stations in the registry", Color.OrangeRed)
            : new($"Home - {gallery.Stations.Count} stations; N, P or a pad to visit", Color.White));

        return lines;
    }

    lines.Add(new($"Station {station.Number} of {gallery.Stations.Count} - {station.Exhibit.Title}", Color.White));
    lines.AddRange(OverlayText.Wrap($"{station.Exhibit.Method}: {station.Exhibit.Summary}", Color.LightGray));

    if (station.VariationNames.Count > 1)
    {
        lines.Add(new("V", $"Variation {station.Variation + 1} of {station.VariationNames.Count}: {station.VariationNames[station.Variation]}", Color.Cyan));
    }

    // What each emitter holds, so a child emitter that never spawns shows as a zero
    if (station.Particles is { } particles)
    {
        var emitters = particles.ParticleSystem.Emitters;

        lines.Add(new(string.Join("   ", emitters.Select((e, i) => $"{(string.IsNullOrEmpty(e.EmitterName) ? $"emitter {i + 1}" : e.EmitterName)}: {e.LivingParticles}")), Color.LightGray));

    }

    return lines;
}

/*
---example-metadata
slug: particles
title:
  en: Particle Gallery
  cs: Galerie částic
level: Intermediate
category: Rendering
complexity: 3
order: 140
description:
  en: |-
    Thirty-one particle systems on a ring of stations, all built from code: the building blocks one at a
    time - spawners, shapes, initializers, updaters, materials, flipbooks, soft particles - and then
    the showpieces that put them together: a campfire, fireworks with child emitters, a tornado, a
    swarm driven by an updater of our own, lasers, rain that splashes, a portal, a rocket engine. Most
    stations have variations on a key, so what a setting does is a keypress away.
  cs: |-
    Třicet částicových systémů na kruhu stanic, vše postavené z kódu: stavební kameny jeden po
    druhém - spawnery, tvary, inicializátory, updatery, materiály, flipbooky, měkké částice - a pak
    ukázky, které je skládají dohromady: táborák, ohňostroj s dětskými emitory, tornádo, hejno řízené
    vlastním updaterem, lasery, déšť, který stříká, portál, raketový motor. Většina stanic má varianty
    na klávese, takže co které nastavení dělá, je na jedno stisknutí.
concepts:
  - Building a ParticleSystemComponent from code - emitters, spawners, initializers, updaters, shapes, materials
  - Textures, flipbooks and scrolling texture coordinates on particles
  - Curves over a particle's life for size, colour and rotation
  - Force fields, colliders and spawning by distance
  - Child emitters spawned on a parent's death, distance or collision
  - Soft particles against geometry
  - Writing an updater and an initializer of your own
  - A ring of stations from the shared gallery frame in Example.Common, with variations per station
tags:
  - 3D
  - Rendering
  - Particles
  - Emitter
  - Billboard
  - Effects
related:
  - E11_3D_ShapeBatch_Gallery
  - E10_3D_ComputeBoids
  - E02_3D_Material
enabled: true
created: 2024-11-08
---
*/