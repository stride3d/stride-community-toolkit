using Stride.CommunityToolkit.Rendering.Compositing;
using Stride.Engine;

namespace Stride.CommunityToolkit.Engine;

/// <summary>
/// The setup of a 2D code-only scene: a compositor with the toolkit's UI stage, an orthographic camera looking down Z
/// with the 2D camera controller, and a flat ground box below the origin. There are no lights and no sky; 2D
/// materials are flat.
/// </summary>
/// <example>
/// <code>
/// game.Run(new SceneSetup2D { ClearColor = Color.CornflowerBlue }.UseBepu(), start: Start);
/// </code>
/// </example>
public sealed class SceneSetup2D : SceneSetup
{
    /// <summary>A fresh setup with every property at its default.</summary>
    public static SceneSetup2D Default => new();

    /// <summary>Creates the setup with its defaults: an orthographic camera with the controller and a flat ground.</summary>
    public SceneSetup2D()
    {
        Camera = CameraSetup.Orthographic().WithController();
        Ground = GroundSetup.Plane(new Vector2(GameDefaults.Default2DGroundSize.X, GameDefaults.Default2DGroundSize.Y));
    }

    internal override void Apply(Game game)
    {
        // The 2D compositor has no post effects and no tone map, so flat colours stay as written; asking for
        // post effects installs the clean set first, as the 3D setup has
        var compositor = game.Add2DGraphicsCompositor(ClearColor, Msaa);

        if (PostEffects is { } configure)
            compositor.AddCleanUIStage().ConfigurePostEffects(configure);
        else
            compositor.AddUIStage();

        Camera?.Apply(game);
        Ground?.Apply2D(game);

        ApplyCommon(game);
    }
}