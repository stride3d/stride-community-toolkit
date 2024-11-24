using Stride.BepuPhysics;
using Stride.BepuPhysics.Components;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace Stride.CommunityToolkit.Bepu;

// ToDo Remove this class once it is implemented in Stride
[ComponentCategory("Bepu")]
public class Simulation2DComponent : SyncScript, ISimulationUpdate
{
    //public Entity Entity => throw new NotImplementedException();

    //public float MaxZLiberty { get; set; } = 0.05f;

    public void SimulationUpdate(BepuSimulation sim, float simTimeStep)
    {

    }

    public void AfterSimulationUpdate(BepuSimulation sim, float simTimeStep)
    {
        if (sim.Simulation == null)
            return;

        for (int i = 0; i < sim.Simulation.Bodies.ActiveSet.Count; i++)
        {
            var handle = sim.Simulation.Bodies.ActiveSet.IndexToHandle[i];
            var body = sim.GetComponent(handle);

            if (body is not Body2DComponent)
                continue;

            //if (body.Position.Z > MaxZLiberty || body.Position.Z < -MaxZLiberty)
            if (body.Position.Z != 0)
                body.Position *= new Vector3(1, 1, 0);//Fix Z = 0
            //if (body.LinearVelocity.Z > MaxZLiberty || body.LinearVelocity.Z < -MaxZLiberty)
            if (body.LinearVelocity.Z != 0)
                body.LinearVelocity *= new Vector3(1, 1, 0);

            var bodyRot = body.Orientation;
            Quaternion.RotationYawPitchRoll(ref bodyRot, out var yaw, out var pitch, out var roll);
            //if (yaw > MaxZLiberty || pitch > MaxZLiberty || yaw < -MaxZLiberty || pitch < -MaxZLiberty)
            if (yaw != 0 || pitch != 0)
                body.Orientation = Quaternion.RotationYawPitchRoll(0, 0, roll);
            //if (body.AngularVelocity.X > MaxZLiberty || body.AngularVelocity.Y > MaxZLiberty || body.AngularVelocity.X < -MaxZLiberty || body.AngularVelocity.Y < -MaxZLiberty)
            if (body.AngularVelocity.X != 0 || body.AngularVelocity.Y != 0)
                body.AngularVelocity *= new Vector3(0, 0, 1);
        }
    }

    public override void Update()
    {
    }
}
