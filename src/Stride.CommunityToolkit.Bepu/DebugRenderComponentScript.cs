using Stride.BepuPhysics.Debug;
using Stride.Engine;

namespace Stride.CommunityToolkit.Bepu;

/// <summary>
/// Script that ensures a <see cref="DebugRenderComponent"/> is added to the owning <see cref="Entity"/>
/// once the scene is ready (when visibility groups are present).
/// </summary>
/// <remarks>
/// The <see cref="DebugRenderComponent"/> enables Bepu physics debug rendering. The debug component
/// itself exposes a <c>Visible</c> property and can be toggled at runtime using the keyboard key
/// <c>Keys.F11</c> (see <c>DebugRenderComponent</c> in the Bepu integration).
///<br /><br />
/// Important details:<br />
/// - Adding this script/component to a single entity in the scene will activate the debug mesh
///   rendering for all Bepu physics entities in that scene (the debug rendering is global per-scene).<br />
/// - The component in this script is created with <c>Visible = false</c> by default to avoid
///   enabling debug rendering unexpectedly.<br />
/// - When a physics body is not awake, its debug mesh is rendered with a lighter color to indicate
///   the sleeping/non-awake state.
/// </remarks>
public class DebugRenderComponentScript : SyncScript
{
    /// <summary>
    /// Gets or sets a value indicating whether the debug mesh is initially <see langword="true"/> when added.
    /// </summary>
    public bool Visible { get; set; }

    /// <summary>
    /// True when a <see cref="DebugRenderComponent"/> has already been added to the entity.
    /// This prevents adding multiple debug components on subsequent frames.
    /// </summary>
    private bool _debugAdded;

    /// <summary>
    /// Called once per frame by the engine. When the scene contains one or more visibility groups
    /// and the debug component has not yet been added, this method creates and adds a
    /// <see cref="DebugRenderComponent"/> to the <see cref="Entity"/>.
    /// </summary>
    /// <remarks>
    /// This method intentionally performs a cheap count check every frame and only performs the
    /// addition once. After the component is added, the component's visibility can be toggled
    /// at runtime by pressing <c>Keys.F11</c> (handled by <see cref="DebugRenderComponent"/>).
    /// See the class-level remarks for additional behavior: the debug renderer is global to the
    /// scene, the component is added invisible by default here, and sleeping physics bodies use
    /// a lighter mesh color.
    /// </remarks>
    public override void Update()
    {
        if (_debugAdded) return;

        var visibilityGroups = SceneSystem.SceneInstance.VisibilityGroups.Count;

        if (visibilityGroups > 0 && !_debugAdded)
        {
            Entity.Add(new DebugRenderComponent() { Visible = Visible });

            _debugAdded = true;
        }
    }
}