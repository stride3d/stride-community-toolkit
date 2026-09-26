using Stride.Core.Mathematics;
using System.Globalization;

namespace Stride.CommunityToolkit.Box2D;

/// <summary>
/// Reads the straight-line commands of an SVG path into a list of points, so a level outline drawn
/// in Inkscape can become a chain shape. Ported from the Box2D.NET samples' <c>SvgParser</c>
/// (MIT, (c) 2025 Erin Catto, (c) 2025 Choi Ikpil).
/// </summary>
/// <remarks>
/// <para>
/// Only <c>M</c>, <c>L</c>, <c>H</c>, <c>V</c> and their relative forms <c>m</c>, <c>l</c>,
/// <c>h</c>, <c>v</c> are read, with the SVG rule that coordinates after a move-to continue as
/// line-tos; <c>z</c> or <c>Z</c> ends the path. Curves are refused with a
/// <see cref="FormatException"/>: flatten them in the editor first.
/// </para>
/// <para>
/// SVG's y axis points down, so every point's y is negated after the offset is added, which puts
/// the drawing the right way up in a world where y points up.
/// </para>
/// </remarks>
public static class SvgPath2D
{
    /// <summary>
    /// Parses <paramref name="path"/> into points: <c>scale * (point + offset)</c> with y flipped.
    /// </summary>
    /// <param name="path">The <c>d</c> attribute of an SVG path made of straight lines.</param>
    /// <param name="offset">Added to every point in SVG units before scaling, to move the origin.</param>
    /// <param name="scale">Multiplies every point after the offset.</param>
    /// <param name="reverse">Reverses the point order, which flips the side a chain collides on.</param>
    /// <returns>The points in path order, or reversed when asked.</returns>
    /// <exception cref="FormatException">The path uses a command other than the straight-line ones, or a number does not parse.</exception>
    public static Vector2[] Parse(string path, Vector2 offset = default, float scale = 1f, bool reverse = false)
    {
        ArgumentNullException.ThrowIfNull(path);

        var points = new List<Vector2>();
        var current = Vector2.Zero;
        var command = '\0';
        var index = 0;

        while (SkipSeparators(path, ref index))
        {
            var c = path[index];

            // A letter is the next command; z closes the path and ends the read
            if (char.IsLetter(c))
            {
                if (c is 'z' or 'Z') break;

                command = ReadCommand(c);
                index++;
                continue;
            }

            if (command == '\0')
                throw new FormatException("An SVG path must start with a command.");

            // Anything else is a coordinate for the current command. Further pairs after a move-to
            // are line-tos, in the same absolute or relative form.
            current = Apply(command, current, path, ref index);
            command = command switch { 'M' => 'L', 'm' => 'l', _ => command };

            points.Add(new Vector2(scale * (current.X + offset.X), -scale * (current.Y + offset.Y)));
        }

        if (reverse) points.Reverse();

        return [.. points];
    }

    // The straight-line commands; anything else - curves, arcs - is refused rather than skipped.
    private static char ReadCommand(char c)
        => c is 'M' or 'L' or 'H' or 'V' or 'm' or 'l' or 'h' or 'v'
            ? c
            : throw new FormatException($"SVG path command '{c}' is not supported: only the straight-line commands M, L, H, V and their relative forms are read.");

    // One command's coordinates applied to the current point: an absolute command replaces the
    // coordinate it names, a relative one adds to it.
    private static Vector2 Apply(char command, Vector2 current, string path, ref int index) => command switch
    {
        'M' or 'L' => new Vector2(ReadNumber(path, ref index), ReadNumber(path, ref index)),
        'm' or 'l' => current + new Vector2(ReadNumber(path, ref index), ReadNumber(path, ref index)),
        'H' => current with { X = ReadNumber(path, ref index) },
        'h' => current with { X = current.X + ReadNumber(path, ref index) },
        'V' => current with { Y = ReadNumber(path, ref index) },
        'v' => current with { Y = current.Y + ReadNumber(path, ref index) },
        _ => throw new ArgumentOutOfRangeException(nameof(command), command, "Not a straight-line SVG command."),
    };

    // One number: an optional sign, digits with a decimal point, an optional exponent.
    private static float ReadNumber(string path, ref int index)
    {
        SkipSeparators(path, ref index);

        var start = index;

        ScanNumber(path, ref index);

        var token = path.AsSpan(start, index - start);

        if (token.IsEmpty || !float.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            var found = token.IsEmpty && start < path.Length ? path[start].ToString() : token.ToString();

            throw new FormatException($"Expected a number at position {start} of the SVG path, found '{found}'.");
        }

        return value;
    }

    // Advances over the characters of one number. A sign that follows a digit starts the next
    // number, as SVG allows "10-5"; a sign after an exponent marker belongs to the exponent.
    private static void ScanNumber(string path, ref int index)
    {
        SkipSign(path, ref index);

        while (index < path.Length)
        {
            var c = path[index];

            if (char.IsDigit(c) || c == '.')
            {
                index++;
            }
            else if (c is 'e' or 'E')
            {
                index++;
                SkipSign(path, ref index);
            }
            else
            {
                break;
            }
        }
    }

    private static void SkipSign(string path, ref int index)
    {
        if (index < path.Length && path[index] is '-' or '+') index++;
    }

    // Advances past whitespace and commas; false once the path is used up.
    private static bool SkipSeparators(string path, ref int index)
    {
        while (index < path.Length && (path[index] == ',' || char.IsWhiteSpace(path[index]))) index++;

        return index < path.Length;
    }
}