namespace Example.Common;

/// <summary>
/// A named set of colours the examples share, so that a square is the same blue wherever it
/// appears and a 2D example matches its 3D twin.
/// </summary>
/// <remarks>
/// The five accents are chosen to read against both backgrounds the examples start from: the
/// cornflower blue of a 2D scene and the grey ground of a 3D one. Take them by name, or by index
/// with <see cref="Accent"/> when a loop hands them out in turn.
/// </remarks>
public sealed class ColorTheme
{
    private readonly Color[] _accents;

    public ColorTheme(string name, Color blue, Color orange, Color red, Color green, Color purple)
    {
        Name = name;
        Blue = blue;
        Orange = orange;
        Red = red;
        Green = green;
        Purple = purple;

        _accents = [blue, orange, red, green, purple];
    }

    public string Name { get; }

    public Color Blue { get; }

    public Color Orange { get; }

    public Color Red { get; }

    public Color Green { get; }

    public Color Purple { get; }

    /// <summary>The accents in the order a loop hands them out: blue, orange, red, green, purple.</summary>
    public IReadOnlyList<Color> Accents => _accents;

    /// <summary>The accent at <paramref name="index"/>, starting again from the first after the last.</summary>
    public Color Accent(int index) => _accents[index % _accents.Length];
}