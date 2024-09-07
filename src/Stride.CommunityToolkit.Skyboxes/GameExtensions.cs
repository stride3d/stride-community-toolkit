using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering.Lights;
using Stride.Rendering.Skyboxes;

namespace Stride.CommunityToolkit.Skyboxes;

public static class GameExtensions
{
    private const string SkyboxTexture = "skybox_texture_hdr.dds";

    /// <summary>
    /// Adds a skybox to the specified game scene, providing a background texture to create a more immersive environment.
    /// </summary>
    /// <param name="game">The game instance to which the skybox will be added.</param>
    /// <param name="entityName">The name for the skybox entity. If null, a default name will be used.</param>
    /// <returns>The created skybox entity.</returns>
    /// <remarks>
    /// The skybox texture is loaded from the Resources folder, and is used to generate a skybox using the <see cref="SkyboxGenerator"/>.
    /// A new entity is created with a <see cref="BackgroundComponent"/> and a <see cref="LightComponent"/>, both configured for the skybox, and is added to the game scene.
    /// The default position of the skybox entity is set to (0.0f, 2.0f, -2.0f).
    /// </remarks>
    public static Entity AddSkybox(this Game game, string? entityName = "Skybox")
    {
        using var stream = new FileStream($"{AppContext.BaseDirectory}Resources\\{SkyboxTexture}", FileMode.Open, FileAccess.Read);

        var texture = Texture.Load(game.GraphicsDevice, stream, TextureFlags.ShaderResource, GraphicsResourceUsage.Dynamic);

        var skyboxGeneratorContext = new SkyboxGeneratorContext(game);

        var skybox = new Skybox();

        skybox = SkyboxGenerator.Generate(skybox, skyboxGeneratorContext, texture);

        var entity = new Entity(entityName) {
                new BackgroundComponent { Intensity = 1.0f, Texture = texture },
                new LightComponent {
                    Intensity = 1.0f,
                    Type = new LightSkybox() { Skybox = skybox }
                }
        };

        entity.Transform.Position = new Vector3(0.0f, 2.0f, -2.0f);

        entity.Scene = game.SceneSystem.SceneInstance.RootScene;

        return entity;
    }
}
