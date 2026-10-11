using Stride.CommunityToolkit.Engine;

namespace Stride.CommunityToolkit.Skyboxes;

/// <summary>
/// The package's photographed sky as a <see cref="SkyboxSetupBase"/>: the bundled HDR environment map as the background
/// and the image-based light.
/// </summary>
public sealed class CloudySkyboxSetup : SkyboxSetupBase
{
    /// <inheritdoc/>
    public override Stride.Engine.Entity Create(Stride.Engine.Game game) => game.AddSkybox(EntityName);
}