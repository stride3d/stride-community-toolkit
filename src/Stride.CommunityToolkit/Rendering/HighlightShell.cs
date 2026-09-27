using Stride.Engine;
using Stride.Rendering;

namespace Stride.CommunityToolkit.Rendering;

/// <summary>
/// One glowing shell that moves onto whatever should look highlighted: a copy of the target's model,
/// every material slot filled with a highlight material, drawn a little larger over the original. A
/// selection or hover highlight with no render feature and no post effect, only a material and an
/// entity - the way the engine's TopDownRPG template lights up the loot under the mouse.
/// </summary>
/// <remarks>
/// <para>
/// Call <see cref="Show(ModelComponent)"/> every frame with what is under the mouse, or once on a
/// selection; showing the same target again costs nothing. The shell becomes a child of the target's
/// entity, so it follows the target as it moves. For one instance of an instanced model, which has no
/// entity of its own, <see cref="Show(ModelComponent, Matrix)"/> places the shell at the instance's world
/// matrix instead.
/// </para>
/// <para>
/// The shell casts no shadow and draws in its own render group, <see cref="RenderGroup.Group30"/> by
/// default, so a picker can leave it out: a GPU picker that sees the shell answers "the shell" for every
/// pixel it covers. Exclude the group from the picker's pickable mask.
/// </para>
/// </remarks>
/// <param name="material">The material every slot of the shell gets; compile <see cref="MaterialDescriptors.Highlight"/>.</param>
/// <param name="renderGroup">The render group the shell draws in.</param>
public sealed class HighlightShell(Material material, RenderGroup renderGroup = RenderGroup.Group30)
{
    private readonly ModelComponent _model = new() { IsShadowCaster = false, RenderGroup = renderGroup };

    /// <summary>The shell's entity: detached while hidden, a child of the target or a free entity at an instance while shown.</summary>
    public Entity Entity { get; } = new("Highlight shell");

    /// <summary>What the shell is on, or <see langword="null"/> while hidden.</summary>
    public ModelComponent? Target { get; private set; }

    /// <summary>The material every slot of the shell gets.</summary>
    public Material Material { get; } = material ?? throw new ArgumentNullException(nameof(material));

    /// <summary>The render group the shell draws in; leave it out of a picker's pickable mask.</summary>
    public RenderGroup RenderGroup => _model.RenderGroup;

    /// <summary>Whether the shell is on something.</summary>
    public bool IsShown => Target is not null;

    /// <summary>Puts the shell on a model, as a child of its entity, so it follows the model as it moves.</summary>
    /// <param name="target">The model to highlight.</param>
    public void Show(ModelComponent target)
    {
        ArgumentNullException.ThrowIfNull(target);

        if (Target == target && Entity.Transform.Parent == target.Entity.Transform) return;

        Wear(target);

        Entity.Scene = null;
        Entity.Transform.UseTRS = true;
        Entity.Transform.Position = Vector3.Zero;
        Entity.Transform.Rotation = Quaternion.Identity;
        Entity.Transform.Scale = Vector3.One;
        Entity.Transform.Parent = target.Entity.Transform;
    }

    /// <summary>
    /// Puts the shell on one instance of an instanced model: the model's shape, placed at the instance's
    /// world matrix in the target's scene. Call again when the instance moves.
    /// </summary>
    /// <param name="target">The instanced model's component.</param>
    /// <param name="world">The instance's world matrix.</param>
    public void Show(ModelComponent target, Matrix world)
    {
        ArgumentNullException.ThrowIfNull(target);

        Wear(target);

        Entity.Transform.Parent = null;
        Entity.Transform.UseTRS = false;
        Entity.Transform.LocalMatrix = world;
        Entity.Scene = target.Entity.Scene;
    }

    /// <summary>Takes the shell off whatever it is on.</summary>
    public void Hide()
    {
        Entity.Transform.Parent = null;
        Entity.Scene = null;
        Target = null;
    }

    // The target's model, with the highlight on every slot a mesh can point at
    private void Wear(ModelComponent target)
    {
        if (Entity.Get<ModelComponent>() is null) Entity.Add(_model);

        Target = target;

        if (_model.Model == target.Model) return;

        _model.Model = target.Model;
        _model.Materials.Clear();

        var slots = target.Model is { } model ? SlotCount(model) : 0;

        for (var i = 0; i < slots; i++)
        {
            _model.Materials[i] = Material;
        }
    }

    /// <summary>How many material slots a model's meshes use: at least its material list, and every index a mesh names.</summary>
    private static int SlotCount(Model model)
    {
        var count = Math.Max(1, model.Materials.Count);

        foreach (var mesh in model.Meshes)
        {
            count = Math.Max(count, mesh.MaterialIndex + 1);
        }

        return count;
    }
}