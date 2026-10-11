using Stride.CommunityToolkit.Engine;
using Stride.Engine;

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
    public static GrabberScript AddGrabber(this Game game)
    {
        ArgumentNullException.ThrowIfNull(game);

        var grabber = new GrabberScript();

        game.GetCameraEntity().Add(grabber);

        return grabber;
    }
}