using Example07_CubeClicker.Managers;
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
    private GameManager? _gameManager;
    private CameraComponent? _camera;
    private readonly Random _random = new();
    private Material? _material;

    public override async Task Execute()
    {
        _camera = Entity.Scene.Entities.FirstOrDefault(x => x.Get<CameraComponent>() != null)?.Get<CameraComponent>();
        _gameManager = Game.Services.GetService<GameManager>();

        if (_camera is null || _gameManager is null)
        {
            // notify user about missing components or services
            return;
        }

        _material = Game.CreateMaterial(Color.Yellow, 0.0f, 0.1f);

        await _gameManager.LoadClickDataAsync();
        await LoadCubeDataAsync();

        while (Game.IsRunning)
        {
            if (!Input.HasMouse)
            {
                await Script.NextFrame();
                continue;
            }

            if (_gameManager.ReloadCubes)
            {
                await LoadCubeDataAsync();
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

    private async Task LoadCubeDataAsync()
    {
        _gameManager!.ReloadCubes = false;

        foreach (var cube in GetCubeEntities())
        {
            cube.Remove();
        }

        var loadedCubes = await _gameManager.LoadCubeDataAsync();

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
    }

    private void ProcessRaycast(MouseButton mouseButton)
    {
        var hitResult = _camera!.RaycastMouse(this);

        if (hitResult.Succeeded && hitResult.Collider.Entity.Name == HitEntityName)
        {
            if (mouseButton == MouseButton.Left)
            {
                AddNewEntity(hitResult.Collider.Entity);
            }
            else if (mouseButton == MouseButton.Right)
            {
                RemoveEntity(hitResult.Collider.Entity);
            }

            _gameManager?.HandleClick(mouseButton, GetCubeEntities().ConvertAll(s => s.Transform.Position));
        }

    }

    private List<Entity> GetCubeEntities()
        => Entity.Scene.Entities
        .Where(w => w.Name == HitEntityName && w.Get<CubeVanisher>() is null)
        .ToList();

    private void AddNewEntity(Entity clickedEntity)
    {
        ChangeColor(clickedEntity);

        Console.WriteLine("Adding new entity");

        CreateCube(new Vector3(_random.Next(-4, 4), 8, _random.Next(-4, 4)));
    }

    private static void RemoveEntity(Entity entity)
    {
        Console.WriteLine("Removing entity");

        entity.Add(new CubeVanisher());
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

    private void CreateCube(Vector3? position = null)
    {
        var entity = Game.CreatePrimitive(PrimitiveModelType.Cube, HitEntityName);
        entity.Transform.Position = position ?? _defaultCubePosition;
        entity.Add(new CubeGrower());
        entity.Scene = Entity.Scene;
    }
}