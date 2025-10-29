using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;

using var game = new Game();

game.Run(start: (Scene rootScene) =>
{
    game.SetupBase3DScene();
    game.AddSkybox();

    var entity = game.Create3DPrimitive(PrimitiveModelType.Capsule);

    entity.Transform.Position = new Vector3(0, 8, 0);

    entity.Scene = rootScene;
});

/*
EXAMPLE_META_START
Title: Basic3D Scene (Capsule)
Category: Shapes
Level: Getting Started
Complexity:1
Order:1
Description: |
 Create a minimal 3D scene using toolkit helpers and place a single capsule primitive.
 Demonstrates entity creation, basic positioning, and attaching the entity to the scene.
Concepts:
 - Creating a 3D primitive (Capsule)
 - Positioning an entity with Transform.Position
 - Adding entities to a Scene (rootScene)
 - Using helpers: SetupBase3DScene and AddSkybox
Related:
 - Example02_GiveMeACube
 - Example01_Basic3DScene_Primitives
 - Example01_Material
Tags:
 -3D
 - Shapes
 - Primitive
 - Capsule
 - Scene Setup
 - Skybox
 - Transform
 - Position
 - Getting Started
EXAMPLE_META_END
*/