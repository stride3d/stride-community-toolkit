using Stride.CommunityToolkit.Mathematics;
using Stride.Core.Mathematics;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Mathematics;

/// <summary>
/// <see cref="QuaternionUtil.LookAt"/> gives the camera convention: looking down -Z is the identity, a target to the
/// left is a positive yaw, and a target above is a positive pitch.
/// </summary>
public class QuaternionUtilTests
{
    [Fact]
    public void LookAt_DownMinusZ_IsTheIdentity()
        => AssertSameRotation(Quaternion.Identity, QuaternionUtil.LookAt(Vector3.Zero, new Vector3(0, 0, -5)));

    [Fact]
    public void LookAt_ToTheLeft_IsAQuarterTurnOfYaw()
        => AssertSameRotation(Quaternion.RotationYawPitchRoll(MathUtil.PiOverTwo, 0f, 0f), QuaternionUtil.LookAt(Vector3.Zero, new Vector3(-5, 0, 0)));

    [Fact]
    public void LookAt_Upwards_IsAPositivePitch()
        => AssertSameRotation(Quaternion.RotationYawPitchRoll(0f, MathUtil.PiOverFour, 0f), QuaternionUtil.LookAt(Vector3.Zero, new Vector3(0, 5, -5)));

    [Fact]
    public void LookAt_FromAnywhere_PointsTheForwardAxisAtTheTarget()
    {
        var eye = new Vector3(3, 2, 1);
        var target = new Vector3(-4, 5, -6);

        var rotation = QuaternionUtil.LookAt(eye, target);
        var forward = Vector3.Transform(-Vector3.UnitZ, rotation);

        var expected = Vector3.Normalize(target - eye);

        Assert.Equal(expected.X, forward.X, 0.0001f);
        Assert.Equal(expected.Y, forward.Y, 0.0001f);
        Assert.Equal(expected.Z, forward.Z, 0.0001f);
    }

    /// <summary>A quaternion and its negation are one rotation, so the comparison is on the absolute dot product.</summary>
    private static void AssertSameRotation(Quaternion expected, Quaternion actual)
        => Assert.Equal(1f, MathF.Abs(Quaternion.Dot(expected, actual)), 0.0001f);
}