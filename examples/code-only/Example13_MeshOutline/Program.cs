using Example13_MeshOutline;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Rendering;

// This example demonstrates how to use MeshOutlineRenderFeature to create outlines around 3D primitives.
// The original code is from https://github.com/herocrab/StrideMeshOutlineRenderFeature

using var game = new Game();

// Start the game and initialize the scene
game.Run(start: Start);

void Start(Scene rootScene)
{
    // Set up a basic 3D scene with lighting and camera
    game.SetupBase3DScene();

    // Add a default skybox to the scene for background visuals
    game.AddSkybox();

    // Enable the mesh outline render feature for objects in RenderGroup.Group5
    // The ScaleAdjust parameter controls the thickness of the outline
    game.AddRootRenderFeature(new MeshOutlineRenderFeature()
    {
        RenderGroupMask = RenderGroupMask.Group5,
        ScaleAdjust = 0.03f
    });

    // Create several 3D primitives with different colors and positions, each with an outline effect.
    CreateOutlinedPrimitive(rootScene, PrimitiveModelType.Sphere, Color.Cyan, new Vector3(2f, 0.5f, -2f));
    CreateOutlinedPrimitive(rootScene, PrimitiveModelType.Capsule, Color.Yellow, new Vector3(-1f, 0.5f, -2f));
    CreateOutlinedPrimitive(rootScene, PrimitiveModelType.Sphere, Color.Red, new Vector3(-1f, 0.5f, 4f));
    CreateOutlinedPrimitive(rootScene, PrimitiveModelType.Capsule, Color.Green, new Vector3(2f, 0.5f, 1f));
    CreateOutlinedPrimitive(rootScene, PrimitiveModelType.Sphere, Color.Magenta, new Vector3(-1f, 0.5f, 1f));
}

/// <summary>
/// Creates a 3D primitive entity, applies an outline effect, and adds it to the specified scene.
/// </summary>
/// <param name="rootScene">The scene to which the entity will be added.</param>
/// <param name="modelType">The type of primitive model to create (e.g., Sphere, Capsule).</param>
/// <param name="color">The color of the outline effect.</param>
/// <param name="position">The position of the entity in the scene.</param>
void CreateOutlinedPrimitive(Scene rootScene, PrimitiveModelType modelType, Color4 color, Vector3 position)
{
    // Create a primitive entity of the specified type and assign it to RenderGroup.Group5
    var entity = game.Create3DPrimitive(modelType, options: new()
    {
        RenderGroup = RenderGroup.Group5
    });

    entity.Transform.Position = position;

    // Add the MeshOutlineComponent to enable the outline effect
    // The Intensity parameter controls the brightness of the outline (higher values = brighter)
    entity.Add(new MeshOutlineComponent()
    {
        Enabled = true,
        Color = color,
        Intensity = 100f
    });

    entity.Scene = rootScene;
}