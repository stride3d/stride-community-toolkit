using Example19_Jitter2Physics;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Games;
using Stride.CommunityToolkit.Skyboxes;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Engine;
using Stride.Core.Mathematics;
using Stride.Games;
using Jitter2.Dynamics;
using Jitter2.Collision.Shapes;
using Jitter2.LinearMath;


using var game = new Game();

RigidBody? groundBody = null;
Entity? groundEntity = null;

// store one Entity + one RigidBody per cube
var cubeEntities = new List<Entity>();
var cubeBodies = new List<RigidBody>();

game.Run(start: Start, update: Update);

void Start(Scene rootScene)
{
    game.Window.Title = "Jitter 2 Physics Example - Stride Community Toolkit";

    game.SetupBase3D();
    game.Add3DCameraController();
    game.AddProfiler();
    game.AddSkybox();
    game.SetMaxFPS(100);

    PhysicsWorld.Init();

    const float groundBoxDim = 15.0f;

    // ground Entity setup
    groundEntity = game.Create3DPrimitive(PrimitiveModelType.Plane);
    groundEntity.Transform.Position = new Vector3(-10, -10, -10);
    groundEntity.Transform.Scale = new Vector3(groundBoxDim, 1, groundBoxDim);
    var groundPosition = groundEntity.Transform.Position;
    groundEntity.Scene = rootScene;

    // ground body setup
    groundBody = PhysicsWorld.world.CreateRigidBody();
    groundBody.MotionType = MotionType.Static;
    groundBody.AddShape(new BoxShape(groundBoxDim));

    // Physics box is centered; shift down so its top matches the plane.
    groundBody.Position = new JVector(groundPosition.X,
                                      groundPosition.Y - 0.5f * groundBoxDim,
                                      groundPosition.Z);

    // spawning cubes
    for (int i = 0; i <= 150; i++)
    {
        var ent = game.Create3DPrimitive(PrimitiveModelType.Cube, new()
        {
            Material = game.CreateMaterial(Color.Red)
        });
        var cubePosition = new Vector3(-10, 10 + i * 2, -10);

        ent.Transform.Position = cubePosition;
        ent.Transform.Scale = new Vector3(0.5f);
        ent.Scene = rootScene;

        //cube body setup
        var body = PhysicsWorld.world.CreateRigidBody();
        body.AddShape(new BoxShape(0.5f));
        body.SetMassInertia(1f);
        body.Position = new JVector(cubePosition.X, cubePosition.Y, cubePosition.Z);

        cubeEntities.Add(ent);
        cubeBodies.Add(body);
    }
}

void Update(Scene scene, GameTime time)
{
    PhysicsWorld.Update();

    if (cubeBodies.Count == 0) return;

    // sync every cube entity to its corresponding physics body
    for (int i = 0; i < cubeBodies.Count; i++)
    {
        var body = cubeBodies[i];
        var entity = cubeEntities[i];

        var bodyPos = body.Position;
        var bodyOrientation = body.Orientation;

        entity.Transform.Position = new Vector3(bodyPos.X, bodyPos.Y, bodyPos.Z);
        entity.Transform.Rotation = new Quaternion(bodyOrientation.X, bodyOrientation.Y, bodyOrientation.Z, bodyOrientation.W);
    }
}