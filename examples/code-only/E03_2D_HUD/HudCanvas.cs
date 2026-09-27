using Stride.CommunityToolkit.Mathematics;
using Stride.CommunityToolkit.Rendering.Text;
using Stride.CommunityToolkit.Shapes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Graphics.Font;

namespace E03_2D_HUD;

/// <summary>
/// What a widget draws with: the shape batch, the text labels, the theme, the clock and the pointer.
/// Shapes go through one <see cref="ShapeBatch"/>. Text is a <see cref="WorldTextComponent"/> per
/// label, created the first time a widget draws it and hidden in any frame the widget does not.
/// </summary>
public sealed class HudCanvas
{
    /// <summary>The width of an ordinary line, in pixels.</summary>
    public const float Thin = 1.5f;

    /// <summary>The width of a line that matters, in pixels.</summary>
    public const float Thick = 2.5f;

    // A thin ring or a small dot is hard to hit: this many pixels outside a shape still count
    private const float PickSlack = 3f;

    // After a scheme change the widgets fade in one after another
    private const float BootStagger = 0.04f;
    private const float BootFade = 0.5f;

    private const int FontSize = 48;

    private readonly Scene _scene;
    private readonly Dictionary<HudFont, SpriteFont?> _fonts;
    private readonly Dictionary<HudId, Label> _labels = [];
    private readonly Dictionary<HudId, float> _animated = [];

    private HudId? _hovered;
    private bool _clicked;
    private float _boot;
    private int _widget;
    private int _frame;

    public HudCanvas(Game game, Scene scene, ShapeBatch shapes, HudTheme theme)
    {
        _scene = scene;
        Shapes = shapes;
        Theme = theme;

        _fonts = new()
        {
            [HudFont.Sans] = SystemFonts.LoadFirst(game.Services, SystemFonts.SansSerifCandidates, FontSize),
            [HudFont.Bold] = SystemFonts.LoadFirst(game.Services, SystemFonts.SansSerifCandidates, FontSize, FontStyle.Bold),
            [HudFont.Mono] = SystemFonts.LoadFirst(game.Services, SystemFonts.MonospaceCandidates, FontSize),
        };
    }

    public ShapeBatch Shapes { get; }

    public HudTheme Theme { get; private set; }

    /// <summary>The pattern of the panels' glass.</summary>
    public GlassPattern Glass { get; set; }

    /// <summary>The ship's clock in seconds. It stops when the ship is frozen.</summary>
    public float Time { get; private set; }

    /// <summary>The real time since the last frame, in seconds.</summary>
    public float Elapsed { get; private set; }

    /// <summary>The opacity of the widget being drawn, applied to every shape and label.</summary>
    public float Opacity { get; private set; } = 1f;

    public int LabelCount => _labels.Count;

    /// <summary>Starts a frame: takes the clock and finds what is under the pointer.</summary>
    /// <param name="time">The ship's clock in seconds.</param>
    /// <param name="elapsed">The real time since the last frame, in seconds.</param>
    /// <param name="pointer">The pointer, from (0,0) top left to (1,1) bottom right.</param>
    /// <param name="clicked">Whether the pointer was pressed this frame.</param>
    public void Begin(float time, float elapsed, Vector2 pointer, bool clicked)
    {
        Time = time;
        Elapsed = elapsed;
        Opacity = 1f;

        _frame++;
        _widget = 0;
        _boot += elapsed;
        _clicked = clicked;

        // A pick is answered from the frame last drawn, with the shapes the widgets tagged
        _hovered = Shapes.TryPick(pointer, out var hit, PickSlack) ? hit.Tag as HudId? : null;
    }

    /// <summary>Ends a frame: hides the labels no widget drew.</summary>
    public void End()
    {
        foreach (var label in _labels.Values)
        {
            if (label.Frame != _frame) label.Component.IsVisible = false;
        }

        Shapes.Tag = null;
    }

    /// <summary>Changes the scheme and starts the fade-in again.</summary>
    public void Apply(HudTheme theme)
    {
        Theme = theme;

        _boot = 0f;
    }

