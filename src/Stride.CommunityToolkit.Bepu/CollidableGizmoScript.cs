using Stride.BepuPhysics;
using Stride.BepuPhysics.Systems;
using Stride.CommunityToolkit.Engine;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;
using Stride.Rendering.Materials;

namespace Stride.CommunityToolkit.Bepu;

/// <summary>
/// Toggles visualization gizmos for all <see cref="CollidableComponent"/> instances in the current scene
/// when a specified key is pressed.
/// </summary>
[Display("Collidable Gizmo (Bepu)")]
[ComponentCategory("Physics")]
public class CollidableGizmoScript : SyncScript
{
    private const string GizmoEntityName = "CollidableGizmo";

    private readonly List<CollidableGizmo> _gizmos = [];

    /// <summary>
    /// Gets or sets the key used to toggle collidable gizmos on and off. Default: <see cref="Keys.P"/>.
    /// </summary>
    public Keys Key { get; set; } = Keys.P;

    /// <summary>
    /// Gets or sets an optional emissive color applied to created gizmos. When <see langword="null"/>, the default material color is used.
    /// </summary>
    public Color4? Color { get; set; }

    /// <summary>
    /// Gets or sets the size multiplier applied to created gizmos. Defaults to <c>1.0</c>.
    /// </summary>
    public float SizeFactor { get; set; } = 1f;

    /// <summary>
    /// Gets or sets whether the gizmos are shown. Set it before the script starts to show them from the first frame.
    /// </summary>
    public bool Visible { get; set; }

    ///<inheritdoc />
    public override void Start()
    {
        if (Visible) CreateGizmos();
    }

    ///<inheritdoc />
    public override void Update()
    {
        if (Input.IsKeyPressed(Key))
        {
            Visible = !Visible;

            if (Visible)
            {
                CreateGizmos();
            }
            else
            {
                RemoveGizmos();
            }
        }

        if (!Visible) return;

        foreach (var gizmo in _gizmos)
        {
            gizmo.Update();
        }
    }

    private void CreateGizmos()
    {
        _gizmos.Clear();

        var scene = Entity?.Scene ?? SceneSystem?.SceneInstance?.RootScene;

        if (scene is null) return;

        foreach (var component in scene.GetComponentsInScene<CollidableComponent>(includeChildScenes: false))
        {
            var gizmo = new CollidableGizmo(component) { SizeFactor = SizeFactor };

            // This needs to be before IsEnabled and IsSelected
            gizmo.Initialize(Game.Services, scene);
            gizmo.IsEnabled = true;
            gizmo.IsSelected = true;

            _gizmos.Add(gizmo);
        }

        ApplyGizmoEmissiveColor(scene);
    }

    // Each gizmo puts its own entity, named after it, at the top of the scene; the colour goes on their models
    private void ApplyGizmoEmissiveColor(Scene scene)
    {
        if (Color is not { } color) return;

        foreach (var entity in scene.Entities)
        {
            if (entity.Name.Contains(GizmoEntityName) && entity.Get<ModelComponent>() is { } model)
            {
                model.SetMaterialParameter(MaterialKeys.EmissiveValue, color);
            }
        }
    }

    private void RemoveGizmos()
    {
        foreach (var gizmo in _gizmos)
        {
            gizmo.Dispose();
        }

        _gizmos.Clear();
    }
}