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
    private Vector3 _defaultCubePosition = new(0, 8, 0);
    private GameUIManager? _gameUI;
    private CameraComponent? _camera;
    private readonly Random _random = new();
    private Material? _material;

    public override async Task Execute()
    {
        _camera = Entity.Scene.Entities.FirstOrDefault(x => x.Get<CameraComponent>() != null)?.Get<CameraComponent>();

        if (_camera is null) return;

        _material = Game.CreateMaterial(Color.Yellow, 0.0f, 0.1f);
        _gameUI = Game.Services.GetService<GameUIManager>();
        //_cubeCollector = Entity.GetComponent<CubeCollector>();

        await _gameUI.LoadClickDataAsync();

        var loadedCubes = await _gameUI.LoadCubeDataAsync();

        if (loadedCubes.Count == 0)
        {
            // Create initial cube
            CreateCube();
        }
        else
        {
            foreach (var vector in loadedCubes)
            {
                CreateCube(vector);
            }
        }

        while (Game.IsRunning)
        {
            if (!Input.HasMouse)
            {
                await Script.NextFrame();
                continue;
            }

            if (Input.IsMouseButtonPressed(MouseButton.Left))
            {
                ProcessRaycast(MouseButton.Left);
            }
            else if (Input.IsMouseButtonPressed(MouseButton.Right))
            {
                ProcessRaycast(MouseButton.Right);
            }

            await Script.NextFrame();
        }
    }

    private void ProcessRaycast(MouseButton mouseButton)
    {
        var hitResult = _camera!.RaycastMouse(this);

        if (hitResult.Succeeded && hitResult.Collider.Entity.Name == HitEntityName)
        {
            _gameUI?.HandleClick(mouseButton, GetCubeEntityPositions());

            if (mouseButton == MouseButton.Left)
            {
                AddNewEntity(hitResult.Collider.Entity);
            }
            else if (mouseButton == MouseButton.Right)
            {
                RemoveEntity(hitResult.Collider.Entity);
            }
        }

        List<Vector3> GetCubeEntityPositions()
            => Entity.Scene.Entities
            .Where(w => w.Name == HitEntityName && w.Get<CubeVanisher>() is null)
            .Select(s => s.Transform.Position)
            .ToList();
    }

    private void AddNewEntity(Entity clickedEntity)
    {
        ChangeColor(clickedEntity);

        Console.WriteLine("Adding new entity");

        CreateCube(new Vector3(_random.Next(-4, 4), 8, _random.Next(-4, 4)));

        //var entity = Game.CreatePrimitive(PrimitiveModelType.Cube, HitEntityName);
        //entity.Transform.Position = new Vector3(_random.Next(-4, 4), 8, _random.Next(-4, 4));
        //entity.Add(new CubeGrower());

        //_cubeCollector?.Add(entity);

        //entity.Scene = clickedEntity.Scene;
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

    void CreateCube(Vector3? position = null)
    {
        var entity = Game.CreatePrimitive(PrimitiveModelType.Cube, HitEntityName);
        entity.Transform.Position = position ?? _defaultCubePosition;
        entity.Add(new CubeGrower());
        entity.Scene = Entity.Scene;
    }
}