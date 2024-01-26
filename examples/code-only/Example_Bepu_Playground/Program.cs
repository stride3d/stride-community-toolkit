using Stride.BepuPhysics;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Input;
using Stride.Rendering;

var shift = new Vector3(0, 0, 0);
var boxSize = new Vector3(0.2f, 0.2f, 0.04f);
var groundSize = new Vector3(20, 0.01f, 20);
var box2DSize = new Vector2(0.2f, 0.2f);
var rectangleSize = new Vector3(0.2f, 0.3f, 0);
int cubes = 0;
int debugX = 5;
int debugY = 30;
var bgImage = "JumpyJetBackground.jpg";
const string ShapeName = "Shape";
const float Depth = 0.04f;
Model? model = null;

using var game = new Game();

game.Run(start: (Action<Scene>?)((Scene rootScene) =>
{
    game.SetupBase3DSceneWithBepu();
    game.AddProfiler();

    game.Window.AllowUserResizing = true;
    game.Window.Title = "2D Example";

    //game.AddAllDirectionLighting(20);

    //game.ShowColliders();

    //var simulation = game.SceneSystem.SceneInstance.GetProcessor<PhysicsProcessor>()?.Simulation;
    //simulation.FixedTimeStep = 1f / 90;

    var entity = game.CreatePrimitive(PrimitiveModelType.Capsule);
    entity.Transform.Position = new Vector3(0, 20, 0);
    entity.Scene = rootScene;

    //var groundProceduralModel = Procedural3DModelBuilder.Build(PrimitiveModelType.Cube, groundSize);
    //var groundModel = groundProceduralModel.Generate(game.Services);

    //var ground = new Entity("Ground")
    //{
    //    new ModelComponent(groundModel) { RenderGroup = RenderGroup.Group0 },
    //    new StaticComponent() {
    //        Collider = new CompoundCollider {
    //            Colliders = { new BoxCollider() { Size = groundSize } }
    //        }
    //    }
    //};

    //ground.Transform.Position = new Vector3(0, 2, 0) + shift;
    //ground.AddGizmo(game.GraphicsDevice, showAxisName: true);
    //ground.Scene = rootScene;

    //var cubeProceduralModel = Procedural2DModelBuilder.Build(Primitive2DModelType.Circle, box2DSize, Depth);
    var cubeProceduralModel = Procedural2DModelBuilder.Build(Primitive2DModelType.Square, box2DSize, Depth);
    model = cubeProceduralModel.Generate(game.Services);

    GenerateCubes(rootScene, shift, model);

}), update: Update);

void Update(Scene scene, GameTime time)
{
    if (game.Input.IsKeyDown(Keys.Space))
    {
        GenerateCubes(scene, shift, model);

        SetCubeCount(scene);
    }
    else if (game.Input.IsKeyReleased(Keys.X))
    {
        foreach (var entity in scene.Entities.Where(w => w.Name == "BepuCube").ToList())
        {
            entity.Remove();
        }

        SetCubeCount(scene);
    }

    RenderNavigation();
}

