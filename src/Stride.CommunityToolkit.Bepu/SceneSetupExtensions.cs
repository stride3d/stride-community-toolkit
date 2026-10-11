using Stride.CommunityToolkit.Engine;

namespace Stride.CommunityToolkit.Bepu;

/// <summary>
/// Chooses Bepu as the physics engine of a scene setup.
/// </summary>
public static class SceneSetupExtensions
{
    /// <summary>
    /// Makes Bepu the physics engine of the setup: <c>CreatePrimitive</c> attaches Bepu bodies and the ground a
    /// Bepu static collider.
    /// </summary>
    /// <typeparam name="T">The setup's type, kept so the call chains.</typeparam>
    /// <param name="setup">The setup to change.</param>
    /// <returns>The same setup.</returns>
    /// <example>
    /// <code>
    /// game.Run(new SceneSetup3D().UseBepu(), start: Start);
    /// </code>
    /// </example>
    public static T UseBepu<T>(this T setup) where T : SceneSetup
    {
        ArgumentNullException.ThrowIfNull(setup);

        setup.Physics = new BepuPhysicsSetup();

        return setup;
    }
}