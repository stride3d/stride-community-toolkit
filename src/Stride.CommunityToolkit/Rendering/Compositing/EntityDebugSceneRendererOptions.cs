namespace Stride.CommunityToolkit.Rendering.Compositing;

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
    /// Gets or sets a value indicating whether to show the entity's name. Default is true.
    /// </summary>
    public bool ShowEntityName { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to show the entity's position.
    /// </summary>
    public bool ShowEntityPosition { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to display a background behind the text.
    /// </summary>
    public bool EnableBackground { get; set; }

    /// <summary>
    /// Gets or sets the offset for positioning the debug text relative to the entity.
    /// Default offset is (0, -25).
    /// </summary>
    public Vector2 Offset { get; set; } = new Vector2(0, -25);

    /// <summary>
    /// Gets or sets the color of the background behind the text.
    /// </summary>
    public Color4? BackgroundColor { get; set; }

    /// <summary>
    /// Gets or sets the padding around the text.
    /// </summary>
    public float Padding { get; set; } = 2;

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
    public static EntityDebugSceneRendererOptions CreateDefault() => new EntityDebugSceneRendererOptions
    {
        FontSize = 12,
        FontColor = Color.Black,
        ShowEntityName = true,
        ShowEntityPosition = false,
        EnableBackground = false,
        Offset = new Vector2(0, -25)
    };
}