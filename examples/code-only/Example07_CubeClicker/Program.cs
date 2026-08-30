using Example07_CubeClicker.Managers;
using Example07_CubeClicker.Scripts;
using NexVYaml;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.Compositing;
using Stride.CommunityToolkit.Rendering.Text;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Engine;
using Stride.Graphics;

// This example demonstrates how to load and save game data. When the game starts,
// it automatically loads the click data and cube positions from the previous session.
// The player interacts with dynamically generated cubes, with the game tracking left
// and right mouse clicks.
// In case of a corrupted Yaml file, navigate to the \bin\Debug\net10.0\data\
// directory and delete the file manually.

using var game = new Game();

// Register all DataContracted Types
NexYamlSerializerRegistry.Init();

game.Run(start: Start);

void Start(Scene rootScene)
{
    game.AddGraphicsCompositor().AddCleanUIStage();
    game.Add3DCamera().Add3DCameraController(displayPosition: DisplayPosition.BottomRight);
    game.AddDirectionalLight();
    game.Add3DGround();
    game.AddSkybox();
    game.AddGroundGizmo(showAxisName: true);

    CreateAndRegisterGameManagerUI(rootScene);
}

void CreateAndRegisterGameManagerUI(Scene rootScene)
{
    var font = game.Content.Load<SpriteFont>("/Stride.Engine/StrideDefaultFont");
    var gameManager = new GameManager(font);
    game.Services.AddService(gameManager);

    var uiEntity = gameManager.CreateUI();
    uiEntity.Add(new ClickHandlerComponent());
    uiEntity.Scene = rootScene;
}
/*
---example-metadata
slug: stride-ui-cube-clicker
title:
  en: Cube Clicker
level: Intermediate
category: UI
complexity: 4
order: 200
description:
  en: |-
    A small clicker game: cubes appear, left and right clicks are counted, and both the score and the
    cube positions are written to disk so the next run picks up where the last one stopped. It is the
    largest UI example in the toolkit and the only one that persists state, split across several files
    by responsibility rather than kept in one Program.cs.
concepts:
  - Building an interactive UI from Grid, TextBlock and Button
  - Saving and loading game state with the NexVYaml serializer
  - "Choosing between SyncScript and AsyncScript for game logic"
  - Separating UI, state and game logic into their own files
  - Restoring a scene from persisted data on startup
tags:
  - 3D
  - UI
  - Stride UI
  - Serialization
  - Persistence
  - Game
  - Scripts
related:
  - Example03_StrideUI_CapsuleAndWindow
  - Example10_StrideUI_DragAndDrop
enabled: true
created: 2023-12-27
---
*/