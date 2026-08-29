using Stride.CommunityToolkit.Rendering.Text;
using Stride.Engine;

namespace Stride.CommunityToolkit.Renderers;

/// <summary>
/// Options for rendering debug information for entities in the scene.
/// </summary>
public class EntityDebugSceneRendererOptions
{
    /// <summary>
    /// Gets or sets the font size for the debug text. Default is 12.
    /// </summary>
    public int FontSize { get; set; } = 12;

    /// <summary>
    /// Gets or sets the font color for the debug text. Default is black.
    /// </summary>
    public Color FontColor { get; set; } = Color.Black;

    /// <summary>
    /// Gets or sets a separate colour for the coordinates. Leave <see langword="null"/> to draw them
    /// in <see cref="FontColor"/> on the same line as the name.
    /// </summary>
    /// <remarks>
    /// Setting this moves the coordinates onto their own line beneath the name, because two colours
    /// on one line means measuring and chaining the parts, and the result is harder to read than a
    /// stack. Name and numbers are easier to tell apart when they differ in both colour and position.
    /// </remarks>
    public Color? PositionColor { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to show the entity's name. Default is true.
    /// </summary>
    public bool ShowEntityName { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to show the entity's position.
    /// </summary>
    public bool ShowEntityPosition { get; set; }

    /// <summary>
    /// Gets or sets whether entities nested under others are labelled too. Default is false.
    /// </summary>
    /// <remarks>
    /// Off by default because a scene built from composed entities can hold far more children than
    /// top-level entities, and labelling all of them at once is usually unreadable. Turn it on when
    /// the thing being debugged <em>is</em> the hierarchy.
    /// </remarks>
    public bool IncludeChildEntities { get; set; }

    /// <summary>
    /// Gets or sets an optional test deciding which entities are labelled. Return <see langword="true"/>
    /// to label the entity.
    /// </summary>
    /// <remarks>
    /// The cheapest way to make a busy scene readable - narrow to one name, one component type, or
    /// whatever is actually being investigated, rather than reading every label on screen.
    /// </remarks>
    public Func<Entity, bool>? EntityFilter { get; set; }

    /// <summary>
    /// Gets or sets the distance beyond which labels are not drawn, in world units. Leave
    /// <see langword="null"/> for no limit.
    /// </summary>
    public float? MaxDistance { get; set; }

    /// <summary>
    /// Gets or sets which point of the label sits on the entity's projected position. Default is
    /// <see cref="TextAnchor.TopLeft"/>, which matches how this renderer has always placed text.
    /// </summary>
    /// <remarks>
    /// <see cref="TextAnchor.BottomCenter"/> is usually the one wanted for a label floating over an
    /// object, together with a negative Y <see cref="Offset"/>.
    /// </remarks>
    public TextAnchor Anchor { get; set; } = TextAnchor.TopLeft;

    /// <summary>
    /// Gets or sets a value indicating whether to display a background behind the text.
    /// </summary>
    public bool EnableBackground { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to draw a drop shadow behind the text.
    /// </summary>
    /// <remarks>
    /// An alternative to <see cref="EnableBackground"/> for readability, and cheaper on screen space:
    /// it costs one extra draw of the same string and does not put a panel over the scene.
    /// </remarks>
    public bool EnableShadow { get; set; }

    /// <summary>
    /// Gets or sets the shadow colour. Defaults to half-transparent white, to suit dark text.
    /// </summary>
    public Color ShadowColor { get; set; } = new(255, 255, 255, 128);

    /// <summary>
    /// Gets or sets how far the shadow sits from the text, in pixels.
    /// </summary>
    public Vector2 ShadowOffset { get; set; } = new(1, 1);

    /// <summary>
    /// Gets or sets the offset for positioning the debug text relative to the entity.
    /// Default offset is (0, -25).
    /// </summary>
    public Vector2 Offset { get; set; } = new Vector2(0, -25);

    /// <summary>
    /// Gets or sets the color of the background behind the text.
    /// </summary>
    /// <remarks>
    /// Leave <see langword="null"/> for a light, half-transparent panel that suits the dark default
    /// <see cref="FontColor"/>. A background only makes sense paired with a text colour, so change
    /// both together.
    /// </remarks>
    public Color4? BackgroundColor { get; set; }

    /// <summary>
    /// Gets or sets the padding around the text, in pixels.
    /// </summary>
    public Vector2 Padding { get; set; } = new(2, 2);

    /// <summary>
    /// Initializes a new instance of <see cref="EntityDebugSceneRendererOptions"/> with default settings.
    /// </summary>
    public EntityDebugSceneRendererOptions() { }

    /// <summary>
    /// Initializes a new instance of <see cref="EntityDebugSceneRendererOptions"/> with specified font size and color.
    /// </summary>
    /// <param name="fontSize">The size of the debug font text.</param>
    /// <param name="fontColor">The color of the debug font text.</param>
    public EntityDebugSceneRendererOptions(int fontSize, Color fontColor)
    {
        FontSize = fontSize;
        FontColor = fontColor;
    }

    /// <summary>
    /// Provides default settings for rendering entity debug information.
    /// </summary>
    /// <returns>A new instance of <see cref="EntityDebugSceneRendererOptions"/> with default values.</returns>
    public static EntityDebugSceneRendererOptions CreateDefault() => new();
}