namespace Stride.CommunityToolkit.Rendering.Compositing;

/// <summary>
/// The names of the render stages the toolkit's compositors create and its render features look up, so a package
/// finds a stage by the name the compositor gave it rather than by re-spelling the string.
/// </summary>
/// <remarks>
/// <see cref="Opaque"/> and <see cref="Transparent"/> are the engine's own names, which
/// <see cref="Stride.Rendering.Compositing.GraphicsCompositorHelper.CreateDefault"/> uses; <see cref="Ui"/> is the stage
/// <see cref="GraphicsCompositorExtensions.AddUIStage"/> adds for the UI drawn after the post effects.
/// </remarks>
public static class RenderStageNames
{
    /// <summary>The stage opaque meshes draw into, before the transparent one.</summary>
    public static readonly string Opaque = "Opaque";

    /// <summary>The stage transparent meshes, particles and shapes draw into, sorted back to front.</summary>
    public static readonly string Transparent = "Transparent";

    /// <summary>The stage the UI in <see cref="Stride.Rendering.RenderGroup.Group31"/> draws into, after the post effects.</summary>
    public static readonly string Ui = "UiStage";
}