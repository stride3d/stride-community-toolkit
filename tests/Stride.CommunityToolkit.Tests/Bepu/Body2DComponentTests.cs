using Stride.BepuPhysics.Definitions.Colliders;
using Stride.CommunityToolkit.Bepu;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Bepu;

/// <summary>
/// Covers the parts of the toolkit's <see cref="Body2DComponent"/> that do not need a running
/// simulation.
/// </summary>
/// <remarks>
/// <para>
/// The plane confinement itself only exists while a <c>BepuSimulation</c> is stepping, so it belongs
/// in an integration test built on Stride's <c>GameTestBase</c> rather than here. What is testable in
/// isolation is the <see cref="Body2DComponent.ZTolerance"/> validation, which silently substitutes
/// the default for values that would otherwise break the correction: negative or zero would make it
/// fire on floating-point noise and write a velocity every step, stopping bodies from sleeping, while
/// NaN and infinity would disable it altogether.
/// </para>
/// <para>
/// This file deliberately does not import <c>Stride.BepuPhysics</c>, which declares a component of
/// the same name, so these tests always run against the toolkit's copy.
/// </para>
/// </remarks>
public class Body2DComponentTests
{
    private const float DefaultZTolerance = 0.001f;

    /// <summary>
    /// Creates a body outside any simulation. <c>Collider</c> is a required member, so it has to be
    /// supplied even though nothing here attaches the body or reads the collider.
    /// </summary>
    private static Body2DComponent CreateBody() => new() { Collider = new CompoundCollider() };

    [Fact]
    public void ZTolerance_DefaultsToOneMillimetre()
    {
        Assert.Equal(DefaultZTolerance, CreateBody().ZTolerance);
    }

    [Theory]
    [InlineData(0.5f)]
    [InlineData(0.001f)]
    [InlineData(1e-7f)]
    [InlineData(float.Epsilon)]
    [InlineData(1000f)]
    public void ZTolerance_KeepsFinitePositiveValues(float value)
    {
        var body = CreateBody();

        body.ZTolerance = value;

        Assert.Equal(value, body.ZTolerance);
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-0.001f)]
    [InlineData(-1000f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void ZTolerance_FallsBackToDefaultForUnusableValues(float value)
    {
        var body = CreateBody();

        body.ZTolerance = value;

        Assert.Equal(DefaultZTolerance, body.ZTolerance);
    }

    [Fact]
    public void ZTolerance_RecoversAfterAnUnusableValue()
    {
        // A rejected assignment must not leave the property stuck or poison later valid ones
        var body = CreateBody();

        body.ZTolerance = 0.25f;
        body.ZTolerance = float.NaN;

        Assert.Equal(DefaultZTolerance, body.ZTolerance);

        body.ZTolerance = 0.5f;

        Assert.Equal(0.5f, body.ZTolerance);
    }
}
