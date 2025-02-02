using Stride.BepuPhysics;
using Stride.BepuPhysics.Constraints;
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
    game.AddGroundGizmo(showAxisName: true);

    var entity = game.Create3DPrimitive(PrimitiveModelType.Capsule);

    entity.Transform.Position = new Vector3(0, 8, 0);

    entity.Scene = rootScene;

    var entity1 = game.Create3DPrimitive(PrimitiveModelType.Sphere);
    entity1.Transform.Position = new Vector3(-2, 8, -2);
    var body1 = entity1.Get<BodyComponent>();

    var entity2 = game.Create3DPrimitive(PrimitiveModelType.Sphere);
    entity2.Transform.Position = new Vector3(-2.1f, 16, -2.9f);
    var body2 = entity2.Get<BodyComponent>();

    var constrain1 = new DistanceLimitConstraintComponent
    {
        A = body1,
        B = body2,
        MinimumDistance = 0,
        MaximumDistance = 3.0f,
    };

    entity1.Add(constrain1);

    //var constrain2 = new SwingLimitConstraintComponent
    //{
    //    A = body1,
    //    B = body1,
    //    AxisLocalA = Vector3.UnitZ,
    //    AxisLocalB = Vector3.UnitZ,
    //    SpringDampingRatio = 1,
    //    SpringFrequency = 120,
    //    MaximumSwingAngle = MathF.PI * 0.05f,
    //};

    //entity1.Add(constrain2);

    entity1.Scene = rootScene;
    entity2.Scene = rootScene;
});