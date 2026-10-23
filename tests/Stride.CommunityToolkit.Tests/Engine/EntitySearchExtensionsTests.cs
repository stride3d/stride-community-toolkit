using Stride.CommunityToolkit.Engine;
using Stride.Engine;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Engine;

/// <summary>
/// The entity-tree searches: which entities each one visits, in what order, and that a disabled
/// component is skipped unless asked for.
/// </summary>
public class EntitySearchExtensionsTests
{
    /// <summary>
    /// <c>Root</c> with children <c>A</c> and <c>B</c>; <c>A</c> has children <c>A1</c> and <c>A2</c>.
    /// Every entity carries a camera, so the transform and camera searches both see the whole tree.
    /// </summary>
    private static Entity BuildTree()
    {
        var root = new Entity("Root") { new CameraComponent() };
        var a = new Entity("A") { new CameraComponent() };
        var b = new Entity("B") { new CameraComponent() };

        a.AddChild(new Entity("A1") { new CameraComponent() });
        a.AddChild(new Entity("A2") { new CameraComponent() });
        root.AddChild(a);
        root.AddChild(b);

        return root;
    }

    private static string[] Names<T>(IEnumerable<T> components) where T : EntityComponent
        => components.Select(component => component.Entity.Name).ToArray();

    [Fact]
    public void Descendants_AreDepthFirst_AndChildrenAreTheDirectOnes()
    {
        var root = BuildTree();

        Assert.Equal(["A", "A1", "A2", "B"], Names(root.GetComponentsInDescendants<TransformComponent>()));
        Assert.Equal(["Root", "A", "A1", "A2", "B"], Names(root.GetComponentsInDescendantsAndSelf<TransformComponent>()));
        Assert.Equal(["A", "B"], Names(root.GetComponentsInChildren<TransformComponent>()));
        Assert.Equal(["Root", "A", "B"], Names(root.GetComponentsInChildrenAndSelf<TransformComponent>()));
    }

    [Fact]
    public void SingleSearches_AreBreadthFirstForChildren_AndDepthFirstForDescendants()
    {
        var root = BuildTree();

        // Only the grandchildren carry a light: breadth-first reaches A1 after A and B, depth-first before B
        root.FindChild("A1")!.Add(new LightComponent());
        root.FindChild("B")!.Add(new LightComponent());

        Assert.Equal("B", root.GetComponentInChildren<LightComponent>()!.Entity.Name);
        Assert.Equal("A1", root.GetComponentInDescendantsAndSelf<LightComponent>()!.Entity.Name);
        Assert.Equal("Root", root.GetComponentInChildrenAndSelf<TransformComponent>()!.Entity.Name);
        Assert.Equal("A", root.GetComponentInChildren<TransformComponent>()!.Entity.Name);
    }

    [Fact]
    public void Parents_WalkFromTheEntityUpToTheRoot()
    {
        var root = BuildTree();
        var leaf = root.FindChild("A1")!;

        Assert.Equal(["A1", "A", "Root"], Names(leaf.GetComponentsInParent<TransformComponent>()));
        Assert.Same(leaf.Get<CameraComponent>(), leaf.GetComponentInParent<CameraComponent>());
    }

    [Fact]
    public void DisabledComponents_AreSkippedUnlessAskedFor()
    {
        var root = BuildTree();

        root.Get<CameraComponent>().Enabled = false;
        root.FindChild("A")!.Get<CameraComponent>().Enabled = false;

        Assert.Equal(["A1", "A2", "B"], Names(root.GetComponentsInDescendantsAndSelf<CameraComponent>()));
        Assert.Equal(["Root", "A", "A1", "A2", "B"], Names(root.GetComponentsInDescendantsAndSelf<CameraComponent>(includeDisabled: true)));

        Assert.Equal("B", root.GetComponentInChildren<CameraComponent>()!.Entity.Name);
        Assert.Equal("A", root.GetComponentInChildren<CameraComponent>(includeDisabled: true)!.Entity.Name);

        Assert.Null(root.FindChild("A")!.GetComponentInParent<CameraComponent>());
        Assert.Equal("A", root.FindChild("A")!.GetComponentInParent<CameraComponent>(includeDisabled: true)!.Entity.Name);

        // A component that cannot be disabled is always found
        Assert.Equal(5, root.GetComponentsInDescendantsAndSelf<TransformComponent>().Count());
    }

    [Fact]
    public void NullEntity_ThrowsAtTheCall_NotAtTheFirstStep()
    {
        Entity entity = null!;

        Assert.Throws<ArgumentNullException>(() => entity.GetComponentInChildren<CameraComponent>());
        Assert.Throws<ArgumentNullException>(() => entity.GetComponentsInDescendants<CameraComponent>());
        Assert.Throws<ArgumentNullException>(() => entity.GetComponentsInChildren<CameraComponent>());
        Assert.Throws<ArgumentNullException>(() => entity.GetComponentsInParent<CameraComponent>());
    }

    [Fact]
    public void SeveralComponentsOfOneType_OnOneEntity_AreAllFoundInOrder()
    {
        var root = BuildTree();
        var a = root.FindChild("A")!;
        var first = new LightComponent();
        var second = new LightComponent { Enabled = false };
        var third = new LightComponent();

        a.Add(first);
        a.Add(second);
        a.Add(third);

        Assert.Equal([first, third], root.GetComponentsInChildren<LightComponent>());
        Assert.Equal([first, second, third], root.GetComponentsInChildren<LightComponent>(includeDisabled: true));
        Assert.Same(first, root.GetComponentInChildren<LightComponent>());
    }

    [Fact]
    public void BaseTypeSearch_SeesEveryComponent_AndStillSkipsDisabledOnes()
    {
        var root = BuildTree();

        // Five entities, each with a transform and a camera; only the camera can be disabled
        root.Get<CameraComponent>().Enabled = false;

        Assert.Equal(9, root.GetComponentsInDescendantsAndSelf<EntityComponent>().Count());
        Assert.Equal(10, root.GetComponentsInDescendantsAndSelf<EntityComponent>(includeDisabled: true).Count());
        Assert.Equal(["Root", "Root"], Names(root.GetComponentsInParent<EntityComponent>(includeDisabled: true)));
        Assert.IsType<TransformComponent>(root.GetComponentInChildrenAndSelf<EntityComponent>());
    }
}