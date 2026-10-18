using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering;
using Stride.CommunityToolkit.Rendering.Gizmos;
using Stride.CommunityToolkit.Tests.Engine;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Rendering.Compositing;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Rendering;

/// <summary>
/// The orientation gizmo's projection, which is the part that can be wrong quietly: an axis drawn
/// the wrong way round or in the wrong order would still look like a gizmo.
/// </summary>
public class OrientationGizmoMathTests
{
    private const float Tolerance = 1e-5f;

    [Fact]
    public void AnUnrotatedCameraSeesXRightYUpAndZTowardsIt()
    {
        var ends = Project(Matrix.Identity);

        var x = Positive(ends, 0);
        var y = Positive(ends, 1);
        var z = Positive(ends, 2);

        Assert.Equal(1f, x.Direction.X, Tolerance);
        Assert.Equal(0f, x.Direction.Y, Tolerance);
        Assert.Equal(0f, x.Depth, Tolerance);

        // Screen Y points down, so up on the screen is a negative Y
        Assert.Equal(0f, y.Direction.X, Tolerance);
        Assert.Equal(-1f, y.Direction.Y, Tolerance);

        // The camera looks down -Z, so +Z comes straight at it: no screen offset, full depth
        Assert.Equal(0f, z.Direction.Length(), Tolerance);
        Assert.Equal(1f, z.Depth, Tolerance);
    }

    [Fact]
    public void ACameraOnThePositiveXAxisSeesXComingAtItAndZToTheLeft()
    {
        var view = Matrix.LookAtRH(new Vector3(10f, 0f, 0f), Vector3.Zero, Vector3.UnitY);

        var ends = Project(view);

        Assert.Equal(1f, Positive(ends, 0).Depth, Tolerance);
        Assert.Equal(-1f, Positive(ends, 2).Direction.X, Tolerance);
        Assert.Equal(0f, Positive(ends, 2).Depth, Tolerance);
        Assert.Equal(-1f, Positive(ends, 1).Direction.Y, Tolerance);
    }

    [Fact]
    public void TheNegativeHalfIsTheMirrorOfThePositiveOne()
    {
        var view = Matrix.LookAtRH(new Vector3(4f, 3f, 5f), Vector3.Zero, Vector3.UnitY);

        var ends = Project(view);

        for (var axis = 0; axis < 3; axis++)
        {
            var positive = Positive(ends, axis);
            var negative = Assert.Single(ends, end => end.Axis == axis && !end.Positive);

            Assert.Equal(-positive.Direction.X, negative.Direction.X, Tolerance);
            Assert.Equal(-positive.Direction.Y, negative.Direction.Y, Tolerance);
            Assert.Equal(-positive.Depth, negative.Depth, Tolerance);
        }
    }

    [Theory]
    [InlineData(1f, 0f, 0f)]
    [InlineData(0f, 0.001f, 10f)]
    [InlineData(-3f, 2f, -7f)]
    [InlineData(0.5f, 8f, 0.5f)]
    public void EndsComeFarthestFirst(float x, float y, float z)
    {
        var view = Matrix.LookAtRH(new Vector3(x, y, z), Vector3.Zero, Vector3.UnitY);

        var ends = Project(view);

        for (var i = 1; i < ends.Length; i++)
        {
            Assert.True(ends[i - 1].Depth <= ends[i].Depth, $"End {i} is nearer than the one before it.");
        }

        Assert.Equal(6, ends.Length);
        Assert.Equal(3, ends.Count(end => end.Positive));
    }

    private static OrientationGizmoEnd[] Project(Matrix view)
    {
        var ends = new OrientationGizmoEnd[OrientationGizmoMath.EndCount];

        OrientationGizmoMath.Project(in view, ends);

        return ends;
    }

    private static OrientationGizmoEnd Positive(OrientationGizmoEnd[] ends, int axis)
        => Assert.Single(ends, end => end.Axis == axis && end.Positive);
}

/// <summary>
/// <c>AddOrientationGizmo</c> against a headless game: one renderer in the compositor, placed where
/// the call says.
/// </summary>
[Collection(GameExtensionsRunTests.Name)]
public class OrientationGizmoGameTests
{
    [Fact]
    public void AddOrientationGizmoAddsOneRendererAndPlacesIt()
    {
        using var game = new Game();

        var sameInstance = false;
        var renderers = -1;
        var anchor = ScreenAnchor.TopLeft;

        game.Run(new SceneSetup3D { Camera = null, Lighting = null, Skybox = null, Ground = null }, start: _ =>
        {
            var first = game.AddOrientationGizmo(ScreenAnchor.BottomRight);
            var second = game.AddOrientationGizmo(ScreenAnchor.BottomRight);

            sameInstance = ReferenceEquals(first, second);
            anchor = second.Anchor;
            renderers = ((SceneRendererCollection)game.SceneSystem.GraphicsCompositor.Game).Children.OfType<OrientationGizmoRenderer>().Count();

            game.Exit();
        }, context: new GameContextHeadless());

        Assert.True(sameInstance);
        Assert.Equal(1, renderers);
        Assert.Equal(ScreenAnchor.BottomRight, anchor);
    }
}