using Stride.CommunityToolkit.Shapes;
using Stride.Core.Mathematics;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Rendering;

/// <summary>
/// The batch answers "which shape is under this point" from what it drew last frame, with the
/// shader's own distance functions on the CPU. Each test draws through a batch, hands the frame
/// over the way the render feature does, gives the batch a view, and picks at the screen position
/// a known world point projects to - so every test is the round trip a mouse makes.
/// </summary>
public class ShapePickTests
{
    private static readonly Vector2 ViewSize = new(800f, 600f);

    [Fact]
    public void DiscOnTheGroundIsPickedInsideAndNotOutside()
    {
        var (batch, view) = Scene(new Vector3(0f, 8f, 8f), Vector3.Zero);

        batch.Tag = "disc";
        batch.DrawDisc(Vector3.Zero, Vector3.UnitY, 2f, Color.White);
        Drawn(batch, view);

        Assert.True(batch.TryPick(ScreenOf(new Vector3(1f, 0f, 0.5f), view), out var hit));
        Assert.Equal("disc", hit.Tag);
        Assert.True(hit.Distance < 0f);
        Assert.Equal(1f, hit.Local.X, 2);
        Assert.False(batch.TryPick(ScreenOf(new Vector3(3f, 0f, 0f), view), out _));
    }

    [Fact]
    public void UntaggedShapeIsNeverPicked()
    {
        var (batch, view) = Scene(new Vector3(0f, 8f, 8f), Vector3.Zero);

        batch.DrawDisc(Vector3.Zero, Vector3.UnitY, 2f, Color.White);
        Drawn(batch, view);

        Assert.False(batch.TryPick(ScreenOf(Vector3.Zero, view), out _));
    }

    [Fact]
    public void RingPicksItsBandAndNotItsHole()
    {
        var (batch, view) = Scene(new Vector3(0f, 8f, 8f), Vector3.Zero);

        batch.Tag = "ring";
        batch.BorderWidth = 3f;
        batch.DrawRing(Vector3.Zero, Vector3.UnitY, 2f, Color.White);
        Drawn(batch, view);

        Assert.True(batch.TryPick(ScreenOf(new Vector3(2f, 0f, 0f), view), out var hit));
        Assert.Equal("ring", hit.Tag);
        Assert.False(batch.TryPick(ScreenOf(Vector3.Zero, view), out _));
    }

    [Fact]
    public void SectorRespectsItsCut()
    {
        var (batch, view) = Scene(new Vector3(0f, 0f, 10f), Vector3.Zero);

        batch.Tag = "sector";
        batch.DrawSector(Vector2.Zero, 2f, 0f, MathF.PI * 0.5f, Color.White);
        Drawn(batch, view);

        Assert.True(batch.TryPick(ScreenOf(new Vector3(0.5f, 0.5f, 0f), view), out _));
        Assert.False(batch.TryPick(ScreenOf(new Vector3(-0.5f, 0.5f, 0f), view), out _));
    }

    [Fact]
    public void RoundedRectangleMissesItsCutCorner()
    {
        var (batch, view) = Scene(new Vector3(0f, 0f, 10f), Vector3.Zero);

        batch.Tag = "rectangle";
        batch.BorderWidth = 0f;
        batch.DrawRectangle(Vector3.Zero, Vector3.UnitX, Vector3.UnitY, new Vector2(4f, 2f), Color.White, cornerRadius: 0.5f);
        Drawn(batch, view);

        Assert.True(batch.TryPick(ScreenOf(new Vector3(1.9f, 0f, 0f), view), out var hit));
        Assert.Equal(1.9f, hit.Local.X, 2);
        Assert.False(batch.TryPick(ScreenOf(new Vector3(1.9f, 0.9f, 0f), view), out _));
    }

    [Fact]
    public void ScreenRectangleIsPickedInPixels()
    {
        var (batch, view) = Scene(new Vector3(0f, 0f, 10f), Vector3.Zero);

        batch.Screen = true;
        batch.Tag = "button";
        batch.DrawRectangle(new Vector3(100f, 50f, 0f), Vector3.UnitX, Vector3.UnitY, new Vector2(40f, 20f), Color.White);
        Drawn(batch, view);

        Assert.True(batch.TryPick(new Vector2(110f / ViewSize.X, 55f / ViewSize.Y), out var hit));
        Assert.Equal("button", hit.Tag);
        Assert.Equal(-1f, hit.Depth);
        Assert.Equal(10f, hit.Local.X, 2);
        Assert.False(batch.TryPick(new Vector2(200f / ViewSize.X, 55f / ViewSize.Y), out _));
    }

    [Fact]
    public void ThinLineNeedsSlackToBeHit()
    {
        var (batch, view) = Scene(new Vector3(0f, 0f, 10f), Vector3.Zero);

        batch.Tag = "line";
        batch.DrawPixelPolyline([new Vector2(-2f, 0f), new Vector2(2f, 0f)], 2f, Color.White);
        Drawn(batch, view);

        // About four pixels off the line at this distance
        var beside = ScreenOf(new Vector3(0f, 0.05f, 0f), view);

        Assert.True(batch.TryPick(ScreenOf(Vector3.Zero, view), out _));
        Assert.False(batch.TryPick(beside, out _));
        Assert.True(batch.TryPick(beside, out _, slackPixels: 6f));
    }

