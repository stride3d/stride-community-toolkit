using Example_CubicleCalamity.Components;
using Example_CubicleCalamity.Gameplay;
using Example_CubicleCalamity.Setup;
using Example_CubicleCalamity.Shared;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Rendering;

namespace Example_CubicleCalamity.Scripts;

/// <summary>
/// Shows, before any click, what the cube under the mouse would do: a clearable group lights up,
/// a lone cube fades.
/// </summary>
/// <remarks>
/// <para>
/// This is the game's one wordless tutorial. The minimum-group rule is otherwise only discoverable
/// by clicking and being rejected; with the hover, a group brightens to say "this will clear" and a
/// stranded single dims to say "this one is dead", so the player reads the board instead of testing
/// it.
/// </para>
/// <para>
/// The highlight is a material swap, not an outline. The shell-inflation outline technique
/// (Example13_MeshOutline) offsets vertices along their normals, and a flat-shaded cube has split
/// normals at every corner - the shell tears apart at the edges. Swapping to a pre-built
/// brightened or dimmed variant of the cube's own material needs no rendering code at all, and the
/// per-<see cref="ModelComponent"/> material override means no other cube sharing that colour is
/// touched.
/// </para>
/// <para>
/// Everything is re-evaluated every frame from the raycast and the logical grid: restore all, then
/// re-apply to whatever is under the mouse now. That makes the bookkeeping immune to cubes being
/// cleared, collapsing into new columns, the game ending or restarting - there is no cached state
/// to go stale, and the work (one raycast, one flood fill, a few dictionary writes) is trivial next
/// to what a click already does.
/// </para>
/// </remarks>
public class HoverHighlightScript : SyncScript
{
    private readonly List<ModelComponent> _highlighted = [];
    private CameraComponent? _camera;

    /// <summary>
    /// Gets the logical grid the group lookup runs against - the same source of truth the click uses.
    /// </summary>
    public required CubeGrid Grid { get; init; }

    /// <summary>
    /// Gets or sets the cube materials in all three hover states. Settable, because a palette
    /// switch swaps in the new palette's set.
    /// </summary>
    public required CubeMaterialSet Materials { get; set; }

    /// <summary>
    /// Gets the click script, consulted so the hover goes quiet once the game is over.
    /// </summary>
    public required CubeClickScript Click { get; init; }

    /// <inheritdoc />
    public override void Update()
    {
        RestoreAll();

        if (Click.IsGameOver || !Input.HasMouse) return;

        _camera ??= Entity.Scene.GetCamera();

        if (_camera is null || !_camera.RaycastMouse(this, 100, out var hitInfo)) return;

        var cube = hitInfo.Collidable.Entity;

        if (cube.Name != EntityNames.Cube) return;

        var group = MatchFinder.FindGroup(Grid, cube);

        if (MatchFinder.IsClearable(group.Count))
        {
            foreach (var member in group)
            {
                Apply(member, Materials.Brightened);
            }
        }
        else
        {
            Apply(cube, Materials.Dimmed);
        }
    }

    /// <summary>
    /// Dresses one cube in a hover variant of its own colour, remembering it for restoration.
    /// </summary>
    private void Apply(Entity cube, IReadOnlyDictionary<Color, Material> variants)
    {
        var colour = cube.Get<CubeComponent>()?.Color;
        var model = cube.Get<ModelComponent>();

        if (colour is null || model is null || !variants.TryGetValue(colour.Value, out var material)) return;

        // A per-component override: the model underneath still owns its normal material
        model.Materials[0] = material;

        _highlighted.Add(model);
    }

    /// <summary>
    /// Removes every override applied last frame, falling back to each model's own material.
    /// </summary>
    private void RestoreAll()
    {
        foreach (var model in _highlighted)
        {
            model.Materials.Remove(0);
        }

        _highlighted.Clear();
    }
}