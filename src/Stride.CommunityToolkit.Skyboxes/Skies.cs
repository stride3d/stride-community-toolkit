using Stride.CommunityToolkit.Engine;

namespace Stride.CommunityToolkit.Skyboxes;

/// <summary>
/// The skies this package ships, as values for <see cref="SceneSetup3D.Skybox"/>.
/// </summary>
public static class Skies
{
    /// <summary>The bundled HDR photograph of a cloudy sky, the sky <see cref="GameExtensions.AddSkybox"/> adds.</summary>
    public static SkyboxSetupBase Cloudy => new CloudySkyboxSetup();
}