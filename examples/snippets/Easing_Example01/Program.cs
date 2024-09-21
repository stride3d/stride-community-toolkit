using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Mathematics;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Input;

var elapsed = TimeSpan.Zero;
var duration = TimeSpan.FromSeconds(4);
var bottom = new Vector3(0, 2, 0);
var startPosition = new Vector3(0, 8, 0);

var entity = new Entity();

using var game = new Game();

game.Run(start: Start, update: Update);

void Start(Scene scene)
{
    // Set up a base 3D scene with default lighting and camera
    game.SetupBase3DScene();

    // Create a 3D cube primitive and assign the material to it
    entity = game.Create3DPrimitive(PrimitiveModelType.Sphere);

    // Add the cube to the root scene
    entity.Scene = scene;
}

void Update(Scene scene, GameTime time)
{
    var progress = (float)(elapsed.TotalSeconds / duration.TotalSeconds);

    var position = MathUtilEx.Interpolate(startPosition, bottom, progress, EasingFunction.QuinticEaseOut);

    Console.WriteLine(position);

    entity.Transform.Position = position;

    elapsed += time.Elapsed;

    // Reset
    if (game.Input.IsKeyPressed(Keys.Space))
    {
        elapsed = TimeSpan.Zero;
    }
}