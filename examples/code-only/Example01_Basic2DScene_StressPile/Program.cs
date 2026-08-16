using Stride.BepuPhysics;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Games;
using Stride.CommunityToolkit.Rendering.Compositing;
using Stride.CommunityToolkit.Rendering.Instancing;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Rendering;

Vector3 wallSize = new(1, 50, 1);
float wallWidth = 100;
int itemCount = 30000;


BufferedEntityInstancing? bufferedInstancing = null;
Model? sharedModel = null;
PrimitiveModelType modelType = PrimitiveModelType.TriangularPrism;

using var game = new Game();

game.Run(start: Start);

bufferedInstancing?.Dispose();

void Start(Scene rootScene)
{
    // SetupBase3D() unrolled, so the camera and the light can be aimed for a head-on view of the XY plane
    game.AddGraphicsCompositor().AddCleanUIStage();
    game.Add3DCamera(initialPosition: new Vector3(0, 0, 80), initialRotation: Vector3.Zero);
    game.AddProfiler();

    // The default aim shines toward +Z, which leaves the faces turned towards the camera unlit
    var light = game.AddDirectionalLight();
    light.Transform.Rotation = Quaternion.RotationX(MathUtil.DegreesToRadians(-30)) *
                               Quaternion.RotationY(MathUtil.DegreesToRadians(-30));

    game.Add3DCameraController();
    game.AddSkybox();

    // Options typed explicitly: with both overloads in scope, an argument-less call is ambiguous
    var entity = game.Create3DPrimitive(PrimitiveModelType.Capsule, new Bepu3DPhysicsOptions());
    entity.Transform.Position = new Vector3(0, 8, 0);
    entity.Scene = rootScene;


    CreateWall(rootScene, new Vector3(-wallWidth / 2, 0, 0), wallSize);
    CreateWall(rootScene, new Vector3(wallWidth / 2, 0, 0), wallSize);
    CreateWall(rootScene, new Vector3(0, -25, 0), new Vector3(wallWidth, 1, 1));

    SetupInstancing(rootScene);

    GenerateItems(rootScene, itemCount, modelType);
}

void CreateWall(Scene rootScene, Vector3 position, Vector3 size)
{
    var leftWall = game.Create3DPrimitive(PrimitiveModelType.Cube, new()
    {
        Size = size,
        Material = game.CreateMaterial(Color.LightGray),
        Component = new StaticComponent { Collider = new CompoundCollider() }
    });
    leftWall.Transform.Position = position;
    leftWall.Scene = rootScene;
}

void SetupInstancing(Scene rootScene)
{
    // Without this nothing instanced is drawn, and nothing warns you: the code-built compositor
    // wires up transform, skinning, material and lighting, but not instancing
    game.AddInstancingSupport();

    sharedModel = CreateSharedModel(rootScene, modelType);

    bufferedInstancing = new BufferedEntityInstancing(new BepuEntityInstancing());

    CreateMaster(rootScene, sharedModel, bufferedInstancing, "BufferedMaster");

    game.AddInstancingBufferUpload(bufferedInstancing);
}

Model CreateSharedModel(Scene rootScene, PrimitiveModelType modelType)
{
    // Primitive3DEntityOptions, explicitly typed, selects the overload that does NOT attach a body.
    // Passing new() here would pick the Bepu one instead and leave a dynamic body falling forever.
    var prototype = game.Create3DPrimitive(modelType, new Primitive3DEntityOptions());

    // Parked out of sight; it exists only to own the Model
    prototype.Transform.Position = new Vector3(0, -100, 0);
    prototype.Scene = rootScene;

    return prototype.Get<ModelComponent>().Model;
}

InstancingComponent CreateMaster(Scene rootScene, Model model, IInstancing instancingType, string name)
{
    var entity = new Entity(name)
    {
        new ModelComponent(model),
        new InstancingComponent { Type = instancingType }
    };

    entity.Scene = rootScene;

    return entity.Get<InstancingComponent>();
}

void GenerateItems(Scene rootScene, int count, PrimitiveModelType modelType)
{
    var rows = count / wallWidth;
    var space = 1.2f;
    var verticalOffset = 1.2f;

    for (int i = 0; i < rows; i++)
    {
        for (int j = 0; j < (wallWidth - 30); j++)
        {
            var entity = new Entity("InstancedItem") { new ModelComponent(sharedModel) };

            entity.AddBepu3DPhysics(modelType, new Bepu3DPhysicsOptions
            {
                Component = new Body2DComponent() { Collider = new CompoundCollider() }
            });

            //var entity = game.Create3DPrimitive(modelType, new()
            //{
            //    Component = new Body2DComponent() { Collider = new CompoundCollider() }
            //});
            // The master draws every instance. Leaving each entity its own ModelComponent would draw the
            // whole pile twice - once per entity, once instanced - which is slower than not instancing at
            // all. Create3DPrimitive always adds one, so it has to come back off; what is kept is the
            // collider it derived from the primitive type and size.
            entity.Remove<ModelComponent>();
            var position = new Vector3((j - (wallWidth - 30) / 2) * space, 5 + i * verticalOffset, 0);
            //Console.WriteLine(position);
            entity.Transform.Position = position;
            bufferedInstancing?.AddInstance(entity);
            entity.Scene = rootScene;
        }
    }

    //for (int i = 0; i < count; i++)
    //{
    //    var entity = new Entity("InstancedItem") { new ModelComponent(sharedModel) };

    //    entity.AddBepu3DPhysics(modelType, new Bepu3DPhysicsOptions
    //    {
    //        Component = new Body2DComponent() { Collider = new CompoundCollider() }
    //    });

    //    //var entity = game.Create3DPrimitive(modelType, new()
    //    //{
    //    //    Component = new Body2DComponent() { Collider = new CompoundCollider() }
    //    //});

    //    // The master draws every instance. Leaving each entity its own ModelComponent would draw the
    //    // whole pile twice - once per entity, once instanced - which is slower than not instancing at
    //    // all. Create3DPrimitive always adds one, so it has to come back off; what is kept is the
    //    // collider it derived from the primitive type and size.
    //    entity.Remove<ModelComponent>();

    //    entity.Transform.Position = Stride.CommunityToolkit.Helpers.VectorHelper.RandomVector3(xRange: [-40, 40], yRange: [20, 400], zRange: [0, 0]);

    //    bufferedInstancing?.AddInstance(entity);

    //    entity.Scene = rootScene;
    //}
}