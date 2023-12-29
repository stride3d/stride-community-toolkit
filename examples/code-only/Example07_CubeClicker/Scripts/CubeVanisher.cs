using Stride.CommunityToolkit.Engine;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Physics;

namespace Example07_CubeClicker.Scripts;

public class CubeVanisher : AsyncScript
{
    private const float TotalTime = 1.7f;
    private const float RotationSpeed = 900;

    public override async Task Execute()
    {
        var elapsedTime = 0f;

        var collider = Entity.GetComponent<RigidbodyComponent>();

        while (elapsedTime < TotalTime)
        {
            elapsedTime += (float)Game.UpdateTime.Elapsed.TotalSeconds;

            Entity.Transform.Scale = new Vector3(1 - elapsedTime / TotalTime);
            Entity.Transform.Rotation = Quaternion.RotationY(MathUtil.DegreesToRadians(RotationSpeed * elapsedTime));

            collider?.UpdatePhysicsTransformation();

            await Script.NextFrame();
        }

        Entity.Remove();

        Console.WriteLine("Entity removed");
    }
}