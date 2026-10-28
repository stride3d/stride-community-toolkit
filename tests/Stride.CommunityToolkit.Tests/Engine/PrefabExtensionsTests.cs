using Stride.CommunityToolkit.Engine;
using Stride.Core.Mathematics;
using Stride.Engine;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Engine;

/// <summary>
/// Prefab instantiation with a transform: the transform reaches every entity, and the single-entity
/// variant insists on exactly one.
/// </summary>
public class PrefabExtensionsTests
{
    private static Prefab TwoEntityPrefab()
    {
        var prefab = new Prefab();

        prefab.Entities.Add(new Entity("First") { Transform = { Position = new Vector3(1, 0, 0) } });
        prefab.Entities.Add(new Entity("Second") { Transform = { Position = new Vector3(0, 1, 0) } });

        return prefab;
    }

    [Fact]
    public void Instantiate_AppliesTheTransform_ToEveryEntity()
    {
        var instances = TwoEntityPrefab().Instantiate(new Vector3(10, 0, 0), Quaternion.Identity, new Vector3(2));

        Assert.Equal(2, instances.Count);
        Assert.Equal(new Vector3(12, 0, 0), instances[0].Transform.Position);
        Assert.Equal(new Vector3(10, 2, 0), instances[1].Transform.Position);
        Assert.All(instances, instance => Assert.Equal(new Vector3(2), instance.Transform.Scale));
        Assert.All(instances, instance => Assert.True(instance.Transform.UseTRS));
    }

    [Fact]
    public void Instantiate_WithEulerAngles_RotatesAroundTheOrigin()
    {
        var instances = TwoEntityPrefab().Instantiate(Vector3.Zero, new Vector3(0, MathUtil.PiOverTwo, 0));

        // A quarter turn about Y takes +X to -Z and leaves +Y where it is
        Assert.Equal(0f, instances[0].Transform.Position.X, 5);
        Assert.Equal(-1f, instances[0].Transform.Position.Z, 5);
        Assert.Equal(new Vector3(0, 1, 0), instances[1].Transform.Position);
    }

    [Fact]
    public void InstantiateSingle_ReturnsTheOneEntity_Transformed()
    {
        var prefab = new Prefab();

        prefab.Entities.Add(new Entity("Only") { Transform = { Position = new Vector3(1, 0, 0) } });

        var instance = prefab.InstantiateSingle(new Vector3(0, 5, 0));

        Assert.Equal("Only", instance.Name);
        Assert.Equal(new Vector3(1, 5, 0), instance.Transform.Position);
    }

    [Fact]
    public void InstantiateSingle_Throws_UnlessThePrefabHasExactlyOneEntity()
    {
        Assert.Throws<InvalidOperationException>(() => new Prefab().InstantiateSingle());
        Assert.Throws<InvalidOperationException>(() => TwoEntityPrefab().InstantiateSingle(Vector3.Zero));
        Assert.Throws<ArgumentNullException>(() => ((Prefab)null!).InstantiateSingle());
    }
}