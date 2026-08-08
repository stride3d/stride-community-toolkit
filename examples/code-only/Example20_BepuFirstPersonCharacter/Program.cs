using Example20_BepuFirstPersonCharacter;
using Stride.BepuPhysics;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;

// A code-only first-person character controller on Bepu physics:
//  • a CharacterComponent capsule built entirely from code (no Game Studio),
//  • driven by a custom EntityComponent + EntityProcessor pair that auto-registers itself
//    via [DefaultEntityComponentProcessor] - see FirstPersonControllerComponent.cs.

CharacterComponent? character = null;

using var game = new Game();

game.Run(start: Start, update: Update);

void Start(Scene scene)
{
    // SetupBase3D = graphics compositor + camera + directional light. Deliberately NOT
    // SetupBase3DScene(): that would also attach the fly-around debug camera controller, which
    // would fight our first-person controller for the same camera.
    game.SetupBase3D();
    game.Add3DGround();
    game.AddSkybox();

    // A few boxes to walk into and jump onto.
    for (int i = 0; i < 5; i++)
    {
        var box = game.Create3DPrimitive(PrimitiveModelType.Cube);
        box.Transform.Position = new Vector3(3 + i * 2.5f, 0.5f + i * 0.4f, -4);
        box.Scene = scene;
    }

    // The character body: a Bepu CharacterComponent riding a capsule primitive. Passing the
    // component through Bepu3DPhysicsOptions makes Create3DPrimitive attach a matching capsule
    // collider to it.
    character = new CharacterComponent
    {
        Speed = 6f,
        JumpForce = 8f,
        Collider = new CompoundCollider(),
        Gravity = true,
    };
    var body = game.Create3DPrimitive(PrimitiveModelType.Capsule, new Bepu3DPhysicsOptions { Component = character });
    body.Transform.Position = new Vector3(0, 2f, 0);
    body.Scene = scene;

    // The controller lives on the CAMERA entity and drives the body. Adding the component is all
    // it takes - [DefaultEntityComponentProcessor] spins up FirstPersonControllerProcessor
    // automatically.
    var camera = scene.GetCamera();
    camera?.Entity.Add(new FirstPersonControllerComponent { Character = character });
}

void Update(Scene scene, GameTime time)
{
    game.DebugTextSystem.Print("WASD move, Space jump, Shift sprint, V toggle fly/noclip", new(5, 30));
    game.DebugTextSystem.Print("Escape releases the mouse, left-click grabs it again", new(5, 50));

    if (character != null)
        game.DebugTextSystem.Print($"IsGrounded: {character.IsGrounded}", new(5, 70));
}