    [Fact]
    public void NearerShapeWinsAndThenTheLastDrawn()
    {
        var (batch, view) = Scene(new Vector3(0f, 0f, 10f), Vector3.Zero);

        batch.Tag = "far";
        batch.DrawDisc(Vector3.Zero, Vector3.UnitZ, 2f, Color.White);
        batch.Tag = "near";
        batch.DrawDisc(new Vector3(0f, 0f, 3f), Vector3.UnitZ, 2f, Color.White);
        batch.Tag = "first";
        batch.DrawSolidCircle(Vector2.Zero, 1f, Color.White);
        batch.Tag = "second";
        batch.DrawSolidCircle(Vector2.Zero, 1f, Color.White);
        Drawn(batch, view);

        // The eye is at z = 10, so the disc at z = 3 is in front of everything at z = 0
        Assert.True(batch.TryPick(ScreenOf(new Vector3(0f, 0f, 3f), view), out var hit));
        Assert.Equal("near", hit.Tag);

        var all = batch.PickAll(ScreenOf(new Vector3(0f, 0f, 3f), view));

        Assert.Equal(["near", "second", "first", "far"], all.Select(h => h.Tag).ToArray());
    }

    [Fact]
    public void BillboardFacesTheCamera()
    {
        var (batch, view) = Scene(new Vector3(0f, 0f, 10f), Vector3.Zero);

        batch.Tag = "billboard";
        batch.DrawBillboardCircle(Vector3.Zero, 1f, Color.White);
        Drawn(batch, view);

        Assert.True(batch.TryPick(ScreenOf(new Vector3(0.5f, 0f, 0f), view), out _));
        Assert.False(batch.TryPick(ScreenOf(new Vector3(1.5f, 0f, 0f), view), out _));
    }

    [Fact]
    public void PixelDiscIsTheSameSizeOnScreenAtAnyDistance()
    {
        var (batch, view) = Scene(new Vector3(0f, 0f, 10f), Vector3.Zero);

        batch.Tag = "marker";
        batch.BorderWidth = 0f;
        batch.DrawPixelDisc(new Vector3(0f, 0f, -20f), 10f, Color.White);
        Drawn(batch, view);

        var centre = ScreenOf(new Vector3(0f, 0f, -20f), view);

        Assert.True(batch.TryPick(centre + new Vector2(6f / ViewSize.X, 0f), out _));
        Assert.False(batch.TryPick(centre + new Vector2(14f / ViewSize.X, 0f), out _));
    }

    [Fact]
    public void SpaceStrokeIsPickedOnItsProjection()
    {
        var (batch, view) = Scene(new Vector3(0f, 0f, 10f), Vector3.Zero);

        batch.Tag = "rope";
        batch.DrawPixelPolyline([new Vector3(-2f, 0f, 0f), new Vector3(2f, 0f, 2f)], 3f, Color.White);
        Drawn(batch, view);

        Assert.True(batch.TryPick(ScreenOf(new Vector3(0f, 0f, 1f), view), out var hit));
        Assert.Equal("rope", hit.Tag);
        Assert.Equal(1f, hit.Point.Z, 1);
        Assert.False(batch.TryPick(ScreenOf(new Vector3(0f, 1f, 1f), view), out _));
    }

    [Fact]
    public void RecordsLastExactlyOneFrame()
    {
        var (batch, view) = Scene(new Vector3(0f, 0f, 10f), Vector3.Zero);

        batch.Tag = "disc";
        batch.DrawSolidCircle(Vector2.Zero, 1f, Color.White);
        Drawn(batch, view);

        Assert.True(batch.TryPick(ScreenOf(Vector3.Zero, view), out _));

        // A frame with nothing drawn: the previous frame's shapes are gone
        batch.Reset();

        Assert.False(batch.TryPick(ScreenOf(Vector3.Zero, view), out _));
    }

    [Fact]
    public void NothingIsPickedBeforeTheFirstDraw()
    {
        var batch = new ShapeBatch { Tag = "disc" };

        batch.DrawSolidCircle(Vector2.Zero, 1f, Color.White);
        batch.Reset();

        Assert.False(batch.CanPick);
        Assert.False(batch.TryPick(new Vector2(0.5f, 0.5f), out _));
    }

    /// <summary>A batch and a perspective view from an eye towards a target, the way the render feature would describe it.</summary>
    private static (ShapeBatch Batch, ShapeView View) Scene(Vector3 eye, Vector3 target)
    {
        var viewMatrix = Matrix.LookAtRH(eye, target, Vector3.UnitY);
        var projection = Matrix.PerspectiveFovRH(MathUtil.DegreesToRadians(45f), ViewSize.X / ViewSize.Y, 0.1f, 1000f);
        var viewProjection = viewMatrix * projection;
        var viewInverse = Matrix.Invert(viewMatrix);
        var view = new ShapeView(
            viewProjection,
            Matrix.Invert(viewProjection),
            ViewSize,
            ViewSize.Y * projection.M22 * 0.5f,
            1f,
            new Vector3(viewInverse.M11, viewInverse.M12, viewInverse.M13),
            new Vector3(viewInverse.M21, viewInverse.M22, viewInverse.M23),
            viewInverse.TranslationVector);

        return (new ShapeBatch(), view);
    }

    /// <summary>What the render feature does once it has drawn the batch: records the view and hands the frame over.</summary>
    private static void Drawn(ShapeBatch batch, in ShapeView view)
    {
        batch.LastView = view;
        batch.Reset();
    }

    /// <summary>The normalised screen position a world point projects to, (0,0) top left.</summary>
    private static Vector2 ScreenOf(Vector3 world, in ShapeView view)
    {
        var clip = Vector4.Transform(new Vector4(world, 1f), view.ViewProjection);

        return new Vector2((clip.X / clip.W + 1f) * 0.5f, (1f - clip.Y / clip.W) * 0.5f);
    }
}