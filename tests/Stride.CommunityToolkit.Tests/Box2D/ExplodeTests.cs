using Stride.CommunityToolkit.Box2D;
using Stride.CommunityToolkit.Tests.Engine;
using Stride.Core.Mathematics;
using Xunit;
using static Box2D.NET.B2Bodies;

namespace Stride.CommunityToolkit.Tests.Box2D;

/// <summary>
/// Pins <see cref="Box2DSimulation.Explode"/>: a body inside the radius is pushed away from the
/// centre, one beyond the radius and falloff is not touched.
/// </summary>
/// <remarks>
/// Box2D.NET keeps its worlds in process-wide state that is not safe to create and destroy from
/// several test classes at once, so every test that owns a world runs in the serialised collection.
/// </remarks>
[Collection(GameExtensionsRunTests.Name)]
public class ExplodeTests
{
    [Fact]
    public void Explode_PushesBodiesInsideTheRadiusAwayFromTheCentre()
    {
        using var simulation = new Box2DSimulation();
        var near = Joints2DTests.Box(simulation, new Vector3(3, 0, 0));
        var far = Joints2DTests.Box(simulation, new Vector3(30, 0, 0));

        simulation.Explode(Vector2.Zero, radius: 5, impulsePerLength: 10, falloff: 1);

        var nearVelocity = b2Body_GetLinearVelocity(near);
        var farVelocity = b2Body_GetLinearVelocity(far);

        Assert.True(nearVelocity.X > 0, $"Expected the near body to move away along +x; velocity was {nearVelocity.X}.");
        Assert.Equal(0, farVelocity.X, 5);
    }
}
