using Stride.BepuPhysics;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Helpers;
using Stride.CommunityToolkit.Rendering.Compositing;
using Stride.CommunityToolkit.Rendering.Instancing;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Rendering;

Vector3 wallSize = new(1, 50, 1);
float wallWidth = 70;

BufferedEntityInstancing? bufferedInstancing = null;
Model? sharedModel = null;
PrimitiveModelType modelType = PrimitiveModelType.Cube;

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

    var entity = game.Create3DPrimitive(PrimitiveModelType.Capsule);
    entity.Transform.Position = new Vector3(0, 8, 0);
    entity.Scene = rootScene;


    CreateWall(rootScene, new Vector3(-wallWidth / 2, 0, 0), wallSize);
    CreateWall(rootScene, new Vector3(wallWidth / 2, 0, 0), wallSize);
    CreateWall(rootScene, new Vector3(0, -25, 0), new Vector3(wallWidth, 1, 1));

    SetupInstancing(rootScene);

    GenerateItems(rootScene, 5000, modelType);
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
    sharedModel = CreateSharedModel(rootScene, modelType);

    bufferedInstancing = new BufferedEntityInstancing(new BepuEntityInstancing());

    CreateMaster(rootScene, sharedModel, bufferedInstancing, "BufferedMaster");

    game.AddInstancingBufferUpload(bufferedInstancing);
}

Model CreateSharedModel(Scene rootScene, PrimitiveModelType modelType)
{
    var prototype = game.Create3DPrimitive(modelType);

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
    for (int i = 0; i < count; i++)
    {
        var entity = game.Create3DPrimitive(modelType, new()
        {
            Component = new Body2DComponent() { Collider = new CompoundCollider() }
        });

        entity.Transform.Position = VectorHelper.RandomVector3(xRange: [-20, 20], yRange: [20, 200], zRange: [0, 0]);
        entity.Scene = rootScene;

        bufferedInstancing?.AddInstance(entity);
    }
}