using Example_CubicleCalamity.Components;
using Example_CubicleCalamity.Shared;
using Stride.Audio;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;

namespace Example_CubicleCalamity.Scripts;

public class RaycastInteractionScript : AsyncScript
{
    private int _totalScore;
    private SoundInstance? _soundInstance;
    private EntityTextComponent? _scoreComponent;

    public override async Task Execute()
    {
        var cameraComponent = Entity.Scene.GetCamera();
        var totalScoreEntity = Entity.Scene.Entities.FirstOrDefault(e => e.Name == Constants.TotalScore);

        _scoreComponent = totalScoreEntity?.Get<EntityTextComponent>();

        //var simulation = this.GetSimulation();

        if (cameraComponent == null) return;

        var sound = Game.Content.Load<Sound>("wood-tap-5");
        _soundInstance = sound.CreateInstance(Audio.AudioEngine.DefaultListener);

        while (Game.IsRunning)
        {
            if (!Input.HasMouse)
            {
                await Script.NextFrame();

                continue;
            }

            if (Input.IsKeyDown(Keys.LeftShift) && Input.IsMouseButtonDown(MouseButton.Left))
            {
                ProcessMouseRaycast(cameraComponent);
            }
            else if (Input.IsMouseButtonPressed(MouseButton.Left))
            {
                ProcessMouseRaycast(cameraComponent);
            }

            await Script.NextFrame();
        }
    }

    private void ProcessMouseRaycast(CameraComponent cameraComponent)
    {
        var hit = cameraComponent.RaycastMouse(this, 100, out var hitInfo);

        if (hit)
        {
            OnEntityHit(hitInfo.Collidable.Entity);
        }
    }

    private void OnEntityHit(Entity entity)
    {
        if (entity.Name == "Cube")
        {
            _soundInstance?.Play();

            var cubeComponent = entity.Get<CubeComponent>();

            Console.WriteLine($"Cube hit: {cubeComponent.Color}");
            Console.WriteLine($"Cube position: {entity.Transform.Position}");

            var cubesToRemove = GetCubesToRemove(entity, cubeComponent.Color);

            Console.WriteLine($"Cubes to remove: {cubesToRemove.Count()}");

            var score = CalculateScore(cubesToRemove.Count()).Result;

            _totalScore += score;

            if (_scoreComponent != null)
            {
                _scoreComponent.Text = $"{Constants.TotalScore}: {_totalScore:N0}";
            }

            Console.WriteLine($"Score: {CalculateScore(cubesToRemove.Count()).Calculations}, Total Score: {_totalScore - score} + {score}");

            foreach (var cube in cubesToRemove)
            {
                cube.Remove();
            }

            AddDisplayScoreEntity(entity.Transform.Position, score);

            entity.Remove();
        }
    }

    private void AddDisplayScoreEntity(Vector3 position, int score)
    {
        var fontSize = score > 10000 ? 24 : 18;

        var entity = new Entity("DisplayScore", position)
        {
            new EntityTextComponent() { Text = score.ToString(), FontSize = fontSize },
            new ScoreScript()
        };

        entity.Scene = SceneSystem.SceneInstance.RootScene;
    }

    private static IEnumerable<Entity> GetCubesToRemove(Entity entity, Color color)
    {
        var processedCubes = new HashSet<Entity>();
        var cubesToCheck = new Queue<Entity>();

        cubesToCheck.Enqueue(entity);

        while (cubesToCheck.TryDequeue(out var currentCube))
        {
            if (!processedCubes.Add(currentCube)) continue;

            foreach (var touchingCube in GetTouchingCubes(currentCube, color))
            {
                if (processedCubes.Contains(touchingCube)) continue;

                cubesToCheck.Enqueue(touchingCube);
            }
        }

        return processedCubes;
    }

    private static IEnumerable<Entity> GetTouchingCubes(Entity entity, Color color)
    {
        var position = entity.Transform.Position;

        return entity.Scene.Entities.Where(x =>
            x.Name == "Cube" &&
            x.Get<CubeComponent>().Color == color &&
            IsNeighbor(position, x.Transform.Position, Constants.CubeSize));
    }

    private static bool IsNeighbor(Vector3 position, Vector3 otherPosition, Vector3 cubeSize)
        => AreEqual(position.Y, otherPosition.Y) && (
                (AreEqual(position.X, otherPosition.X - cubeSize.X) || AreEqual(position.X, otherPosition.X + cubeSize.X)) &&
                AreEqual(position.Z, otherPosition.Z) ||
                (AreEqual(position.Z, otherPosition.Z - cubeSize.Z) || AreEqual(position.Z, otherPosition.Z + cubeSize.Z)) &&
                AreEqual(position.X, otherPosition.X)
            ) ||
            AreEqual(position.X, otherPosition.X) &&
            AreEqual(position.Z, otherPosition.Z) &&
            (AreEqual(position.Y, otherPosition.Y - cubeSize.Y) || AreEqual(position.Y, otherPosition.Y + cubeSize.Y));

    private static bool AreEqual(float a, float b, float tolerance = 0.1f)
        => Math.Abs(a - b) < tolerance;

    public static (int Result, string Calculations) CalculateScore(int numberOfCubes)
    {
        int baseScore = numberOfCubes * Constants.BasePointsPerCube;

        int bonus = (numberOfCubes == 1 ? 0 : numberOfCubes) * numberOfCubes * 10;

        return (baseScore + bonus, $"{numberOfCubes} * {Constants.BasePointsPerCube} + {numberOfCubes} * {numberOfCubes}");
    }
}