using Stride.CommunityToolkit.Shapes;
using Stride.Core.Mathematics;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Rendering;

/// <summary>
/// What a draw call records for the GPU at the edges of its input: shapes with no extent, a
/// rounding larger than the rectangle, a line shorter than it is wide. No graphics device; the
/// records are read as the batch holds them.
/// </summary>
public class ShapeBatchSubmissionTests
{
    [Fact]
    public void FlatOverloadsDrawWhatTheirXYPlaneCallsDraw()
    {
        var flat = new ShapeBatch();
        var plane = new ShapeBatch();

        flat.DrawRectangle(new Vector2(3f, 2f), new Vector2(4f, 1f), Color.Red, cornerRadius: 0.2f);
        plane.DrawRectangle(new Vector3(3f, 2f, 0f), Vector3.UnitX, Vector3.UnitY, new Vector2(4f, 1f), Color.Red, 0.2f);
        flat.DrawDisc(new Vector2(1f, 1f), 0.5f, Color.Red);
        plane.DrawDisc(new Vector3(1f, 1f, 0f), Vector3.UnitZ, 0.5f, Color.Red);
        flat.DrawPixelLine(new Vector2(0f, 0f), new Vector2(2f, 1f), 2f, Color.Red);
        plane.DrawPixelLine(Vector3.Zero, new Vector3(2f, 1f, 0f), 2f, Color.Red);

        Assert.Equal(plane.Instances.Count, flat.Instances.Count);

        for (var i = 0; i < flat.Instances.Count; i++)
        {
            Assert.Equal(plane.Instances[i].Position, flat.Instances[i].Position);
            Assert.Equal(plane.Instances[i].Radius, flat.Instances[i].Radius);
        }

        Assert.Equal(plane.Points, flat.Points);
    }

    [Fact]
    public void FlatRectangleTurnsCounterClockwise()
    {
        var batch = new ShapeBatch();

        batch.DrawRectangle(Vector2.Zero, new Vector2(4f, 1f), Color.Red, rotation: MathF.PI / 2f);

        // A quarter turn puts the rectangle's X axis along world Y
        var axis = Assert.Single(batch.Instances).AxisX;

        Assert.Equal(0f, axis.X, 5);
        Assert.Equal(1f, axis.Y, 5);
    }

    [Theory]
    [InlineData(0.5f)]
    [InlineData(2f)]
    public void LineIsSolidWhateverItsLength(float length)
    {
        var batch = new ShapeBatch { BorderWidth = 0f };

        // A fill the line must ignore: it is drawn solid, in its own colour
        batch.Fill.Set(Color.Blue, 0f);
        batch.DrawLine(Vector3.Zero, new Vector3(length, 0f, 0f), 1f, Color.Red);

        var line = Assert.Single(batch.Instances);

        Assert.Equal(Color.Red, line.FillColor);
        Assert.Equal(1f, line.AxisY.W);
    }

    [Fact]
    public void ShapeIsSolidInTheColourOfItsDrawCallByDefault()
    {
        var batch = new ShapeBatch();

        batch.DrawRectangle(Vector3.Zero, Vector3.UnitX, Vector3.UnitY, new Vector2(2f, 1f), Color.Red);

        var rectangle = Assert.Single(batch.Instances);

        Assert.Equal(Color.Red, rectangle.Color);
        Assert.Equal(Color.Red, rectangle.FillColor);
        Assert.Equal(1f, rectangle.AxisY.W);
    }

    [Fact]
    public void CircleOfRadiusZeroHasAScaleTheShaderCanDivideBy()
    {
        var batch = new ShapeBatch();

        batch.DrawSolidCircle(Vector2.Zero, 0f, Color.White);

        Assert.True(Assert.Single(batch.Instances).LocalScale > 0f);
    }

