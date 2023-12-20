using CubicleCalamity.Components;
using Stride.CommunityToolkit.Engine;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;
using Stride.Physics;

namespace CubicleCalamity.Scripts;

public class RaycastHandler : AsyncScript
{
    private int _totalScore;

    public override async Task Execute()
    {
        var cameraComponent = Entity.Scene.Entities.FirstOrDefault(x => x.Get<CameraComponent>() != null)?.Get<CameraComponent>();

        // not working
        var cameraComponent2 = Entity.GetComponent<CameraComponent>();

        // not working
        var cameraComponent3 = this.GetFirstCamera();

        var simulation = this.GetSimulation();

        if (cameraComponent == null || simulation == null) return;

        while (Game.IsRunning)
        {
            if (Input.HasMouse && Input.IsMouseButtonPressed(MouseButton.Left))
            {
                var hitResult = cameraComponent.RaycastMouse(this);

                if (hitResult.Succeeded)
                {
                    OnEntityHit(hitResult.Collider.Entity);
                }
            }

            await Script.NextFrame();
        }
    }

    private void OnEntityHit(Entity entity)
    {
        if (entity.Name == "Cube")
        {
            var cubeComponent = entity.Get<CubeComponent>();

            Console.WriteLine($"Cube hit: {cubeComponent.Color}");
            Console.WriteLine($"Cube position: {entity.Transform.Position}");

            var cubesToRemove = GetCubesToRemove(entity, cubeComponent.Color);

            Console.WriteLine($"Cubes to remove: {cubesToRemove.Count()}");

            var score = CalculateScore(cubesToRemove.Count()).Result;

            _totalScore += score;

            Console.WriteLine($"Score: {CalculateScore(cubesToRemove.Count()).Calculations}, Total Score: {_totalScore - score} + {score}");

            foreach (var cube in cubesToRemove)
            {
                cube.Remove();
            }

            entity.Remove();
        }
    }

    private static IEnumerable<Entity> GetCubesToRemove(Entity entity, Color color)
    {
        var processedCubes = new HashSet<Entity>();
        var cubesToCheck = new Queue<Entity>();

        cubesToCheck.Enqueue(entity);

        while (cubesToCheck.Count > 0)
        {
            var currentCube = cubesToCheck.Dequeue();

            processedCubes.Add(currentCube);

            var touchingCubes = GetTouchingCubes(currentCube, color)
                .Where(cube => !processedCubes.Contains(cube) && !cubesToCheck.Contains(cube));

            foreach (var touchingCube in touchingCubes)
            {
                cubesToCheck.Enqueue(touchingCube);
            }
        }

        return processedCubes;
    }

    private static IEnumerable<Entity> GetTouchingCubes(Entity entity, Color color) => entity.Scene.Entities
        .Where(x => x.Name == "Cube"
            && (Equals(x.Transform.Position.Y, entity.Transform.Position.Y)
                    && ((Equals(x.Transform.Position.X, entity.Transform.Position.X - Constants.CubeSize.X)
                        || Equals(x.Transform.Position.X, entity.Transform.Position.X + Constants.CubeSize.X))
                        && Equals(x.Transform.Position.Z, entity.Transform.Position.Z)
                        || (Equals(x.Transform.Position.Z, entity.Transform.Position.Z - Constants.CubeSize.Z)
                        || Equals(x.Transform.Position.Z, entity.Transform.Position.Z + Constants.CubeSize.Z))
                        && Equals(x.Transform.Position.X, entity.Transform.Position.X)
                    )
                || (Equals(x.Transform.Position.Y, entity.Transform.Position.Y - Constants.CubeSize.Y)
                    || Equals(x.Transform.Position.Y, entity.Transform.Position.Y + Constants.CubeSize.Y))
                    && Equals(x.Transform.Position.X, entity.Transform.Position.X)
                    && Equals(x.Transform.Position.Z, entity.Transform.Position.Z)
                )
            && x.Get<CubeComponent>().Color == color
        );

    private static bool Equals(float a, float b, float tolerance = 0.1f)
        => Math.Abs(a - b) < tolerance;

    public static (int Result, string Calculations) CalculateScore(int numberOfCubes)
    {
        int baseScore = numberOfCubes * Constants.BasePointsPerCube;

        int bonus = numberOfCubes * numberOfCubes;

        return (baseScore + bonus, $"{numberOfCubes} * {Constants.BasePointsPerCube} + {numberOfCubes} * {numberOfCubes}");
    }
}