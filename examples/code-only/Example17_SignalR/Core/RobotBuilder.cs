using Example17_SignalR.Scripts;
using Example17_SignalR_Shared.Core;
using Example17_SignalR_Shared.Dtos;
using Stride.BepuPhysics;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Helpers;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Engine;
using Stride.Games;

namespace Example17_SignalR.Core;

public class RobotBuilder
{
    private readonly IGame _game;
    private readonly MaterialManager _materialManager;
    private readonly ContactTriggerHandler _triggerScript = new();

    public RobotBuilder(IGame game, MaterialManager materialManager)
    {
        _game = game;
        _materialManager = materialManager;
    }

    public void CreatePrimitives(CountDto countDto, Scene scene)
    {
        for (var i = 0; i < countDto.Count; i++)
        {
            var entity = _game.Create3DPrimitive(PrimitiveModelType.Cube,
                new()
                {
                    EntityName = $"Entity - {countDto.Type}",
                    Material = _materialManager.GetMaterial(countDto.Type),
                    Size = countDto.Type == EntityType.Destroyer ? new(0.5f, 0.5f, 0.5f) : new(1, 1, 1)
                });

            entity.Add(new RobotComponent()
            {
                Type = countDto.Type,
            });
            entity.Add(new RemoveEntityScript());

            entity.Transform.Position = VectorHelper.RandomVector3([-5, 5], [5, 10], [-5, 5]);

            var collider = entity.Get<BodyComponent>();

            if (collider != null)
            {
                collider.ContactEventHandler = _triggerScript;
                //collider.ContactEventHandler = new TriggerScript();
            }

            entity.Scene = scene;
        }
    }
}