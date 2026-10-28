using Stride.CommunityToolkit.Engine;
using Stride.Engine;
using Stride.Games;

namespace Stride.CommunityToolkit.Skyboxes;

/// <summary>
/// The package's photographed sky as a <see cref="SkyboxSetupBase"/>: the bundled HDR environment map as the background
/// and the image-based light.
/// </summary>
public sealed class CloudySkyboxSetup : SkyboxSetupBase
{
    /// <inheritdoc/>
    public override Entity Create(IGame game) => game.AddSkybox(EntityName);
}