using Stride.CommunityToolkit.Mathematics;
using Stride.Core.Mathematics;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Mathematics;

public class RandomExtensionsTests
{
    // A fixed seed, so a failure is the same failure every run
    private readonly Random _random = new(123456);

    [Fact]
    public void NextSingle_ReturnsValueBetweenZeroAndOne()
    {
        for (var i = 0; i < 1000; i++) Assert.InRange(_random.NextSingle(), 0f, 1f);
    }

    [Fact]
    public void NextPoint2D_InsideRectangle()
    {
        var rect = new RectangleF(10, 20, 5, 8);

        for (var i = 0; i < 500; i++)
        {
            var point = _random.NextPoint(rect);

            Assert.InRange(point.X, rect.X, rect.X + rect.Width);
            Assert.InRange(point.Y, rect.Y, rect.Y + rect.Height);
        }
    }

    [Fact]
    public void NextPoint3D_InsideBoundingBox()
    {
        var box = new BoundingBox(new Vector3(-2, -1, -3), new Vector3(4, 5, 6));

        for (var i = 0; i < 500; i++)
        {
            var point = _random.NextPoint(box);

            Assert.InRange(point.X, box.Minimum.X, box.Maximum.X);
            Assert.InRange(point.Y, box.Minimum.Y, box.Maximum.Y);
            Assert.InRange(point.Z, box.Minimum.Z, box.Maximum.Z);
        }
    }

    [Fact]
    public void NextDirection2D_IsNormalized()
    {
        for (var i = 0; i < 200; i++)
        {
            var dir = _random.NextDirection2D();

            // The length drifts a little with float precision, and a zero vector is left as it is by the normalisation
            Assert.InRange(dir.Length(), 0.0f, 1.0001f);

            if (dir != Vector2.Zero)
            {
                Assert.InRange(Math.Abs(dir.Length() - 1f), 0f, 1e-4f);
            }
        }
    }

    [Fact]
    public void NextDirection3D_IsNormalized()
    {
        for (var i = 0; i < 200; i++)
        {
            var dir = _random.NextDirection3D();

            Assert.InRange(dir.Length(), 0.0f, 1.0001f);

            if (dir != Vector3.Zero)
            {
                Assert.InRange(Math.Abs(dir.Length() - 1f), 0f, 2e-4f);
            }
        }
    }

    [Fact]
    public void PointInACircle_WithinRadius()
    {
        const float radius = 5f;

        for (var i = 0; i < 1000; i++)
        {
            var point = _random.PointInACircle(radius);

            Assert.True(point.Length() <= radius + 1e-5f, $"Point {point} outside radius {radius}");
        }
    }

    [Fact]
    public void NextColor_ReturnsOpaqueColor()
    {
        for (var i = 0; i < 10; i++) Assert.Equal<byte>(255, _random.NextColor().A);
    }

    [Fact]
    public void NextVector3_StaysInsideTheBox()
    {
        var min = new Vector3(-4, 4, -4);
        var max = new Vector3(4, 8, 4);

        for (var i = 0; i < 500; i++)
        {
            var point = _random.NextVector3(min, max);

            Assert.InRange(point.X, min.X, max.X);
            Assert.InRange(point.Y, min.Y, max.Y);
            Assert.InRange(point.Z, min.Z, max.Z);
        }

        var cube = _random.NextVector3(1f, 2f);

        Assert.InRange(cube.X, 1f, 2f);
        Assert.InRange(cube.Y, 1f, 2f);
        Assert.InRange(cube.Z, 1f, 2f);
    }

    [Theory]
    [InlineData(ColorTone.Vivid, 0.7f, 1f, 0.8f, 1f)]
    [InlineData(ColorTone.Light, 0.2f, 0.5f, 0.9f, 1f)]
    [InlineData(ColorTone.Dark, 0.5f, 1f, 0.2f, 0.45f)]
    public void NextColor_StaysInsideItsTone(ColorTone tone, float minSaturation, float maxSaturation, float minValue, float maxValue)
    {
        for (var i = 0; i < 200; i++)
        {
            var color = _random.NextColor(tone);
            var hsv = ColorHSV.FromColor(color);

            // Going through bytes moves saturation and value by a few hundredths at the dim end
            Assert.Equal<byte>(255, color.A);
            Assert.InRange(hsv.S, minSaturation - 0.03f, maxSaturation + 0.03f);
            Assert.InRange(hsv.V, minValue - 0.03f, maxValue + 0.03f);
        }
    }
}