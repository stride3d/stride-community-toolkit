using Box2D.NET;
using Stride.CommunityToolkit.Box2D;
using Stride.CommunityToolkit.Tests.Engine;
using Stride.Core.Mathematics;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Box2D;

/// <summary>
/// Pins the shape casts and the filtered overlap on <see cref="PhysicsQueries2D"/> against a real
/// world: a circle sweep stops at the near face with the right fraction and normal, the nearest
/// of two hits wins, category bits exclude, and a segment sweeps too.
/// </summary>
/// <remarks>Box2D worlds are process-wide state, so this runs in the serialised collection.</remarks>
[Collection(GameExtensionsRunTests.Name)]
public class ShapeCastTests
{
    private static readonly B2QueryFilter Everything = new(ulong.MaxValue, ulong.MaxValue);

    [Fact]
    public void CastCircleClosest_StopsAtTheNearFace()
    {
        using var simulation = new Box2DSimulation();
        var box = StaticBox(simulation, new Vector2(5, 0), categoryBits: 1);

        var hit = simulation.CastCircleClosest(Vector2.Zero, 0.5f, new Vector2(10, 0), Everything);

        Assert.NotNull(hit);
        Assert.Equal(box, hit.Value.BodyId);
        Assert.Equal(0.35f, hit.Value.Fraction, 2);          // (4 - 0.5) / 10: the face is at x = 4, give or take the 5 mm skin
        Assert.Equal(-1f, hit.Value.Normal.X, 3);
        Assert.Equal(4f, hit.Value.Point.X, 2);
    }

    [Fact]
    public void CastCircleClosest_PrefersTheNearerOfTwoHits()
    {
        using var simulation = new Box2DSimulation();
        var far = StaticBox(simulation, new Vector2(8, 0), categoryBits: 1);
        var near = StaticBox(simulation, new Vector2(5, 0), categoryBits: 1);

        var hit = simulation.CastCircleClosest(Vector2.Zero, 0.5f, new Vector2(10, 0), Everything);

        Assert.NotNull(hit);
        Assert.Equal(near, hit.Value.BodyId);
        Assert.NotEqual(far, hit.Value.BodyId);
    }

    [Fact]
    public void CastCircleClosest_MissesWhenTheFilterExcludesTheShape()
    {
        using var simulation = new Box2DSimulation();
        StaticBox(simulation, new Vector2(5, 0), categoryBits: 2);

        var onlyCategoryOne = new B2QueryFilter(ulong.MaxValue, 1);

        Assert.Null(simulation.CastCircleClosest(Vector2.Zero, 0.5f, new Vector2(10, 0), onlyCategoryOne));
        Assert.NotNull(simulation.CastCircleClosest(Vector2.Zero, 0.5f, new Vector2(10, 0), Everything));
    }

    [Fact]
    public void CastSegmentClosest_SweepsTheSegment()
    {
        using var simulation = new Box2DSimulation();
        StaticBox(simulation, new Vector2(5, 0), categoryBits: 1);

        var hit = simulation.CastSegmentClosest(new Vector2(0, -0.5f), new Vector2(0, 0.5f), new Vector2(10, 0), Everything);

        Assert.NotNull(hit);
        Assert.Equal(0.4f, hit.Value.Fraction, 2);           // 4 / 10: the segment has no radius, the polygon a 5 mm skin
    }

    [Fact]
    public void OverlapCircle_WithFilter_ExcludesByCategory()
    {
        using var simulation = new Box2DSimulation();
        var wanted = StaticBox(simulation, new Vector2(0, 0), categoryBits: 1);
        var unwanted = StaticBox(simulation, new Vector2(0.5f, 0), categoryBits: 2);

        var bodies = simulation.OverlapCircle(Vector2.Zero, 2f, new B2QueryFilter(ulong.MaxValue, 1));

        Assert.Contains(wanted, bodies);
        Assert.DoesNotContain(unwanted, bodies);
        Assert.Equal(2, simulation.OverlapCircle(Vector2.Zero, 2f).Count);
    }

    // A static 2 x 2 box at a position, with the category bits given.
    private static B2BodyId StaticBox(Box2DSimulation simulation, Vector2 position, ulong categoryBits)
        => Box2DFixtures.StaticBox(simulation, position, halfWidth: 1f, halfHeight: 1f, categoryBits);
}