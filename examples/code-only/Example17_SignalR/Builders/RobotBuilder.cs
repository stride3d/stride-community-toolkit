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

/// <summary>
/// Factory for creating robot entities with physics and metadata components.
/// </summary>
public class RobotBuilder(IGame game)
{
    private readonly IGame _game = game;
    private readonly ContactTriggerHandler _triggerScript = new();

    /// <summary>
    /// Creates a robot entity using provided attributes and adds it to the given scene.
    /// </summary>
    /// <param name="id">A local identifier used for the entity name only.</param>
    /// <param name="entityType">Logical robot type.</param>
    /// <param name="scene">Target scene to attach the entity to.</param>
    /// <param name="material">Material to apply to the model.</param>
    public void CreateRobot(int id, EntityType entityType, Scene scene, Material material)
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

        entity.Transform.Position = VectorHelper.RandomVector3([-5, 5], [5, 10], [-5, 5]);

        var collider = entity.Get<BodyComponent>();

        if (collider != null)
        {
            collider.ContactEventHandler = _triggerScript;
        }

        entity.Scene = scene;
    }
}