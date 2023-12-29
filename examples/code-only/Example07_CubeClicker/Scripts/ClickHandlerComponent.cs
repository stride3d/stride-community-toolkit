using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;
using Stride.Rendering;

namespace Example07_CubeClicker.Scripts;

public class ClickHandlerComponent : AsyncScript
{
    private const string HitEntityName = "Cube";
    private GameUI? _gameUI;
    private CameraComponent? _camera;
    private readonly Random _random = new();
    private Material? _material;

    public override async Task Execute()
    {
        _gameUI = Game.Services.GetService<GameUI>();
        _camera = Entity.Scene.Entities.FirstOrDefault(x => x.Get<CameraComponent>() != null)?.Get<CameraComponent>();
        _material = Game.CreateMaterial(Color.Yellow, 0.0f, 0.1f);

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
        ChangeColor(clickedEntity);

        Console.WriteLine("Adding new entity");

        var entity = Game.CreatePrimitive(PrimitiveModelType.Cube, HitEntityName);
        entity.Transform.Position = new Vector3(_random.Next(-4, 4), 8, _random.Next(-4, 4));
        entity.Add(new CubeGrower());

        entity.Scene = clickedEntity.Scene;
    }

    private void ChangeColor(Entity clickedEntity)
    {
        var model = clickedEntity.GetComponent<ModelComponent>();

        if (model is null) return;

        if (model.Materials.Count > 0)
        {
            model.Model.Materials[0] = _material;
        }
        else
        {
            model.Model.Materials.Add(_material);
        }
    }

    private void RemoveEntity(Entity entity)
    {
        Console.WriteLine("Removing entity");

        entity.Add(new CubeVanisher());
    }
}