using CubicleCalamity;
using Stride.CommunityToolkit.Engine;
using Stride.Engine;

using var game = new Game();

var cubeStacker = new CubeStacker(game);

game.Run(start: cubeStacker.Start, update: cubeStacker.Update);