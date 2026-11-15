using Stride.CommunityToolkit.Mathematics;
using Stride.Core.Mathematics;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Mathematics;

/// <summary>
/// <see cref="QuaternionUtil.LookAt(Vector3, Vector3, Vector3?)"/> gives the camera convention: looking down -Z is
/// the identity, a target to the left is a positive yaw, and a target above is a positive pitch. The degenerate
/// inputs that used to produce a non-finite or non-unit quaternion are guarded too.
/// </summary>
/// <remarks>
/// A NaN here does not stay local: assigning it to a <c>TransformComponent.Rotation</c> poisons the
/// entity's rotation matrix, and anything that then integrates a position through that matrix turns
/// the position into NaN too. In a camera that means a view that cannot be recovered except by
/// resetting the transform outright, which is how this was found.
/// </remarks>
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

    /// <summary>
    /// The forward direction has to keep pointing at the target, which is the part a rewrite of the
    /// conversion could silently get wrong while still returning something finite.
    /// </summary>
    [Theory]
    [InlineData(3f, 2f, 1f, -4f, 5f, -6f)]
    [InlineData(0f, 0f, 10f, 0f, 0f, 0f)]
    [InlineData(10f, 0f, 0f, 0f, 0f, 0f)]
    [InlineData(-7f, 3f, 4f, 0f, 0f, 0f)]
    [InlineData(0f, -6f, 2f, 0f, 0f, 0f)]
    public void LookAt_FromAnywhere_PointsTheForwardAxisAtTheTarget(float eyeX, float eyeY, float eyeZ, float targetX, float targetY, float targetZ)
    {
        var eye = new Vector3(eyeX, eyeY, eyeZ);
        var target = new Vector3(targetX, targetY, targetZ);

        var rotation = QuaternionUtil.LookAt(eye, target);

        // A Stride camera with no rotation looks down its local -Z, so the world-space view direction
        // is that axis put through the rotation
        var forward = Vector3.Transform(-Vector3.UnitZ, rotation);
        var expected = Vector3.Normalize(target - eye);

        Assert.Equal(expected.X, forward.X, Tolerances.Geometry);
        Assert.Equal(expected.Y, forward.Y, Tolerances.Geometry);
        Assert.Equal(expected.Z, forward.Z, Tolerances.Geometry);
    }

    /// <summary>The by-reference form is the same rotation as the by-value one.</summary>
    [Fact]
    public void LookAt_ByReference_MatchesByValue()
    {
        var eye = new Vector3(-7, 3, 4);
        var target = new Vector3(1, 2, -3);
        var up = Vector3.UnitY;

        QuaternionUtil.LookAt(in eye, in target, in up, out var byReference);

        AssertSameRotation(QuaternionUtil.LookAt(eye, target, up), byReference);
    }

    /// <summary>
    /// The case that was actually hit in play: orbiting a camera to the far side of its target, so
    /// the look rotation is a 180 degree turn about Y. The rotation matrix trace is exactly -1 there,
    /// which the old single-branch conversion turned into a division by zero.
    /// </summary>
    [Fact]
    public void LookingFromBehindTheTargetIsFiniteAndUnit()
        => AssertFiniteAndUnit(QuaternionUtil.LookAt(new Vector3(0, 2.5f, -10), new Vector3(0, 2.5f, 0)));

    /// <summary>
    /// The same singularity swept through, because the exact angle is only reached when the frame
    /// timing happens to land on it. Every orbit angle has to be safe, not just the ones a given run
    /// samples.
    /// </summary>
    [Fact]
    public void EveryOrbitAngleIsFiniteAndUnit()
    {
        var target = new Vector3(0, 2.5f, 0);

        for (var degrees = 0; degrees < 360; degrees++)
        {
            var radians = MathUtil.DegreesToRadians(degrees);
            var eye = target + new Vector3(MathF.Sin(radians) * 10f, 4f, MathF.Cos(radians) * 10f);

            AssertFiniteAndUnit(QuaternionUtil.LookAt(eye, target));
        }
    }

    /// <summary>
    /// Looking straight down: the forward direction is parallel to the up vector, so the basis cannot
    /// be built the usual way and a fallback reference axis is needed. Straight up is the same degeneracy
    /// with the opposite sign.
    /// </summary>
    [Theory]
    [InlineData(0f, 10f, 0f, 0f, 0f, 0f)]
    [InlineData(0f, 0f, 0f, 0f, 10f, 0f)]
    public void LookingAlongTheUpVectorIsFiniteAndUnit(float eyeX, float eyeY, float eyeZ, float targetX, float targetY, float targetZ)
        => AssertFiniteAndUnit(QuaternionUtil.LookAt(new Vector3(eyeX, eyeY, eyeZ), new Vector3(targetX, targetY, targetZ)));

    /// <summary>
    /// Eye and target in the same place. There is no direction to face, so the only requirement is
    /// that it does not produce a broken rotation; the identity is what comes back.
    /// </summary>
    [Fact]
    public void EyeAtTargetIsTheIdentity()
    {
        var rotation = QuaternionUtil.LookAt(new Vector3(3, 4, 5), new Vector3(3, 4, 5));

        AssertFiniteAndUnit(rotation);
        AssertSameRotation(Quaternion.Identity, rotation);
    }

    /// <summary>
    /// A zero up vector is a caller error rather than a geometric degeneracy, but it should still not
    /// hand back something that will poison a transform.
    /// </summary>
    [Fact]
    public void ZeroUpVectorIsFiniteAndUnit()
        => AssertFiniteAndUnit(QuaternionUtil.LookAt(new Vector3(0, 0, 10), Vector3.Zero, Vector3.Zero));

    /// <summary>A quaternion and its negation are one rotation, so the comparison is on the absolute dot product.</summary>
    private static void AssertSameRotation(Quaternion expected, Quaternion actual)
        => Assert.Equal(1f, MathF.Abs(Quaternion.Dot(expected, actual)), Tolerances.Geometry);

    private static void AssertFiniteAndUnit(Quaternion rotation)
    {
        Assert.True(float.IsFinite(rotation.X) && float.IsFinite(rotation.Y) && float.IsFinite(rotation.Z) && float.IsFinite(rotation.W), $"Not finite: {rotation}");
        Assert.Equal(1f, rotation.Length(), 3);
    }
}