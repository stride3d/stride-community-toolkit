using Stride.BepuPhysics;
using Stride.BepuPhysics.Systems;
using Stride.CommunityToolkit.Engine;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;
using Stride.Rendering.Materials;

namespace Stride.CommunityToolkit.Bepu;

/// <summary>
/// Toggles visualization gizmos for all <see cref="CollidableComponent"/> instances in the current scene
/// when a specified key is pressed.
/// </summary>
public class CollidableGizmoScript : SyncScript
{
    private const string GizmoEntityName = "CollidableGizmo";

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
    /// Gets a value indicating whether gizmos are currently active in the scene.
    /// </summary>
    public bool Visible { get; set; }

    private readonly List<CollidableGizmo> _gizmos = [];

    ///<inheritdoc />
    public override void Start()
    {
        if (Visible)
        {
            CreateGizmos();

            ApplyGizmoEmissiveColor();
        }
    }

    ///<inheritdoc />
    public override void Update()
    {
        if (Input.IsKeyPressed(Key))
        {
            if (!Visible)
            {
                CreateGizmos();
                ApplyGizmoEmissiveColor();
            }
            else
            {
                RemoveGizmos();
            }

            Visible = !Visible;
        }

        if (!Visible) return;

        foreach (var gizmo in _gizmos)
        {
            gizmo.Update();
        }
    }

    private void ApplyGizmoEmissiveColor()
    {
        if (Color is null) return;

        var gizmoEntities = Entity.Scene.Entities.Where(e => e.Name.Contains(GizmoEntityName)).ToList();

        foreach (var gizmoEntity in gizmoEntities)
        {
            if (!gizmoEntity.Name.Contains(GizmoEntityName))
                continue;

            var modelComponent = gizmoEntity.Get<ModelComponent>();

            if (modelComponent is null) continue;

            modelComponent.SetMaterialParameter(MaterialKeys.EmissiveValue, Color.Value);
        }
    }

    private void CreateGizmos()
    {
        _gizmos.Clear();

        var scene = Entity?.Scene ?? SceneSystem?.SceneInstance?.RootScene;
        if (scene is null)
            return;

        foreach (var component in GetAllComponents<CollidableComponent>(scene))
        {
            var gizmo = new CollidableGizmo(component)
            {
                SizeFactor = SizeFactor
            };

            // This needs to be before IsEnabled and IsSelected
            gizmo.Initialize(Game.Services, scene);
            gizmo.IsEnabled = true;
            gizmo.IsSelected = true;

            _gizmos.Add(gizmo);
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

    private static IEnumerable<T> GetAllComponents<T>(Scene scene)
        where T : EntityComponent
    {
        foreach (var entity in GetAllEntities(scene.Entities))
        {
            foreach (var component in entity.Components)
            {
                if (component is T typed) yield return typed;
            }
        }
    }

    private static IEnumerable<Entity> GetAllEntities(IEnumerable<Entity> entities)
    {
        foreach (var entity in entities)
        {
            yield return entity;

            foreach (var child in GetAllEntities(entity.GetChildren()))
            {
                yield return child;
            }
        }
    }
}