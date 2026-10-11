using Stride.CommunityToolkit.Engine;

namespace Stride.CommunityToolkit.Skyboxes;

/// <summary>
/// Chooses a photographed sky for a scene setup.
/// </summary>
public static class SceneSetupExtensions
{
    /// <summary>
    /// Replaces the setup's gradient sky with a photographed one: <see cref="Skies.Cloudy"/> unless another is given.
    /// </summary>
    /// <param name="setup">The setup to change.</param>
    /// <param name="skybox">The sky, or <see langword="null"/> for <see cref="Skies.Cloudy"/>.</param>
    /// <returns>The same setup.</returns>
    /// <example>
    /// <code>
    /// game.Run(new SceneSetup3D().UseBepu().UseSkybox(), start: Start);
    /// </code>
    /// </example>
    public static SceneSetup3D UseSkybox(this SceneSetup3D setup, SkyboxSetupBase? skybox = null)
    {
        ArgumentNullException.ThrowIfNull(setup);

        setup.Skybox = skybox ?? Skies.Cloudy;

        return setup;
    }
}