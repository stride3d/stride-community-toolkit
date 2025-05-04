using Example17_SignalR_Shared.Core;
using Example17_SignalR_Shared.Dtos;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Helpers;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Engine;
using Stride.Games;

namespace Example17_SignalR.Core;

public class PrimitiveBuilder
{
    private readonly IGame _game;
    private readonly MaterialManager _materialManager;

    public PrimitiveBuilder(IGame game, MaterialManager materialManager)
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

            entity.Transform.Position = VectorHelper.RandomVector3([-5, 5], [5, 10], [-5, 5]);
            entity.Scene = scene;
        }
    }
}