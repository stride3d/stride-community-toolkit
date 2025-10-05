using Example17_SignalR.Scripts;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace Example17_SignalR.Builders;

/// <summary>
/// Composes the scene for Example17 (SignalR) and attaches game scripts.
/// </summary>
public static class SceneBuilder
{
    public static void Build(Game game, Scene rootScene)
    {
        // Base scene setup
        game.SetupBase3DScene();
        game.AddSkybox();
        game.AddProfiler();

        // Script entities
        new Entity("HubEventPump") { new HubEventPumpScript() }.AttachTo(rootScene);
        new Entity("MessageHud") { new MessageHudScript() }.AttachTo(rootScene);
        new Entity("RobotSpawner") { new RobotSpawnScript() }.AttachTo(rootScene);
        new Entity("RemoveRelay") { new RemoveRelayScript() }.AttachTo(rootScene);
        new Entity("RemoveProcessor") { new RemovalQueueProcessorScript() }.AttachTo(rootScene);

        // Demo primitive
        var entity = game.Create3DPrimitive(PrimitiveModelType.Capsule);
        entity.Transform.Position = new Vector3(0, 8, 0);
        entity.Scene = rootScene;
    }

    private static void AttachTo(this Entity entity, Scene scene)
    {
        entity.Scene = scene;
    }
}