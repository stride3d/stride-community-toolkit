using Stride.CommunityToolkit.Engine;
using Stride.Engine;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Engine;

/// <summary>
/// Finding a camera by its entity's name, in a scene built without a game.
/// </summary>
public class SceneExtensionsTests
{
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