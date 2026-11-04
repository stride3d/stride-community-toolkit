using Stride.CommunityToolkit.Mathematics;
using Stride.Core.Mathematics;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Mathematics;

/// <summary>
/// <see cref="MathUtilEx.MoveTowards(float, float, float)"/> and its vector overloads: a step of at most
/// the given length towards the target, landing on it exactly rather than overshooting.
/// </summary>
public class MoveTowardsTests
{
    [Theory]
    [InlineData(0f, 10f, 3f, 3f)]
    [InlineData(10f, 0f, 3f, 7f)]
    [InlineData(0f, 2f, 3f, 2f)]
    [InlineData(5f, 5f, 3f, 5f)]
    [InlineData(0f, -4f, 1f, -1f)]
    public void Float_steps_by_at_most_the_delta(float current, float target, float maxDelta, float expected)
        => Assert.Equal(expected, MathUtilEx.MoveTowards(current, target, maxDelta), 5);

    [Fact]
    public void Float_lands_on_the_target_exactly()
    {
        var value = 0f;

        for (var i = 0; i < 10; i++)
        {
            value = MathUtilEx.MoveTowards(value, 1f, 0.1f);
        }

        // Ten steps of a tenth accumulate rounding; the last step is clamped to the target itself
        Assert.Equal(1f, MathUtilEx.MoveTowards(value, 1f, 0.1f));
    }

    [Fact]
    public void Float_negative_delta_moves_away()
        => Assert.Equal(-2f, MathUtilEx.MoveTowards(0f, 10f, -2f));

    [Fact]
    public void Vector2_moves_along_the_line_to_the_target()
    {
        var result = MathUtilEx.MoveTowards(Vector2.Zero, new Vector2(3f, 4f), 2.5f);

        Assert.Equal(1.5f, result.X, 5);
        Assert.Equal(2f, result.Y, 5);
    }

    [Fact]
    public void Vector2_does_not_overshoot()
        => Assert.Equal(new Vector2(3f, 4f), MathUtilEx.MoveTowards(Vector2.Zero, new Vector2(3f, 4f), 10f));

    [Fact]
    public void Vector2_at_the_target_stays_there()
        => Assert.Equal(new Vector2(1f, 1f), MathUtilEx.MoveTowards(new Vector2(1f, 1f), new Vector2(1f, 1f), 0.5f));

    [Fact]
    public void Vector3_moves_along_the_line_to_the_target()
    {
        var result = MathUtilEx.MoveTowards(new Vector3(1f, 0f, 0f), new Vector3(1f, 0f, 10f), 4f);

        Assert.Equal(1f, result.X, 5);
        Assert.Equal(0f, result.Y, 5);
        Assert.Equal(4f, result.Z, 5);
    }

    [Fact]
    public void Vector3_does_not_overshoot()
        => Assert.Equal(Vector3.UnitY, MathUtilEx.MoveTowards(Vector3.Zero, Vector3.UnitY, 3f));

    [Fact]
    public void Vector3_zero_delta_does_not_move()
        => Assert.Equal(Vector3.Zero, MathUtilEx.MoveTowards(Vector3.Zero, Vector3.UnitX, 0f));
}