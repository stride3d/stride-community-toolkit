using Stride.CommunityToolkit.Rendering.Text;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace CubeCollapse.Scripts;

/// <summary>
/// Keeps a screen-positioned text in the middle of the window as it is resized.
/// </summary>
/// <remarks>
/// <see cref="TextPositionMode.Anchored"/> with <see cref="EntityTextComponent.ScreenAnchor"/> set to
/// <c>Anchor.MiddleCenter</c> does the same without a script.
/// </remarks>
public class ScreenCentreTextScript : SyncScript
{
    /// <summary>
    /// Gets or sets the text to keep centred.
    /// </summary>
    public EntityTextComponent? Text { get; set; }

    /// <inheritdoc />
    public override void Update()
    {
        if (Text is null || !Text.IsVisible) return;

        if (GraphicsDevice.Presenter?.BackBuffer is not { } backBuffer) return;

        Text.ScreenPosition = new Vector2(backBuffer.Width * 0.5f, backBuffer.Height * 0.5f);
    }
}