void GenerateCubes(Scene rootScene, Vector3 shift, Model model)
{
    var rotationLock = new Vector3(0, 0, 1);

    for (int i = -5; i < 5; i++)
    {
        var entity2 = new Entity("BepuCube") {
            new ModelComponent(model) { RenderGroup = RenderGroup.Group0 }
        };
        entity2.Transform.Position = new Vector3(i * 2, Random.Shared.Next(10, 30), 0) + shift;
        //entity2.Transform.Rotation = Quaternion.RotationAxis(Vector3.UnitX, MathUtil.DegreesToRadians(90));

        var component = new Body2DComponent()
        {
            Collider = new CompoundCollider() { Colliders = { new BoxCollider() { Size = boxSize, Mass = 100 } } },
            //Collider = new CompoundCollider()
            //{
            //    Colliders = { new CylinderCollider() {
            //        Radius = boxSize.X,
            //        Length = Depth,
            //        Mass = 1,
            //        //RotationLocal = Quaternion.RotationAxis(Vector3.UnitX, MathUtil.DegreesToRadians(90)),
            //        PositionLocal = new Vector3(box2DSize.X, box2DSize.X, 0)

            //        }
            //    }
            //},
            //SimulationIndex = 2,
            //SpringFrequency = 30,
            //SpringDampingRatio = 3.0f,
            //FrictionCoefficient = 1,
            //MaximumRecoveryVelocity = 1000,
            //Kinematic = false,
            //IgnoreGlobalGravity = false,
            //SleepThreshold = 0.01f,
            //MinimumTimestepCountUnderThreshold = 32,
            //InterpolationMode = InterpolationMode.None,
            //ContinuousDetectionMode = ContinuousDetectionMode.Discrete,
        };

        //var inverseInternalTensor = new BepuUtilities.Symmetric3x3
        //{
        //    XX = 0,
        //    YX = 0,
        //    YY = 0,
        //    ZX = 0,
        //    ZY = 0,
        //    ZZ = 0
        //};

        //inverseInternalTensor.XX *= rotationLock.X;
        //inverseInternalTensor.YX *= rotationLock.X * rotationLock.Y;
        //inverseInternalTensor.ZX *= rotationLock.Z * rotationLock.X;
        //inverseInternalTensor.YY *= rotationLock.Y;
        //inverseInternalTensor.ZY *= rotationLock.Z * rotationLock.Y;
        //inverseInternalTensor.ZZ *= rotationLock.Z;

        //component.Colliders.Add(new BoxCollider() { Size = boxSize });

        entity2.Add(component);
        //entity2.Add(new LinearAxisLimitConstraintComponent()
        //{
        //    Enabled = true,
        //    TargetVelocity = new Vector3(0, 10, 0),
        //    MotorDamping = 50,
        //    MotorMaximumForce = 10000000,
        //});
        entity2.Scene = rootScene;

        //component.AngularVelocity = new Vector3(Random.Shared.Next(-10, 10), Random.Shared.Next(-10, 10), Random.Shared.Next(-10, 10));

        //component.BodyInertia = new BepuPhysics.BodyInertia()
        //{
        //    InverseInertiaTensor = component.BodyInertia.InverseInertiaTensor
        //};

        //var inverseInternalTensor = component.BodyInertia.InverseInertiaTensor;
        //inverseInternalTensor.XX *= rotationLock.X;
        //inverseInternalTensor.YX *= rotationLock.X * rotationLock.Y;
        //inverseInternalTensor.ZX *= rotationLock.Z * rotationLock.X;
        //inverseInternalTensor.YY *= rotationLock.Y;
        //inverseInternalTensor.ZY *= rotationLock.Z * rotationLock.Y;
        //inverseInternalTensor.ZZ *= rotationLock.Z;

        //component.BodyInertia = new BepuPhysics.BodyInertia()
        //{
        //    InverseInertiaTensor = new BepuUtilities.Symmetric3x3
        //    {
        //        XX = 0.0001f,
        //        YX = 0,
        //        YY = 0.0001f,
        //        ZX = 0,
        //        ZY = 0,
        //        ZZ = 0
        //    }
        //};

        component.LinearVelocity *= new Vector3(1, 1, 0);
        //component.Position *= new Vector3(1, 1, 0);
        component.AngularVelocity *= new Vector3(0, 0, 1);
    }
}

void SetCubeCount(Scene scene) => cubes = scene.Entities.Where(w => w.Name == "BepuCube").Count();

void RenderNavigation()
{
    game.DebugTextSystem.Print($"Cubes: {cubes}", new Int2(x: debugX, y: debugY));
    game.DebugTextSystem.Print($"X - Delete all cubes and shapes", new Int2(x: debugX, y: debugY + 30), Color.Red);
    game.DebugTextSystem.Print($"Space - generate 3D cubes", new Int2(x: debugX, y: debugY + 60));
}