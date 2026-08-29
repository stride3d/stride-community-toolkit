using Example17_SignalR;
using Example17_SignalR.Core;
using Example17_SignalR.Services;
using Example17_SignalR.SignalR;
using Stride.CommunityToolkit.Engine;
using Stride.Core.Diagnostics;
using Stride.Engine;

using var game = new Game();

// Create a Stride logger to surface .NET logs into Stride's console
var strideLogger = GlobalLogger.GetLogger("SignalR");
var loggerAdapter = new StrideLoggerAdapter<SignalRHubClient>(strideLogger);

var hubUrl = $"{GameSettings.HubBaseUrl}/{GameSettings.HubUrl}";

game.Services.AddService(new ScreenService(hubUrl, loggerAdapter));

game.Run(start: (Scene rootScene) =>
{
    SceneBuilder.Build(game, rootScene);
});
/*
---example-metadata
slug: stride-signalr
title:
  en: Stride + SignalR
level: Advanced
category: Networking
complexity: 4
order: 160
description:
  en: |-
    A Stride game and a Blazor web page talking to each other in real time over a SignalR hub, so a
    button in a browser can spawn an entity in the running game and the game can report back. Two
    processes and a server make this the most involved example in the toolkit to run: start the Blazor
    app first so the hub exists, then start the game, which connects to it.
concepts:
  - Connecting a Stride app to a SignalR hub
  - Registering handlers for incoming messages
  - Sending events from the game to a web client
  - Driving scene changes from a remote command
  - "Marshalling hub callbacks onto the game loop before touching the scene"
  - Sharing message contracts through a common project
tags:
  - 3D
  - Networking
  - SignalR
  - Blazor
  - Real Time
  - Multi Project
related:
  - Example17_SignalR_Blazor
media: stride-game-engine-example17-signalr.webp
tocName: Stride + SignalR
screenshot: false
enabled: true
created: 2025-05-04
---
*/