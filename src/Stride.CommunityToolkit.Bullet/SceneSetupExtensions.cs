using Stride.CommunityToolkit.Engine;

namespace Stride.CommunityToolkit.Bullet;

/// <summary>
/// Chooses Bullet as the physics engine of a scene setup.
/// </summary>
public static class SceneSetupExtensions
{
    /// <summary>
    /// Makes Bullet the physics engine of the setup: <c>CreatePrimitive</c> attaches Bullet rigid bodies and the
    /// ground a Bullet static collider.
    /// </summary>
    /// <typeparam name="T">The setup's type, kept so the call chains.</typeparam>
    /// <param name="setup">The setup to change.</param>
    /// <returns>The same setup.</returns>
    /// <example>
    /// <code>
    /// game.Run(new SceneSetup3D().UseBullet(), start: Start);
    /// </code>
    /// </example>
    public static T UseBullet<T>(this T setup) where T : SceneSetup
    {
        ArgumentNullException.ThrowIfNull(setup);

        setup.Physics = new BulletPhysicsSetup();

        return setup;
    }
}