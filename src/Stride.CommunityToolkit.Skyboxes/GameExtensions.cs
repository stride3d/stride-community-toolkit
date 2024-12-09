using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering.Lights;
using Stride.Rendering.Skyboxes;

namespace Stride.CommunityToolkit.Skyboxes;

/// <summary>
/// Provides extension methods for the <see cref="Game"/> class to enhance functionality such as adding skyboxes and other game elements.
/// </summary>
/// <remarks>
/// These methods allow for easy integration of common game elements into a Stride game project, reducing the need for manual setup.
/// </remarks>
public static class GameExtensions
{
    private const string SkyboxTexture = "skybox_texture_hdr.dds";

    /// <summary>
    /// Adds a skybox to the specified game scene, providing a background texture to create a more immersive environment.
    /// </summary>
    /// <param name="game">The <see cref="Game"/> instance to which the skybox will be added.</param>
    /// <param name="entityName">The optional name for the skybox entity. If null, a default name ("Skybox") will be used.</param>
    /// <returns>The created <see cref="Entity"/> representing the skybox.</returns>
    /// <remarks>
    /// The skybox texture is loaded from the Resources folder and is used to generate a skybox using the <see cref="SkyboxGenerator"/>.
    /// The skybox entity is created with both a <see cref="BackgroundComponent"/> and a <see cref="LightComponent"/>, configured for the skybox.
    /// The entity is added to the root scene of the game and placed at the default position (0.0f, 2.0f, -2.0f).
    /// </remarks>
    /// <example>
    /// This example demonstrates how to add a skybox to a game:
    /// <code>
    /// game.AddSkybox();
    /// </code>
    /// </example>
    public static Entity AddSkybox(this Game game, string? entityName = "Skybox")
    {
        using var stream = new FileStream(Path.Combine(AppContext.BaseDirectory, "Resources", SkyboxTexture), FileMode.Open, FileAccess.Read);

        var texture = Texture.Load(game.GraphicsDevice, stream, TextureFlags.ShaderResource, GraphicsResourceUsage.Dynamic);

        using var context = new SkyboxGeneratorContext(game);

        var skybox = new Skybox();

        skybox = SkyboxGenerator.Generate(skybox, context, texture);

        var entity = new Entity(entityName) {
                new BackgroundComponent { Texture = texture },
                new LightComponent {
                    Type = new LightSkybox() { Skybox = skybox }
                }
        };

        entity.Transform.Position = new Vector3(0.0f, 2.0f, -2.0f);

        entity.Scene = game.SceneSystem.SceneInstance.RootScene;

        return entity;
    }
}