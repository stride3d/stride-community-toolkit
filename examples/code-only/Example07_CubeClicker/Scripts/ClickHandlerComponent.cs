using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;

namespace Example07_CubeClicker.Scripts;

public class ClickHandlerComponent : AsyncScript
{
    private const string HitEntityName = "Cube";
    private GameUI? _gameUI;
    private CameraComponent? _camera;

    public override async Task Execute()
    {
        _gameUI = Game.Services.GetService<GameUI>();
        _camera = Entity.Scene.Entities.FirstOrDefault(x => x.Get<CameraComponent>() != null)?.Get<CameraComponent>();

        if (_camera is null) return;

        while (Game.IsRunning)
        {
            if (!Input.HasMouse)
            {
                await Script.NextFrame();
                continue;
            }

            if (Input.IsMouseButtonPressed(MouseButton.Left))
                ProcessRaycast(MouseButton.Left);
            else if (Input.IsMouseButtonPressed(MouseButton.Right))
                ProcessRaycast(MouseButton.Right);

            await Script.NextFrame();
        }
    }

    private void ProcessRaycast(MouseButton mouseButton)
    {
        var hitResult = _camera!.RaycastMouse(this);

        if (hitResult.Succeeded && hitResult.Collider.Entity.Name == HitEntityName)
        {
            _gameUI?.HandleClick(mouseButton);

            if (mouseButton == MouseButton.Left)
                AddNewEntity(hitResult.Collider.Entity);
            else if (mouseButton == MouseButton.Right)
                RemoveEntity(hitResult.Collider.Entity);
        }
    }

    private void AddNewEntity(Entity clickedEntity)
    {
        Console.WriteLine("Adding new entity");

        var entity = Game.CreatePrimitive(PrimitiveModelType.Cube, HitEntityName);
        entity.Transform.Position = new Vector3(0, 8, 0);
        //entity.Transform.Scale = Vector3.Zero;
        entity.Scene = clickedEntity.Scene;
        //entity.Add(new CubeSpawner());
    }

    private void RemoveEntity(Entity entity)
    {
        Console.WriteLine("Removing entity");

        entity.Add(new CubeVanisher());
    }
}