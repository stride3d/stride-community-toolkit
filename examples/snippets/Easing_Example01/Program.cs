using Stride.BepuPhysics;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Mathematics;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Input;
using Stride.Rendering.Materials;

// Time elapsed since the start of the animation
var elapsed = TimeSpan.Zero;

// Duration of the animation (2 seconds)
var duration = TimeSpan.FromSeconds(2);

// Target position (where the object will ease to)
var bottom = new Vector3(0, 2, 0);

// Starting position of the object
var startPosition = new Vector3(0, 8, 0);

// 3D entity to be animated
var entity = new Entity();

// Color to interpolate to, initilized to white
Color color = Color.White;

// Initialize a new game instance
using var game = new Game();

// Run the game, specifying both the Start and Update methods
game.Run(start: Start, update: Update);

// Setup and initialize the scene
void Start(Scene scene)
{
    // Set up a base 3D scene with default lighting and camera
    game.SetupBase3DScene();

    // Create a 3D sphere primitive and assign the material to it
    entity = game.Create3DPrimitive(PrimitiveModelType.Sphere, new()
    {
        Material = game.CreateMaterial(Color.White),
        Component = new BodyComponent()
        {
            Collider = new CompoundCollider(),
            Kinematic = true
        }
    });

    // Add the sphere entity to the root scene
    entity.Scene = scene;

    var random = new Random();

    // Generate a random color to interpolate to
    color = random.NextColor();
}

// Update the scene every frame (for animations and input handling)
void Update(Scene scene, GameTime time)
{
    game.DebugTextSystem.Print("Press Space to reset", new Int2(5, 10));

    // Calculate the progress of the animation as a ratio between 0 and 1
    var progress = (float)(elapsed.TotalSeconds / duration.TotalSeconds);

    if (progress > 1.0f)
    {
        progress = 1.0f;
    }

    // Interpolate the position of the object using a quintic easing function
    var position = MathUtilEx.Interpolate(startPosition, bottom, progress, EasingFunction.QuinticEaseOut);

    Console.WriteLine(position);

    // Apply the new position to the entity
    entity.Transform.Position = position;

    // Interpolate the color using a linear easing function
    var diffuse = MathUtilEx.Interpolate(Color.White, color, progress, EasingFunction.Linear);

    // Apply the interpolated color to the material
    entity.Get<ModelComponent>().SetMaterialParameter(MaterialKeys.DiffuseValue, diffuse);

    // Update the elapsed time with the time since the last frame
    elapsed += time.Elapsed;

    // Reset the animation when the spacebar is pressed
    if (game.Input.IsKeyPressed(Keys.Space))
    {
        elapsed = TimeSpan.Zero;
    }
}