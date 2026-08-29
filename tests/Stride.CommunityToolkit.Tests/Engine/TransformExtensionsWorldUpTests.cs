using Stride.CommunityToolkit.Engine;
using Stride.Core.Mathematics;
using Stride.Engine;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Engine;

/// <summary>
/// Pins the contract of <see cref="TransformExtensions.WorldUp"/>: the default is Y-up, the
/// <c>LookAt</c> overloads without an explicit up vector follow it, and the overloads that take an
/// explicit up vector ignore it.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="TransformExtensions.WorldUp"/> is process-wide state, so this collection disables
/// parallelisation and every test that changes the value restores it in a <c>finally</c>. A test
/// elsewhere that calls a short <c>LookAt</c> overload while one of these is mid-flight would
/// otherwise see the wrong axis.
/// </para>
/// <para>
/// The tests look along +X and read back where the transform's local Y axis ends up, because that is
/// the one direction a look rotation is free to choose and the up vector is what decides it. Looking
/// along -Z, the obvious choice, would be degenerate for Z-up (forward parallel to up).
/// </para>
/// </remarks>
[Collection(Name)]
public class TransformExtensionsWorldUpTests
{
    public const string Name = "TransformExtensions.WorldUp";

    [CollectionDefinition(Name, DisableParallelization = true)]
    public class Collection;

    private static readonly Vector3 Target = new(10f, 0f, 0f);

    private static TransformComponent NewTransform() => new Entity().Transform;

    /// <summary>The world-space direction the transform's local Y axis points after rotation.</summary>
    private static Vector3 UpAxisOf(TransformComponent transform)
        => Vector3.Transform(Vector3.UnitY, transform.Rotation);

    private static void AssertAxis(Vector3 expected, Vector3 actual)
    {
        Assert.Equal(expected.X, actual.X, 3);
        Assert.Equal(expected.Y, actual.Y, 3);
        Assert.Equal(expected.Z, actual.Z, 3);
    }

    private static void WithWorldUp(Vector3 worldUp, Action body)
    {
        var previous = TransformExtensions.WorldUp;
        TransformExtensions.WorldUp = worldUp;

        try
        {
            body();
        }
        finally
        {
            TransformExtensions.WorldUp = previous;
        }
    }

    [Fact]
    public void DefaultIsYUp()
    {
        Assert.Equal(Vector3.UnitY, TransformExtensions.WorldUp);

        var transform = NewTransform();
        transform.LookAt(Target);

        AssertAxis(Vector3.UnitY, UpAxisOf(transform));
    }

    [Fact]
    public void ShortOverloadsFollowWorldUp()
    {
        WithWorldUp(Vector3.UnitZ, () =>
        {
            var byValue = NewTransform();
            byValue.LookAt(Target);
            AssertAxis(Vector3.UnitZ, UpAxisOf(byValue));

            var byRef = NewTransform();
            var target = Target;
            byRef.LookAt(ref target);
            AssertAxis(Vector3.UnitZ, UpAxisOf(byRef));

            var byTransform = NewTransform();
            var targetTransform = NewTransform();
            targetTransform.Position = Target;
            targetTransform.UpdateWorldMatrix();
            byTransform.LookAt(targetTransform);
            AssertAxis(Vector3.UnitZ, UpAxisOf(byTransform));
        });
    }

    [Fact]
    public void ExplicitUpOverloadsIgnoreWorldUp()
    {
        WithWorldUp(Vector3.UnitZ, () =>
        {
            var byValue = NewTransform();
            byValue.LookAt(Target, Vector3.UnitY);
            AssertAxis(Vector3.UnitY, UpAxisOf(byValue));

            var byRef = NewTransform();
            var target = Target;
            var up = Vector3.UnitY;
            byRef.LookAt(ref target, ref up);
            AssertAxis(Vector3.UnitY, UpAxisOf(byRef));

            var targetTransform = NewTransform();
            targetTransform.Position = Target;
            targetTransform.UpdateWorldMatrix();

            var byTransform = NewTransform();
            byTransform.LookAt(targetTransform, Vector3.UnitY);
            AssertAxis(Vector3.UnitY, UpAxisOf(byTransform));

            var byTransformRef = NewTransform();
            byTransformRef.LookAt(targetTransform, ref up);
            AssertAxis(Vector3.UnitY, UpAxisOf(byTransformRef));
        });
    }

    /// <summary>
    /// The short overloads hand the callee a local copy of <see cref="TransformExtensions.WorldUp"/>
    /// by <c>ref</c>. The callee treats it as input only, so the global must come back untouched.
    /// </summary>
    [Fact]
    public void LookAtDoesNotWriteBackToWorldUp()
    {
        var oblique = Vector3.Normalize(new Vector3(0f, 1f, 1f));

        WithWorldUp(oblique, () =>
        {
            NewTransform().LookAt(Target);

            Assert.Equal(oblique, TransformExtensions.WorldUp);
        });
    }
}