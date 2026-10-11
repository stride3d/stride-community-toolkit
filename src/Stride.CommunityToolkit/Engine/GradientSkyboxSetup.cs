using Stride.CommunityToolkit.Rendering;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering.Lights;
using Stride.Rendering.Skyboxes;

namespace Stride.CommunityToolkit.Engine;

/// <summary>
/// A sky with no texture of its own: a small cubemap built on the CPU from three colours, zenith, horizon and
/// nadir, then lit the way a photographed sky is. The default of <see cref="SceneSetup3D.Skybox"/>.
/// </summary>
public sealed class GradientSkyboxSetup : SkyboxSetupBase
{
    /// <summary>The colour straight up.</summary>
    public Color Zenith { get; set; } = new(92, 140, 210);

    /// <summary>The colour at the horizon, all around.</summary>
    public Color Horizon { get; set; } = new(196, 214, 232);

    /// <summary>The colour straight down, below the ground.</summary>
    public Color Nadir { get; set; } = new(70, 74, 80);

    /// <summary>The intensity of the skybox light, 1 for the colours as they are.</summary>
    public float LightIntensity { get; set; } = 1f;

    /// <inheritdoc/>
    public override Entity Create(Game game)
    {
        ArgumentNullException.ThrowIfNull(game);

        var texture = GradientCubemap.Create(game.GraphicsDevice, Zenith, Horizon, Nadir);

        using var context = new SkyboxGeneratorContext(game);

        var skybox = SkyboxGenerator.Generate(new Skybox(), context, texture);

        var entity = new Entity(EntityName)
        {
            new BackgroundComponent { Texture = texture },
            new LightComponent { Type = new LightSkybox { Skybox = skybox }, Intensity = LightIntensity },
        };

        entity.Scene = game.SceneSystem.SceneInstance.RootScene;

        return entity;
    }
}