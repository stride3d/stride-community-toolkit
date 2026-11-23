using Stride.BepuPhysics;
using Stride.BepuPhysics.Debug;
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
    /// Gets one of the game's Bepu simulations: the first, which is the one every entity is in unless the
    /// game configured more.
    /// </summary>
    /// <param name="game">The game the simulation belongs to.</param>
    /// <param name="index">Which simulation, in the order of <see cref="BepuConfiguration.BepuSimulations"/>.</param>
    /// <returns>The simulation, for its gravity, its time scale, its queries and its step settings.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The game has no simulation at <paramref name="index"/>.</exception>
    public static BepuSimulation GetSimulation(this IGame game, int index = 0)
    {
        ArgumentNullException.ThrowIfNull(game);

        var simulations = game.Services.GetOrCreate<BepuConfiguration>().BepuSimulations;

        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, simulations.Count);

        return simulations[index];
    }

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
    /// physics debug meshes, toggled with F11, with the solver's contact points and normals when asked.
    /// </summary>
    /// <param name="game">The game whose root scene gets the entity.</param>
    /// <param name="gizmoColor">An emissive colour for the gizmos, or <see langword="null"/> for the gizmo material's own.</param>
    /// <param name="gizmosVisible">Whether the gizmos are shown from the first frame.</param>
    /// <param name="meshesVisible">Whether the debug meshes are shown from the first frame.</param>
    /// <param name="contactsVisible">Whether the debug meshes include the solver's contact points and normals (Stride 4.4.1).</param>
    /// <returns>The entity, named <c>Physics Debug</c>, carrying a <see cref="CollidableGizmoScript"/> and the engine's <see cref="DebugRenderComponent"/>.</returns>
    /// <remarks>
    /// Both act on the whole scene: the gizmo script draws every collider it finds, and one
    /// <see cref="DebugRenderComponent"/> turns the debug meshes on for every Bepu entity. One entity is all a scene
    /// needs; a second copy of either answers the same key and toggles the first one back. The engine's processor
    /// holds the component back until the scene's visibility groups exist, so it can be added before the first frame.
    /// </remarks>
    public static Entity AddPhysicsDebug(this IGame game, Color4? gizmoColor = null, bool gizmosVisible = false, bool meshesVisible = false, bool contactsVisible = false)
    {
        ArgumentNullException.ThrowIfNull(game);

        var entity = new Entity("Physics Debug")
        {
            new CollidableGizmoScript { Color = gizmoColor ?? default, Visible = gizmosVisible },
            new DebugRenderComponent { Visible = meshesVisible, ShowContactPoints = contactsVisible, ShowContactNormals = contactsVisible },
        };

        entity.Scene = game.GetSceneSystem().SceneInstance.RootScene;

        return entity;
    }
}