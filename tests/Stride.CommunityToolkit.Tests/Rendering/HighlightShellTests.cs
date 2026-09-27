using Stride.CommunityToolkit.Rendering;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Rendering;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Rendering;

/// <summary>
/// The shell's bookkeeping, with no device: which model it wears, how many slots it fills, where its
/// entity hangs, and that it stays out of the shadows and in its own render group.
/// </summary>
public class HighlightShellTests
{
    private static readonly Material Glow = new();

    private static ModelComponent Target(string name, int materials, params int[] meshSlots)
    {
        var model = new Model();

        for (var i = 0; i < materials; i++) model.Materials.Add(new MaterialInstance());

        foreach (var slot in meshSlots) model.Meshes.Add(new Mesh { MaterialIndex = slot });

        var component = new ModelComponent(model);

        _ = new Entity(name) { component };

        return component;
    }

    [Fact]
    public void ShowingMakesTheShellAChildWearingTheTargetsModel()
    {
        var shell = new HighlightShell(Glow);
        var target = Target("crate", 1, 0);

        shell.Show(target);

        Assert.Same(target, shell.Target);
        Assert.Same(target.Entity.Transform, shell.Entity.Transform.Parent);

        var model = shell.Entity.Get<ModelComponent>()!;

        Assert.Same(target.Model, model.Model);
        Assert.Same(Glow, model.Materials[0]);
    }

    [Fact]
    public void EverySlotAMeshNamesGetsTheHighlight()
    {
        var shell = new HighlightShell(Glow);

        // One material in the list, but a mesh pointing at slot 2: three slots to fill
        shell.Show(Target("statue", 1, 0, 2));

        var materials = shell.Entity.Get<ModelComponent>()!.Materials;

        Assert.Equal(3, materials.Count);
        Assert.All(materials, pair => Assert.Same(Glow, pair.Value));
    }

    [Fact]
    public void TheShellCastsNoShadowAndDrawsInItsOwnGroup()
    {
        var shell = new HighlightShell(Glow);

        shell.Show(Target("crate", 1, 0));

        var model = shell.Entity.Get<ModelComponent>()!;

        Assert.False(model.IsShadowCaster);
        Assert.Equal(RenderGroup.Group30, model.RenderGroup);
        Assert.Equal(RenderGroup.Group30, shell.RenderGroup);
    }

    [Fact]
    public void AnInstanceGetsAFreeShellAtItsMatrix()
    {
        var shell = new HighlightShell(Glow);
        var target = Target("crates", 1, 0);
        var world = Matrix.Translation(3f, 0f, -2f);

        shell.Show(target, world);

        Assert.Null(shell.Entity.Transform.Parent);
        Assert.False(shell.Entity.Transform.UseTRS);
        Assert.Equal(world, shell.Entity.Transform.LocalMatrix);
    }

    [Fact]
    public void MovingOnSwapsTheModelAndHidingDetaches()
    {
        var shell = new HighlightShell(Glow);
        var first = Target("first", 1, 0);
        var second = Target("second", 2, 0, 1);

        shell.Show(first);
        shell.Show(second);

        Assert.Same(second.Model, shell.Entity.Get<ModelComponent>()!.Model);
        Assert.Same(second.Entity.Transform, shell.Entity.Transform.Parent);
        Assert.True(shell.Entity.Transform.UseTRS);

        shell.Hide();

        Assert.False(shell.IsShown);
        Assert.Null(shell.Entity.Transform.Parent);
        Assert.DoesNotContain(shell.Entity.Transform, second.Entity.Transform.Children);
    }
}