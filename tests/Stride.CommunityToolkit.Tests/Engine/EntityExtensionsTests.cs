using Stride.CommunityToolkit.Engine;
using Stride.Engine;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Engine;

/// <summary>
/// The component helpers on <see cref="EntityExtensions"/>: the two that forward to Stride's own
/// <c>Get</c> and <c>GetAll</c>, and the <c>Try</c> form built on the first.
/// </summary>
public class EntityExtensionsTests
{
    [Fact]
    public void TryGetComponent_FindsTheFirstComponentOfTheType()
    {
        var camera = new CameraComponent();
        var entity = new Entity("Rig") { camera, new LightComponent() };

        Assert.True(entity.TryGetComponent<CameraComponent>(out var found));
        Assert.Same(camera, found);
    }

    [Fact]
    public void TryGetComponent_IsFalseWithNoComponentWhenTheEntityHasNone()
    {
        var entity = new Entity("Empty");

        Assert.False(entity.TryGetComponent<CameraComponent>(out var found));
        Assert.Null(found);
    }

    [Fact]
    public void GetComponent_AndGetComponents_AreStridesOwn()
    {
        var first = new LightComponent();
        var second = new LightComponent();
        var entity = new Entity("Lights") { first, second };

        Assert.Same(entity.Get<LightComponent>(), entity.GetComponent<LightComponent>());
        Assert.Equal([first, second], entity.GetComponents<LightComponent>());
        Assert.Null(entity.GetComponent<CameraComponent>());
    }
}