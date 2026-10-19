using Stride.CommunityToolkit.Engine;
using Stride.Core.Mathematics;
using Stride.Engine;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Engine;

/// <summary>
/// RotateAround swings the position and turns the orientation together; FaceDirection points -Z along a
/// world direction.
/// </summary>
public class TransformExtensionsRotateAroundTests
{
    private const float Tolerance = 1e-5f;

    [Fact]
    public void RotateAround_SwingsThePositionAboutThePoint()
    {
        var entity = new Entity { Transform = { Position = new Vector3(1f, 0f, 0f) } };

        // A quarter turn about world Y, counter-clockwise seen from above, takes +X to -Z
        entity.Transform.RotateAround(Vector3.Zero, Vector3.UnitY, MathUtil.PiOverTwo);

        Assert.Equal(0f, entity.Transform.Position.X, Tolerance);
        Assert.Equal(0f, entity.Transform.Position.Y, Tolerance);
        Assert.Equal(-1f, entity.Transform.Position.Z, Tolerance);
    }

    [Fact]
    public void RotateAround_TurnsTheOrientationBySameAngle()
    {
        var entity = new Entity { Transform = { Position = new Vector3(0f, 0f, 2f) } };

        entity.Transform.RotateAround(new Vector3(0f, 0f, 2f), Vector3.UnitY, MathUtil.PiOverTwo);

        // Position on the axis stays; the entity's forward (-Z) now points along -X
        Assert.Equal(2f, entity.Transform.Position.Z, Tolerance);

        var forward = entity.Transform.WorldMatrix.Forward;

        Assert.Equal(-1f, forward.X, Tolerance);
        Assert.Equal(0f, forward.Z, Tolerance);
    }

    [Fact]
    public void RotateAround_FullTurnReturnsToTheStart()
    {
        var entity = new Entity { Transform = { Position = new Vector3(3f, 1f, -2f) } };
        var point = new Vector3(1f, 1f, 1f);
        var axis = new Vector3(1f, 2f, 3f);

        for (var i = 0; i < 8; i++)
        {
            entity.Transform.RotateAround(point, axis, MathUtil.TwoPi / 8f);
        }

        Assert.Equal(3f, entity.Transform.Position.X, 1e-4f);
        Assert.Equal(1f, entity.Transform.Position.Y, 1e-4f);
        Assert.Equal(-2f, entity.Transform.Position.Z, 1e-4f);
    }

    [Fact]
    public void RotateAround_RejectsAZeroAxis()
    {
        var entity = new Entity();

        Assert.Throws<ArgumentException>(() => entity.Transform.RotateAround(Vector3.Zero, Vector3.Zero, 1f));
    }

    [Fact]
    public void FaceDirection_PointsForwardAlongTheDirection()
    {
        var entity = new Entity { Transform = { Position = new Vector3(5f, 0f, 5f) } };

        entity.Transform.FaceDirection(new Vector3(2f, 0f, 0f));

        var forward = entity.Transform.WorldMatrix.Forward;

        Assert.Equal(1f, forward.X, Tolerance);
        Assert.Equal(0f, forward.Y, Tolerance);
        Assert.Equal(0f, forward.Z, Tolerance);
    }

    [Fact]
    public void FaceDirection_IgnoresAZeroDirection()
    {
        var entity = new Entity();
        var before = entity.Transform.Rotation;

        entity.Transform.FaceDirection(Vector3.Zero);

        Assert.Equal(before, entity.Transform.Rotation);
    }

    [Fact]
    public void FaceDirection_OnAChildFacesTheWorldDirection()
    {
        var parent = new Entity();
        var child = new Entity();

        parent.AddChild(child);
        parent.Transform.Rotation = Quaternion.RotationY(MathUtil.PiOverTwo);
        parent.Transform.UpdateWorldMatrix();

        child.Transform.FaceDirection(Vector3.UnitX);

        var forward = child.Transform.WorldMatrix.Forward;

        Assert.Equal(1f, forward.X, Tolerance);
        Assert.Equal(0f, forward.Z, Tolerance);
    }
}