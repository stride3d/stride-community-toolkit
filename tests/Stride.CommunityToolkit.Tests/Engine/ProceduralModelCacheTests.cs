using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Rendering;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Engine;

/// <summary>
/// <see cref="ProceduralModelCache"/>, the store behind <c>GetOrCreateModel</c>: a shape and a material are one key
/// however many times the shape is written, a material tells two models apart, and the build runs once per key.
/// </summary>
public class ProceduralModelCacheTests
{
    [Fact]
    public void GetOrAdd_SameShapeWrittenTwice_IsOneModelBuiltOnce()
    {
        var cache = new ProceduralModelCache();
        var builds = 0;

        var first = cache.GetOrAdd(Primitive3D.Sphere(0.5f), null, () => { builds++; return new Model(); });
        var second = cache.GetOrAdd(Primitive3D.Sphere(0.5f), null, () => { builds++; return new Model(); });

        Assert.Same(first, second);
        Assert.Equal(1, builds);
    }

    [Fact]
    public void GetOrAdd_DifferentDimensions_AreDifferentModels()
    {
        var cache = new ProceduralModelCache();

        var small = cache.GetOrAdd(Primitive3D.Sphere(0.5f), null, () => new Model());
        var large = cache.GetOrAdd(Primitive3D.Sphere(1f), null, () => new Model());

        Assert.NotSame(small, large);
    }

    [Fact]
    public void GetOrAdd_SameShapeDifferentMaterial_AreDifferentModels()
    {
        var cache = new ProceduralModelCache();
        var cube = Primitive3D.Box(new Vector3(1f, 2f, 3f));

        var plain = cache.GetOrAdd(cube, null, () => new Model());
        var painted = cache.GetOrAdd(cube, new Material(), () => new Model());

        Assert.NotSame(plain, painted);
    }

    [Fact]
    public void GetOrAdd_A2DShapeAndA3DShape_AreDifferentKeys()
    {
        var cache = new ProceduralModelCache();

        var disc = cache.GetOrAdd(Primitive2D.Circle(0.5f), null, () => new Model());
        var ball = cache.GetOrAdd(Primitive3D.Sphere(0.5f), null, () => new Model());

        Assert.NotSame(disc, ball);
    }
}