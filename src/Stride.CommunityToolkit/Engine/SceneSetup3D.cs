using Stride.CommunityToolkit.Rendering.Compositing;
using Stride.Engine;

namespace Stride.CommunityToolkit.Engine;

/// <summary>
/// The setup of a 3D code-only scene: a forward compositor with the toolkit's UI stage, a perspective camera with
/// the camera controller, a directional light, a gradient sky and a ground plane. <see cref="GameExtensions.Run(Game, Action{Scene}?, Action{Scene, Stride.Games.GameTime}?, Stride.Games.GameContext?)"/>
/// with no setup applies this one.
/// </summary>
/// <example>
/// <code>
/// game.Run(new SceneSetup3D { Ground = null, Lighting = LightingSetup.Studio() }.UseBepu(), start: Start);
/// </code>
/// </example>
public sealed class SceneSetup3D : SceneSetup
{
    /// <summary>A fresh setup with every property at its default.</summary>
    public static SceneSetup3D Default => new();

    /// <summary>Creates the setup with its defaults: a perspective camera with the controller, a directional light, a gradient sky and a ground plane.</summary>
    public SceneSetup3D()
    {
        Camera = CameraSetup.Perspective().WithController();
        Ground = GroundSetup.Plane();
    }

    /// <summary>The lights, or <see langword="null"/> for none. Defaults to <see cref="LightingSetup.Directional"/>.</summary>
    public LightingSetup? Lighting { get; set; } = LightingSetup.Directional();

    /// <summary>
    /// The sky, which also lights the scene, or <see langword="null"/> for none. Defaults to the core's gradient sky;
    /// the skyboxes package offers a photographed one through <c>UseSkybox()</c>.
    /// </summary>
    public SkyboxSetupBase? Skybox { get; set; } = new GradientSkyboxSetup();

    internal override void Apply(Game game)
    {
        var compositor = game.AddGraphicsCompositor(ClearColor, Msaa).AddCleanUIStage();

        if (PostEffects is { } configure)
            compositor.ConfigurePostEffects(configure);

        Camera?.Apply(game);
        Lighting?.Apply(game);
        Skybox?.Create(game);
        Ground?.Apply3D(game);

        ApplyCommon(game);
    }
}