using Stride.CommunityToolkit.Rendering.Text;
using Stride.Core.Mathematics;

namespace E03_2D_Panels;

/// <summary>A palette: one accent, one dark ground, one text colour and one glow.</summary>
sealed record Theme(string Name, Color Accent, Color Fill, Color Text, Color Glow);

/// <summary>What a panel's fill gradient runs to, if anything.</summary>
enum GradientTarget { None, Text, Transparent }

/// <summary>
/// One station of the gallery: a panel recipe, the caption naming it, and the text drawn on the
/// second copy of the panel with the styling that names itself.
/// </summary>
sealed record PanelStation(
    int Number,
    string Caption,
    string Label,
    Action<WorldTextComponent, Theme> StyleText,
    float BorderWidth = 2f,
    float FillAlpha = 1f,
    float CornerRadius = 0f,
    float GlowWidth = 0f,
    byte GlowAlpha = 255,
    bool DerivedFill = false,
    bool WhiteGlow = false,
    bool Ornaments = false,
    bool Dashed = false,
    GradientTarget GradientTo = GradientTarget.None,
    float Opacity = 1f,
    byte BorderAlpha = 255,
    bool AdditiveGlow = false,
    int Sides = 0,
    bool Route = false,
    bool Overlap = false,
    bool Bracket = false);