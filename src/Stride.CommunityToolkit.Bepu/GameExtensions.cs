using Stride.CommunityToolkit.Engine;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;

namespace Stride.CommunityToolkit.Bepu;

/// <summary>
/// Extension methods on <see cref="Game"/> for a scene with Bepu physics. The scene itself is set up through
/// <see cref="SceneSetupExtensions.UseBepu"/> on a <see cref="SceneSetup"/>; the core's <c>CreatePrimitive</c>,
/// <c>Add3DGround</c> and <c>Add2DGround</c> then attach Bepu bodies and colliders.
/// </summary>
public static class GameExtensions
{
    /// <summary>
    /// Puts a <see cref="GrabberScript"/> on the camera entity, so the left mouse button picks up any
    /// dynamic body, carries it on the end of the camera ray and throws it on release.
    /// </summary>
    /// <param name="game">The game whose camera entity gets the grabber.</param>
    /// <returns>The grabber, for tuning its reach, force and keys.</returns>
    /// <remarks>
    /// The script is also a Game Studio component, under Physics, for a project that would rather place
    /// it on the camera entity in the editor.
    /// </remarks>
    public static GrabberScript AddGrabber(this IGame game)
    {
        ArgumentNullException.ThrowIfNull(game);

        var grabber = new GrabberScript();

        game.GetCameraEntity().Add(grabber);

        return grabber;
    }

    /// <summary>
    /// Adds Bepu's two debug views to the root scene on one entity: the collidable gizmos, toggled with P, and the
    /// physics debug meshes, toggled with F11.
    /// </summary>
    /// <param name="game">The game whose root scene gets the entity.</param>
    /// <param name="gizmoColor">An emissive colour for the gizmos, or <see langword="null"/> for the gizmo material's own.</param>
    /// <param name="gizmosVisible">Whether the gizmos are shown from the first frame.</param>
    /// <param name="meshesVisible">Whether the debug meshes are shown from the first frame.</param>
    /// <returns>The entity, named <c>Physics Debug</c>, carrying a <see cref="CollidableGizmoScript"/> and a <see cref="DebugRenderComponentScript"/>.</returns>
    /// <remarks>
    /// Both scripts act on the whole scene: the gizmo script draws every collider it finds, and one
    /// <c>DebugRenderComponent</c> turns the debug meshes on for every Bepu entity. One entity is all a scene
    /// needs; a second copy of either script answers the same key and toggles the first one back.
    /// </remarks>
    public static Entity AddPhysicsDebug(this IGame game, Color4? gizmoColor = null, bool gizmosVisible = false, bool meshesVisible = false)
    {
        ArgumentNullException.ThrowIfNull(game);

        var entity = new Entity("Physics Debug")
        {
            new CollidableGizmoScript { Color = gizmoColor, Visible = gizmosVisible },
            new DebugRenderComponentScript { Visible = meshesVisible },
        };

        entity.Scene = game.GetSceneSystem().SceneInstance.RootScene;

        return entity;
    }
}