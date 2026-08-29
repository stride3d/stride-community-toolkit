using Stride.CommunityToolkit.Rendering.Text;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace Example_CubicleCalamity.Scripts;

/// <summary>
/// Keeps a screen-positioned text in the middle of the window as it is resized.
/// </summary>
/// <remarks>
/// <see cref="TextPositionMode.Anchored"/> snaps to the four corners, and
/// <see cref="TextPositionMode.Screen"/> takes a fixed pixel position, so neither of them centres
/// anything on its own. Recomputing the position each frame covers the gap in four lines. It is worth
/// knowing this is the workaround rather than the intended route - a centre option belongs in the
/// component, and is noted as such in the toolkit's architecture notes.
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

        var backBuffer = GraphicsDevice.Presenter?.BackBuffer;

        if (backBuffer is null) return;

        Text.ScreenPosition = new Vector2(backBuffer.Width * 0.5f, backBuffer.Height * 0.5f);
    }
}