    [Fact]
    public void PolylineOfCoincidentPointsHasAScaleTheShaderCanDivideBy()
    {
        var batch = new ShapeBatch();

        batch.DrawPixelPolyline([new Vector2(3f, 3f), new Vector2(3f, 3f), new Vector2(3f, 3f)], 2f, Color.White);

        Assert.All(batch.Instances, instance => Assert.True(instance.LocalScale > 0f));
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-1f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void ShapeScaledToNothingIsNotDrawn(float scale)
    {
        var batch = new ShapeBatch();

        batch.DrawSolidPolygon([new Vector2(-1f, -1f), new Vector2(1f, -1f), new Vector2(0f, 1f)], Vector3.Zero, Vector3.UnitX, Vector3.UnitY, Color.White, scale: scale);

        Assert.Empty(batch.Instances);
    }

    [Fact]
    public void RadiusThatIsNotANumberIsNotDrawn()
    {
        var batch = new ShapeBatch();

        batch.DrawSolidCircle(Vector2.Zero, float.NaN, Color.White);

        Assert.Empty(batch.Instances);
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(0.5f)]
    [InlineData(1f)]
    [InlineData(2f)]
    [InlineData(100f)]
    public void RectangleKeepsItsSizeWhateverTheRounding(float cornerRadius)
    {
        var batch = new ShapeBatch();

        batch.DrawRectangle(Vector3.Zero, Vector3.UnitX, Vector3.UnitY, new Vector2(4f, 2f), Color.White, cornerRadius);

        var rectangle = Assert.Single(batch.Instances);
        var upper = new Vector2(float.MinValue);

        // The outline reaches the rounding radius beyond the furthest corner
        for (var i = 0; i < rectangle.Count; i++)
        {
            upper = Vector2.Max(upper, batch.Points[rectangle.PointOffset + i] * rectangle.LocalScale + rectangle.Center);
        }

        Assert.Equal(2f, upper.X + rectangle.Radius, 3);
        Assert.Equal(1f, upper.Y + rectangle.Radius, 3);
    }

    [Theory]
    [InlineData(false, 64, 64, 4)]
    [InlineData(true, 64, 64, 5)]
    public void LongPolylineIsSplitIntoPiecesThatShareAPoint(bool closed, int first, int second, int third)
    {
        var batch = new ShapeBatch();
        var points = new Vector2[130];

        for (var i = 0; i < points.Length; i++) points[i] = new Vector2(i, i % 2);

        batch.DrawPixelPolyline(points, 2f, Color.White, closed);

        Assert.Equal([first, second, third], batch.Instances.Select(instance => instance.Count));

        // The caller's points are read, not kept
        Assert.Equal(130, points.Length);
        Assert.Equal(new Vector2(129f, 1f), points[^1]);
    }

    [Theory]
    [InlineData(false, 64, 64, 4)]
    [InlineData(true, 64, 64, 5)]
    public void LongSpacePolylineIsSplitIntoPiecesThatShareAPoint(bool closed, int first, int second, int third)
    {
        var batch = new ShapeBatch();
        var points = new Vector3[130];

        for (var i = 0; i < points.Length; i++) points[i] = new Vector3(i, i % 2, i * 0.5f);

        batch.DrawPixelPolyline(points, 2f, Color.White, closed);

        Assert.Equal([first, second, third], batch.Instances.Select(instance => instance.Count));
        Assert.Equal(first + second + third, batch.SpacePoints.Count);

        // A piece starts on the point the one before it ended on
        Assert.Equal(batch.SpacePoints[first - 1], batch.SpacePoints[first]);
    }

    [Fact]
    public void DiscardedBatchIsEmptyAndAnswersNoPick()
    {
        var batch = new ShapeBatch { Tag = "disc" };

        batch.DrawSolidCircle(Vector2.Zero, 1f, Color.White);
        batch.LastView = new ShapeView(Matrix.Identity, Matrix.Identity, new Vector2(800f, 600f), 300f, 1f, Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ);
        batch.Reset();

        Assert.True(batch.TryPick(new Vector2(0.5f, 0.5f), out _));

        batch.DrawSolidCircle(Vector2.Zero, 1f, Color.White);
        batch.Discard();

        Assert.Equal(0, batch.Count);
        Assert.False(batch.CanPick);
        Assert.False(batch.TryPick(new Vector2(0.5f, 0.5f), out _));
    }
}