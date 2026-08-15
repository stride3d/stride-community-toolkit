using Stride.BepuPhysics;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.Compositing;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;

using var game = new Game();

game.Run(start: Start);

void Start(Scene rootScene)
{
    // SetupBase3D() unrolled, so the camera and the light can be aimed for a head-on view of the XY plane
    game.AddGraphicsCompositor().AddCleanUIStage();
    game.Add3DCamera(initialPosition: new Vector3(0, 0, 80), initialRotation: Vector3.Zero);

    // The default aim shines toward +Z, which leaves the faces turned towards the camera unlit
    var light = game.AddDirectionalLight();
    light.Transform.Rotation = Quaternion.RotationX(MathUtil.DegreesToRadians(-30)) *
                               Quaternion.RotationY(MathUtil.DegreesToRadians(-30));

    game.Add3DCameraController();
    game.AddSkybox();

    var entity = game.Create3DPrimitive(PrimitiveModelType.Capsule);
    entity.Transform.Position = new Vector3(0, 8, 0);
    entity.Scene = rootScene;

    CreateWall(rootScene, new Vector3(-25, 0, 0));
    CreateWall(rootScene, new Vector3(25, 0, 0));
}

void CreateWall(Scene rootScene, Vector3 position)
{
    var leftWall = game.Create3DPrimitive(PrimitiveModelType.Cube, new()
    {
        Size = new Vector3(1, 50, 1),
        Material = game.CreateMaterial(Color.LightGray),
        Component = new StaticComponent { Collider = new CompoundCollider() }
    });
    leftWall.Transform.Position = position;
    leftWall.Scene = rootScene;
}