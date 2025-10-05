using Example17_SignalR.Builders;
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