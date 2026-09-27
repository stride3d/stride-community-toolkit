using Stride.CommunityToolkit.Rendering.Text;

namespace E03_2D_HUD;

/// <summary>The three fonts the HUD uses.</summary>
public enum HudFont
{
    Sans,
    Bold,
    Mono,
}

/// <summary>
/// How a piece of text is set: font, the height of one line in world units, the anchor and the
/// glow. A label takes its style the first time it is drawn.
/// </summary>
public readonly record struct HudText(HudFont Font, float Height, TextAnchor Anchor = TextAnchor.MiddleCenter, float Glow = 0f)
{
    public static readonly HudText Title = new(HudFont.Bold, 0.27f, TextAnchor.MiddleLeft);

    public static readonly HudText Caption = new(HudFont.Bold, 0.24f);

    public static readonly HudText Body = new(HudFont.Sans, 0.27f, TextAnchor.MiddleLeft);

    public static readonly HudText Small = new(HudFont.Mono, 0.25f);

    public static readonly HudText Value = new(HudFont.Mono, 0.3f);

    /// <summary>A figure that matters: the same as <see cref="Value"/>, with a glow.</summary>
    public static readonly HudText Figure = new(HudFont.Mono, 0.3f, Glow: 3f);

    /// <summary>The same style, anchored at its left edge.</summary>
    public HudText Left => this with { Anchor = TextAnchor.MiddleLeft };

    /// <summary>The same style, anchored at its right edge.</summary>
    public HudText Right => this with { Anchor = TextAnchor.MiddleRight };
}