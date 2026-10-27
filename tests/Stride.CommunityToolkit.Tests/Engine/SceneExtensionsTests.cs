using Stride.CommunityToolkit.Engine;
using Stride.Engine;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Engine;

/// <summary>
/// Finding a camera, and an entity, by name in a scene built without a game.
/// </summary>
public class SceneExtensionsTests
{
    [Fact]
    public void FindEntity_FindsATopLevelEntityAndNotAChildUnlessRecursive()
    {
        var scene = new Scene();
        var parent = new Entity("Parent");
        var child = new Entity("Child");

        parent.AddChild(child);
        scene.Entities.Add(parent);

        Assert.Same(parent, scene.FindEntity("Parent"));
        Assert.Null(scene.FindEntity("Child"));
        Assert.Same(child, scene.FindEntity("Child", recursive: true));
        Assert.Null(scene.FindEntity("Missing", recursive: true));
    }

    [Fact]
    public void FindEntity_FromAnEntity_SearchesItsSceneAndSaysSoWhenItHasNone()
    {
        var scene = new Scene();
        var first = new Entity("First");
        var second = new Entity("Second");
        var loose = new Entity("Loose");

        scene.Entities.Add(first);
        scene.Entities.Add(second);

        Assert.Same(second, first.FindEntity("Second"));
        Assert.Same(second, first.FindEntityRecursive("Second"));

        var exception = Assert.Throws<InvalidOperationException>(() => loose.FindEntity("Second"));

        Assert.Contains("'Loose'", exception.Message);
        Assert.Contains("scene.FindEntity(\"Second\")", exception.Message);
    }

    [Fact]
    public void GetCamera_ByName_FindsACameraOnAChildEntity()
    {
        var scene = new Scene();
        var rig = new Entity("Rig");
        var child = new Entity("Second") { new CameraComponent() };

        rig.AddChild(child);
        scene.Entities.Add(new Entity("Main") { new CameraComponent() });
        scene.Entities.Add(rig);

        Assert.Same(child.Get<CameraComponent>(), scene.GetCamera("Second"));
    }

    [Fact]
    public void GetCamera_ByName_IsNullWhenNoCameraHasTheName()
    {
        var scene = new Scene();

        scene.Entities.Add(new Entity("Main") { new CameraComponent() });

        Assert.Null(scene.GetCamera("Missing"));
    }
}