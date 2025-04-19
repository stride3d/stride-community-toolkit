using Example09_Renderer;
using Stride.BepuPhysics;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.Compositing;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;

// This example demonstrates two different ways of adding custom rendering logic to a Stride game.
// 1. Using a custom SceneRenderer via MyCustomSceneRenderer, which renders text for all entities.
// 2. Using a StartupScript via SpriteBatchRendererScript, which draws specific text for a single entity.
// Both approaches integrate into the Stride rendering pipeline, demonstrating how to extend the default rendering behaviour.

BodyComponent? body = null;
bool impluseApplied = false;

using var game = new Game();

/// <summary>
/// Entry point for the game setup. The game runs with two main customizations:
/// 1. A custom scene renderer is added to display entity debug information using SpriteBatch.
/// 2. A custom script is added to an entity for specific entity-related rendering (e.g., "Hello Stride").
/// </summary>
game.Run(start: Start, update: Update);

void Start(Scene scene)
{
    // Sets up the base 3D scene, including lighting, camera, and default settings.
    game.SetupBase3DScene();

    // Adds the built-in profiler, which provides real-time performance metrics.
    game.AddProfiler();

    // Example 1: Adds a custom scene renderer to render text for all entities in the scene.
    game.AddSceneRenderer(new MyCustomSceneRenderer());
    game.AddSceneRenderer(new EntityTextRenderer());

    // Adds a skybox (a background environment) to the scene.
    game.AddSkybox();

    // Creates a 3D capsule primitive and sets its position in the scene.
    var entity = game.Create3DPrimitive(PrimitiveModelType.Capsule);
    entity.Transform.Position = new Vector3(0, 8, 0);

    // Let's rotate the capsule a tiny bit
    entity.Transform.Rotation = Quaternion.RotationZ(MathUtil.DegreesToRadians(2));

    // Get the body component
    body = entity.Get<BodyComponent>();

    // Example 2: Adds a custom startup script to the entity, which draws specific text
    // (e.g., "Hello Stride 2") using SpriteBatch when the game is running.

    entity.Add(new SpriteBatchRendererScript());

    var textComponent = new EntityTextComponent()
    {
        Text = "Hello Boss",
        FontSize = 13,
        TextColor = Color.Green,
        Offset = new(0, -100)
    };
    entity.Add(textComponent);

    // Assigns the entity to the root scene to ensure it is rendered and updated.
    entity.Scene = scene;

    ////////////////
    ///
    var entity2 = game.Create3DPrimitive(PrimitiveModelType.Cube);
    entity2.Transform.Position = new Vector3(0, 3, 0);

    var textComponent2 = new EntityTextComponent()
    {
        Text = "Hello Stride 2026",
        FontSize = 14,
        TextColor = Color.Red,
    };
    entity2.Add(textComponent2);

    entity2.Scene = scene;
}

void Update(Scene scene, GameTime time)
{
    if (body is null) return;

    if (impluseApplied) return;

    // Let's add some momentum so it rolls after it falls, the rigid body is already added by Create3DPrimitive
    body.ApplyImpulse(new(0, 0, 1f), new());
    body.ApplyAngularImpulse(new(2, 0, 0));

    impluseApplied = true;
}