    /// <summary>Called before each widget draws: sets its opacity from its place in the fade-in.</summary>
    public void NextWidget()
    {
        Opacity = Easing.CubicEaseOut(MathUtil.Clamp((_boot - _widget * BootStagger) / BootFade, 0f, 1f));

        _widget++;
    }

    /// <summary>Sets the whole of the batch's captured state at once, so no draw inherits a stale value.</summary>
    public void Style(float border, float fillAlpha, Color? fill = null, float glow = 0f, Color? glowColour = null, float dash = 0f, float gap = 0f, float phase = 0f, Color? gradientTo = null, Vector2? gradientAlong = null, float opacity = 1f, bool additive = false, bool textured = false)
    {
        Shapes.BorderWidth = border;
        Shapes.Fill.Set(fill, fillAlpha);
        Shapes.Glow.Set(glow, glowColour);
        Shapes.Glow.Additive = additive;
        Shapes.Dash.Set(dash, gap, phase);
        Shapes.Gradient.Color = gradientTo;
        Shapes.Gradient.Direction = gradientAlong ?? Vector2.UnitY;
        Shapes.Opacity = opacity * Opacity;
        Shapes.Textured = textured;
    }

    /// <summary>Draws a label: moves it, sets its string and its colour.</summary>
    /// <param name="id">The label's name. One name is one text component.</param>
    /// <param name="text">The string to show.</param>
    /// <param name="position">Where its anchor goes.</param>
    /// <param name="style">Font, size, anchor and glow. Read the first time the label is drawn.</param>
    /// <param name="colour">The text colour.</param>
    /// <param name="glow">The glow colour, or <c>null</c> for the theme's.</param>
    public void Text(HudId id, string text, Vector2 position, HudText style, Color colour, Color? glow = null)
    {
        if (!_labels.TryGetValue(id, out var label))
        {
            label = CreateLabel(id, style);

            _labels[id] = label;
        }

        var component = label.Component;

        if (component.Text != text)
        {
            component.Text = text;

            // Height is the whole block, so a second line must not halve the letters
            component.Height = style.Height * (text.AsSpan().Count('\n') + 1);
        }

        component.IsVisible = true;
        component.TextColor = colour;
        component.GlowColor = glow ?? Theme.Glow;
        component.Opacity = Opacity;

        label.Entity.Transform.Position = new Vector3(position, 0f);
        label.Frame = _frame;
    }

    /// <summary>Whether the pointer is over the shape tagged with this id.</summary>
    public bool Hovers(HudId id) => _hovered == id;

    /// <summary>Whether the pointer is over a panel or anything inside it.</summary>
    public bool HoversPanel(string name) => _hovered?.Name == name;

    /// <summary>Whether the shape tagged with this id was clicked this frame.</summary>
    public bool Clicks(HudId id) => _clicked && _hovered == id;

    /// <summary>
    /// A value that follows its target smoothly: call it every frame with where the value should
    /// be, and draw with what it returns. The target can change at any time.
    /// </summary>
    /// <param name="id">The value's name, such as a widget and the part "hover".</param>
    /// <param name="target">Where the value is heading.</param>
    /// <param name="rate">How fast it gets there. At 12 it covers most of the distance in a fifth of a second.</param>
    public float Animate(HudId id, float target, float rate = 12f)
    {
        var value = _animated.TryGetValue(id, out var current) ? current + (target - current) * (1f - MathF.Exp(-rate * Elapsed)) : target;

        _animated[id] = value;

        return value;
    }

    private Label CreateLabel(HudId id, HudText style)
    {
        var component = new WorldTextComponent
        {
            Text = string.Empty,
            Height = style.Height,
            FontSize = FontSize,
            Font = _fonts[style.Font],
            Anchor = style.Anchor,
            Alignment = TextAlignment.Center,
            GlowSize = style.Glow,
            DepthTest = false,
        };

        var entity = new Entity($"{id.Name} {id.Part} {id.Index}") { component };

        entity.Scene = _scene;

        return new Label(entity, component);
    }

    /// <summary>A text component, the entity that positions it and the frame it was last drawn in.</summary>
    private sealed class Label(Entity entity, WorldTextComponent component)
    {
        public Entity Entity { get; } = entity;

        public WorldTextComponent Component { get; } = component;

        public int Frame { get; set; }
    }
}