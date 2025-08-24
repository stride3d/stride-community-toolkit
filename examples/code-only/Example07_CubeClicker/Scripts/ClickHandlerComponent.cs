using Example07_CubeClicker.Managers;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Mathematics;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;
using Stride.Rendering;

namespace Example07_CubeClicker.Scripts;

public class ClickHandlerComponent : AsyncScript
{
    private const string HitEntityName = "Cube";
    private readonly Vector3 _defaultCubePosition = new(0, 8, 0);
    private readonly Random _random = new();
    private GameManager? _gameManager;
    private CameraComponent? _camera;
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
        if (_camera!.RaycastMouse(this, 100, out var hitResult) && hitResult.Collidable.Entity.Name == HitEntityName)
        {
            if (mouseButton == MouseButton.Left)
            {
                AddNewEntity(hitResult.Collidable.Entity);
            }
            else if (mouseButton == MouseButton.Right)
            {
                RemoveEntity(hitResult.Collidable.Entity);
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

        CreateCube(_random.NextPoint(new BoundingBox(Vector3.One * -7, Vector3.One * 7)) + new Vector3(0, 10, 0), Vector3.Zero);
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

    private void CreateCube(Vector3? position = null, Vector3? size = null)
    {
        var entity = Game.Create3DPrimitive(PrimitiveModelType.Cube, new() { EntityName = HitEntityName });

        entity.Transform.Scale = size ?? Vector3.One;
        entity.Transform.Position = position ?? _defaultCubePosition;
        entity.Add(new CubeGrower());
        entity.Scene = Entity.Scene;
    }
}