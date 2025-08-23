using Example17_SignalR.Core;
using Example17_SignalR_Shared.Core;
using Stride.BepuPhysics;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Helpers;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Engine;
using Stride.Games;
using Stride.Rendering;

namespace Example17_SignalR.Builders;

public class RobotBuilder(IGame game)
{
    private readonly IGame _game = game;
    private readonly ContactTriggerHandler _triggerScript = new();

    public void CreatePrimitive(int id, EntityType entityType, Scene scene, Material material, AsyncScript removeScript)
    {
        var entity = _game.Create3DPrimitive(PrimitiveModelType.Cube,
            new()
            {
                EntityName = $"Entity {id} - {entityType}",
                Material = material,
                Size = entityType == EntityType.Destroyer ? new(0.5f, 0.5f, 0.5f) : new(1, 1, 1)
            });

        entity.Add(new RobotComponent()
        {
            Type = entityType,
        });
        entity.Add(removeScript);

        entity.Transform.Position = VectorHelper.RandomVector3([-5, 5], [5, 10], [-5, 5]);

        var collider = entity.Get<BodyComponent>();

        if (collider != null)
        {
            collider.ContactEventHandler = _triggerScript;
        }

        entity.Scene = scene;

    }
}