using Stride.CommunityToolkit.Rendering.Text;
using Stride.CommunityToolkit.Shapes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Rendering;

/// <summary>
/// The reference grid's arithmetic: which step it draws lines at, which lines fall inside a view,
/// how a value is written, and the trip between the world and the screen that places the numbers
/// and reads the cursor.
/// </summary>
public class ReferenceGridMathTests
{
    [Theory]
    [InlineData(0.8f, 1f)]
    [InlineData(1f, 1f)]
    [InlineData(1.2f, 2f)]
    [InlineData(2f, 2f)]
    [InlineData(2.1f, 5f)]
    [InlineData(5f, 5f)]
    [InlineData(7f, 10f)]
    [InlineData(0.03f, 0.05f)]
    [InlineData(130f, 200f)]
    public void NiceStepIsTheNextOneTwoOrFive(float raw, float expected)
        => Assert.Equal(expected, GridSteps.Nice(raw), 4);

    [Theory]
    [InlineData(0f)]
    [InlineData(-3f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void NiceStepOfNothingUsableIsOne(float raw)
        => Assert.Equal(1f, GridSteps.Nice(raw));

    [Theory]
    [InlineData(1f, 5)]    // minor 0.2
    [InlineData(2f, 4)]    // minor 0.5
    [InlineData(5f, 5)]    // minor 1
    [InlineData(0.2f, 4)]  // minor 0.05
    [InlineData(50f, 5)]   // minor 10
    public void DivisionsGiveRoundMinorSteps(float major, int expected)
        => Assert.Equal(expected, GridSteps.Divisions(major));

    [Fact]
    public void RangeHoldsTheLinesInsideAndIncludesBothEnds()
    {
        Assert.Equal((-2, 3), GridSteps.Range(-4.5f, 7.9f, 2f));
        Assert.Equal((-2, 2), GridSteps.Range(-4f, 4f, 2f));
        Assert.Equal((1, 0), GridSteps.Range(0.2f, 0.8f, 1f));
        Assert.Equal((0, -1), GridSteps.Range(0f, 10f, 0f));
    }

    [Theory]
    [InlineData(4f, 2f, "4")]
    [InlineData(-0.5f, 0.5f, "-0.5")]
    [InlineData(0.25f, 0.05f, "0.25")]
    [InlineData(-0.0001f, 1f, "0")]
    [InlineData(1200f, 100f, "1200")]
    public void ValuesAreWrittenWithTheDecimalsTheirStepNeeds(float value, float step, string expected)
        => Assert.Equal(expected, GridSteps.Format(value, step));

    [Fact]
    public void OrthographicViewPutsTheWorldOriginInTheMiddle()
    {
        var view = Views.Orthographic();

        Assert.True(view.Project(Vector3.Zero, out var origin));
        Assert.Equal(640f, origin.X, 2);
        Assert.Equal(360f, origin.Y, 2);

        // World Y is up, screen Y is down
        Assert.True(view.Project(new Vector3(0f, 1f, 0f), out var above));
        Assert.Equal(360f - 72f, above.Y, 2);
    }

    [Fact]
    public void ScreenPositionComesBackAsTheWorldPointItShows()
    {
        var view = Views.Orthographic();

        Assert.True(view.OnPlaneZ(new Vector2(0.5f, 0.5f), out var middle));
        Assert.Equal(0f, middle.X, 3);
        Assert.Equal(0f, middle.Y, 3);

        Assert.True(view.OnPlaneZ(Vector2.Zero, out var topLeft));
        Assert.Equal(-10f * 1280f / 720f * 0.5f, topLeft.X, 2);
        Assert.Equal(5f, topLeft.Y, 2);
    }

    [Fact]
    public void WorldPerPixelMatchesTheOrthographicHeight()
        => Assert.Equal(10f / 720f, Views.Orthographic().WorldPerPixel(Vector3.Zero), 4);

    [Fact]
    public void WorldPerPixelBehindTheCameraGivesAStepOfOneUnit()
    {
        var scale = Views.Perspective().WorldPerPixel(new Vector3(12f, 12f, 12f));

        Assert.Equal(1f, GridSteps.Nice(GridSteps.TargetMajorPixels * scale));
    }

    [Fact]
    public void PerspectiveViewFindsTheGroundUnderTheMiddleOfTheScreen()
    {
        var view = Views.Perspective();

        Assert.True(view.OnPlaneY(new Vector2(0.5f, 0.5f), out var ground));
        Assert.Equal(0f, ground.X, 2);
        Assert.Equal(0f, ground.Y, 2);

        // A point behind the camera has no place on the screen
        Assert.False(view.Project(new Vector3(12f, 12f, 12f), out _));
    }
}

/// <summary>
/// What the grid draws in each of its states. No graphics device: a canvas is given the grid, two
/// batches and a view the way the grid's script gives them, and the batches and the label entities
/// are read afterwards.
/// </summary>
public class ReferenceGridTests
{
    private static readonly Vector2 Middle = new(0.5f, 0.5f);

    [Fact]
    public void CycleGoesFromWorldToScreenToHiddenAndBack()
    {
        var grid = new ReferenceGrid();

        Assert.True(grid.Visible);
        Assert.Equal(GridSpace.World, grid.Space);

        grid.Cycle();
        Assert.True(grid.Visible);
        Assert.Equal(GridSpace.Screen, grid.Space);

        grid.Cycle();
        Assert.False(grid.Visible);

        grid.Cycle();
        Assert.True(grid.Visible);
        Assert.Equal(GridSpace.World, grid.Space);
    }

    [Fact]
    public void WorldGridUnderA2DCameraDrawsIntoTheWorldBatchAndNumbersBothAxes()
    {
        var (grid, canvas) = Grid();

        canvas.Draw(Views.Orthographic(), Middle);

        Assert.NotEmpty(canvas.World.Instances);
        Assert.Empty(canvas.Overlay.Instances);

        // A view 10 units high in 720 pixels: a hundred pixels are 1.4 units, so the step is 2
        var labels = Labels(canvas).Select(label => label.Text).ToList();

        Assert.Contains("2", labels);
        Assert.Contains("-8", labels);
        Assert.Contains("X", labels);
        Assert.Contains("Y", labels);
        Assert.DoesNotContain("Z", labels);
        Assert.DoesNotContain("1", labels);
    }

    [Fact]
    public void WorldGridUnderA3DCameraLiesOnTheGroundAndNamesThreeAxes()
    {
        var (grid, canvas) = Grid();

        canvas.Draw(Views.Perspective(), Middle);

        Assert.NotEmpty(canvas.World.Instances);
        Assert.Empty(canvas.Overlay.Instances);

        var labels = Labels(canvas).Select(label => label.Text).ToList();

        Assert.Contains("X", labels);
        Assert.Contains("Y", labels);
        Assert.Contains("Z", labels);
    }

    [Fact]
    public void ScreenGridDrawsIntoTheOverlayAndCountsPixelsFromTheTopLeft()
    {
        var (grid, canvas) = Grid();

        grid.Space = GridSpace.Screen;
        canvas.Draw(Views.Orthographic(), Middle);

        Assert.Empty(canvas.World.Instances);
        Assert.NotEmpty(canvas.Overlay.Instances);

        var labels = Labels(canvas).ToList();

        // 1280 by 720: the X axis is numbered to 1200, the Y axis to 700
        Assert.Contains(labels, label => label.Text == "1200");
        Assert.DoesNotContain(labels, label => label.Text == "1300");
        Assert.Equal(2, labels.Count(label => label.Text == "700"));
        Assert.Single(labels, label => label.Text == "800");

        // The number 100 on the X axis sits just right of the line at 100 pixels, near the top edge
        var hundred = labels.First(label => label.Text == "100");

        Assert.InRange(hundred.ScreenPosition.X, 100f, 110f);
        Assert.InRange(hundred.ScreenPosition.Y, 0f, 20f);
    }

    [Fact]
    public void CursorIsWrittenInBothSpaces()
    {
        var (grid, canvas) = Grid();

        grid.ShowNumbers = false;
        canvas.Draw(Views.Orthographic(), new Vector2(0.75f, 0.25f));

        // The view is 10 units high and 17.78 wide: three quarters across and a quarter down
        var cursor = Assert.Single(Labels(canvas));

        Assert.Equal("screen 960, 180\nworld 4.44, 2.50", cursor.Text);
    }

    [Fact]
    public void NothingIsDrawnWhileHidden_AndLabelsOfTheLastFrameAreHidden()
    {
        var (grid, canvas) = Grid();

        canvas.Draw(Views.Orthographic(), Middle);
        Assert.NotEmpty(Labels(canvas));

        canvas.World.Instances.Clear();
        grid.Visible = false;
        canvas.Draw(Views.Orthographic(), Middle);

        Assert.Empty(canvas.World.Instances);
        Assert.Empty(canvas.Overlay.Instances);
        Assert.Empty(Labels(canvas));
    }

    [Fact]
    public void NumbersAndCursorCanBeTurnedOff()
    {
        var (grid, canvas) = Grid();

        grid.ShowNumbers = false;
        grid.ShowCursor = false;
        canvas.Draw(Views.Orthographic(), Middle);

        Assert.NotEmpty(canvas.World.Instances);
        Assert.Empty(Labels(canvas));
    }

    [Fact]
    public void AFixedStepIsUsedAsGiven()
    {
        var (grid, canvas) = Grid();

        grid.MajorStep = 1f;
        canvas.Draw(Views.Orthographic(), Middle);

        Assert.Contains(Labels(canvas), label => label.Text == "1");
        Assert.Contains(Labels(canvas), label => label.Text == "3");
    }

    [Fact]
    public void PannedAwayFromTheOrigin_TheNumbersStayInsideTheWindow()
    {
        var (grid, canvas) = Grid();

        // The camera looks at (100, 100): both axes are far outside the view
        var viewMatrix = Matrix.LookAtRH(new Vector3(100f, 100f, 50f), new Vector3(100f, 100f, 0f), Vector3.UnitY);
        var projection = Matrix.OrthoRH(10f * Views.Size.X / Views.Size.Y, 10f, 0.1f, 1000f);

        grid.ShowCursor = false;
        canvas.Draw(new GridView(viewMatrix * projection, Views.Size, orthographic: true), Middle);

        var labels = Labels(canvas).ToList();

        Assert.Contains(labels, label => label.Text == "100");
        Assert.All(labels, label =>
        {
            Assert.InRange(label.ScreenPosition.X, 0f, Views.Size.X);
            Assert.InRange(label.ScreenPosition.Y, 0f, Views.Size.Y);
        });
    }

    /// <summary>A grid on an entity and a canvas with two batches of its own, as the grid's script sets them up.</summary>
    private static (ReferenceGrid Grid, GridCanvas Canvas) Grid()
    {
        var grid = new ReferenceGrid();
        var entity = new Entity("Reference grid") { grid };
        var overlay = new ShapeBatch { ScreenSizeSource = () => Views.Size };

        return (grid, new GridCanvas(grid, new ShapeBatch(), overlay, entity));
    }

    /// <summary>The labels shown this frame: the text components under the grid's entity that are visible.</summary>
    private static List<EntityTextComponent> Labels(GridCanvas canvas)
        => [.. canvas.Settings.Entity.GetChildren().Select(child => child.Get<EntityTextComponent>()).Where(label => label is { IsVisible: true })];
}

/// <summary>The two cameras the grid tests look through: a 2D scene's and a 3D scene's.</summary>
internal static class Views
{
    internal static readonly Vector2 Size = new(1280f, 720f);

    /// <summary>At (0, 0, 50) looking at the origin, 10 world units high: the base 2D scene's camera.</summary>
    internal static GridView Orthographic()
    {
        var viewMatrix = Matrix.LookAtRH(new Vector3(0f, 0f, 50f), Vector3.Zero, Vector3.UnitY);
        var projection = Matrix.OrthoRH(10f * Size.X / Size.Y, 10f, 0.1f, 1000f);

        return new GridView(viewMatrix * projection, Size, orthographic: true);
    }

    /// <summary>At (6, 6, 6) looking at the origin: the base 3D scene's camera.</summary>
    internal static GridView Perspective()
    {
        var viewMatrix = Matrix.LookAtRH(new Vector3(6f, 6f, 6f), Vector3.Zero, Vector3.UnitY);
        var projection = Matrix.PerspectiveFovRH(MathUtil.DegreesToRadians(45f), Size.X / Size.Y, 0.1f, 1000f);

        return new GridView(viewMatrix * projection, Size, orthographic: false);
    }
}