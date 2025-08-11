using Stride.CommunityToolkit.Mathematics;
using Stride.Core.Mathematics;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Mathematics;

public class RandomExtensionsTests
{
    private readonly Random _random = new(123456); // deterministic seed

    [Fact]
    public void NextSingle_ReturnsValueBetweenZeroAndOne()
    {
        for (int i = 0; i < 1000; i++)
        {
            float value = _random.NextSingle();
            Assert.InRange(value, 0f, 1f);
        }
    }

    [Fact]
    public void NextPoint2D_InsideRectangle()
    {
        var rect = new RectangleF(10, 20, 5, 8);

        for (int i = 0; i < 500; i++)
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

        for (int i = 0; i < 500; i++)
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
        for (int i = 0; i < 200; i++)
        {
            var dir = _random.NextDirection2D();

            // length can drift slightly due to float precision and zero vector probability (~ ignored by normalization)
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
        for (int i = 0; i < 200; i++)
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

        for (int i = 0; i < 1000; i++)
        {
            var point = _random.PointInACircle(radius);
            Assert.True(point.Length() <= radius + 1e-5f, $"Point {point} outside radius {radius}");
        }
    }

    [Fact]
    public void NextColor_ReturnsOpaqueColor()
    {
        for (int i = 0; i < 10; i++)
        {
            var color = _random.NextColor();
            Assert.Equal<byte>(255, color.A);
        }
    }
}