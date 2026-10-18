using Stride.Core.Mathematics;
using Stride.Engine;

namespace E04_CubeClicker.Scripts;

public class CubeGrower : AsyncScript
{
    private const float GrowDuration = 1.0f;

    public override async Task Execute()
    {
        var elapsedTime = 0f;

        Entity.Transform.Scale = Vector3.Zero;

        while (elapsedTime < GrowDuration)
        {
            elapsedTime += (float)Game.UpdateTime.Elapsed.TotalSeconds;

            var newScale = elapsedTime / GrowDuration;
            Entity.Transform.Scale = new Vector3(newScale);

            await Script.NextFrame();
        }

        // Exactly full size at the end, whatever the last step was
        Entity.Transform.Scale = Vector3.One;

        Entity.Remove<CubeGrower>();

        Console.WriteLine("Entity grown");
    }
}