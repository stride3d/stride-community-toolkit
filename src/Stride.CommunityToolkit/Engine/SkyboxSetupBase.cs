using Stride.Engine;

namespace Stride.CommunityToolkit.Engine;

/// <summary>
/// The sky of a <see cref="SceneSetup3D"/>: the background and the image-based light of the scene. The core offers
/// <see cref="GradientSkyboxSetup"/>; the skyboxes package derives its photographed sky and offers it through <c>UseSkybox()</c>.
/// </summary>
public abstract class SkyboxSetupBase
{
    /// <summary>The name of the entity the sky is added as.</summary>
    public string EntityName { get; set; } = "Skybox";

    /// <summary>Creates the sky's entity, a <see cref="BackgroundComponent"/> and a skybox <see cref="LightComponent"/>, and adds it to the root scene.</summary>
    /// <param name="game">The running game; the root scene exists and the graphics device is ready.</param>
    /// <returns>The entity added.</returns>
    public abstract Entity Create(Game game);
}