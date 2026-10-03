namespace Example.Common;

/// <summary>The colour themes the examples choose from.</summary>
public static class ColorThemes
{
    /// <summary>
    /// The theme of the first examples and the playgrounds. Its blue is deep, so that it stands
    /// out from the cornflower blue background of a 2D scene.
    /// </summary>
    public static ColorTheme Default { get; } = new(
        "Default",
        blue: new Color(30, 70, 170),
        orange: new Color(240, 150, 60),
        red: new Color(235, 100, 80),
        green: new Color(110, 200, 110),
        purple: new Color(190, 130, 230));
}