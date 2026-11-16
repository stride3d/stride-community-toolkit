using Stride.CommunityToolkit.Engine;
using Stride.Core.Mathematics;
using Stride.Engine;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Engine;

/// <summary>
/// <c>LookAt</c> aims from where the transform is now. The engine rebuilds a transform's matrices once
/// a frame, so a transform that is moved and then turned in the same update still has last frame's
/// matrices; these tests move one without updating them, the way a follow camera does.
/// </summary>
public class TransformExtensionsLookAtTests
{
    private static Vector3 ForwardOf(TransformComponent transform)
        => Vector3.Transform(-Vector3.UnitZ, transform.Rotation);

    private static void AssertDirection(Vector3 expected, Vector3 actual)
    {
        Assert.Equal(expected.X, actual.X, 4);
        Assert.Equal(expected.Y, actual.Y, 4);
        Assert.Equal(expected.Z, actual.Z, 4);
    }

    [Fact]
    public void LookAt_AimsFromThePositionSetInTheSameUpdate()
    {
        var transform = new Entity().Transform;

        transform.UpdateWorldMatrix();
        transform.Position = new Vector3(10f, 0f, 0f);

        transform.LookAt(new Vector3(10f, 0f, -5f), Vector3.UnitY);

        AssertDirection(-Vector3.UnitZ, ForwardOf(transform));
    }

    [Fact]
    public void LookAt_AimsFromTheLocalPositionOfAChildMovedInTheSameUpdate()
    {
        var parent = new Entity { Transform = { Position = new Vector3(0f, 0f, 20f) } };
        var child = new Entity();

        parent.AddChild(child);
        parent.Transform.UpdateWorldMatrix();
        child.Transform.UpdateWorldMatrix();

        // World (4, 0, 20) now, with matrices that still say (0, 0, 20)
        child.Transform.Position = new Vector3(4f, 0f, 0f);

        child.Transform.LookAt(new Vector3(4f, 0f, 30f), Vector3.UnitY);

        AssertDirection(Vector3.UnitZ, ForwardOf(child.Transform));
    }
}