using Stride.Input;

namespace Stride.CommunityToolkit.Scripts.Utilities;

/// <summary>
/// A collapsible list of choices drawn with the debug text system, driven entirely by the keyboard.
/// </summary>
/// <remarks>
/// <para>
/// Collapsed, it occupies a single line showing the toggle key, the title and the current selection.
/// Pressing the toggle key expands it into one line per entry; pressing an entry's key selects it,
/// runs its action and collapses the list again. This keeps a screen full of options down to one line
/// until it is needed.
/// </para>
/// <para>
/// Nothing here reads the keyboard on its own - call <see cref="Update"/> once per frame from your
/// update loop, and <see cref="Draw"/> to render it standalone. Better still, hand <see cref="GetLines"/>
/// to a <see cref="DebugOverlay"/> section, so the dropdown shares one position and one toggle key
/// with the camera help and everything else on screen.
/// </para>
/// <para>
/// Keep every title and label to printable ASCII. The debug text renderer replaces anything outside
/// the range 32 to 126 with a space, so accented letters and arrow glyphs vanish silently rather than
/// failing loudly.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var shapes = new DebugTextDropdown
/// {
///     Title = "Shape",
///     ToggleKey = Keys.C,
///     Position = new Int2(10, 200),
///     Items =
///     [
///         new(Keys.D1, "Cube", () => Rebuild(PrimitiveModelType.Cube)),
///         new(Keys.D2, "Sphere", () => Rebuild(PrimitiveModelType.Sphere)),
///     ],
/// };
///
/// // in Update
/// shapes.Update(game.Input);
/// shapes.Draw(game.DebugTextSystem);
/// </code>
/// </example>
public class DebugTextDropdown
{
    private const int LineIncrement = 20;

    /// <summary>
    /// Gets the name shown next to the toggle key, for example <c>Shape</c> in "C - Shape: Sphere".
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Gets the key that expands and collapses the list.
    /// </summary>
    public required Keys ToggleKey { get; init; }

    /// <summary>
    /// Gets the entries to choose from.
    /// </summary>
    public required IReadOnlyList<DebugTextDropdownItem> Items { get; init; }

    /// <summary>
    /// Gets or sets the screen position of the first line, in pixels.
    /// </summary>
    public Int2 Position { get; set; }

    /// <summary>
    /// Gets or sets the colour of the collapsed line and of the title while expanded.
    /// </summary>
    public Color? TitleColor { get; set; }

    /// <summary>
    /// Gets or sets the marker appended to the title while the list is collapsed.
    /// </summary>
    /// <remarks>
    /// Printable ASCII only - see the remarks on <see cref="DebugTextDropdown"/>. Arrow glyphs such as
    /// <c>▼</c> render as blanks.
    /// </remarks>
    public string CollapsedMarker { get; set; } = "[+]";

    /// <summary>
    /// Gets or sets the marker appended to the title while the list is expanded.
    /// </summary>
    /// <inheritdoc cref="CollapsedMarker" path="/remarks"/>
    public string ExpandedMarker { get; set; } = "[-]";

    /// <summary>
    /// Gets or sets the colour used for the currently selected entry while the list is expanded.
    /// Set to <see langword="null"/> to draw it like any other entry.
    /// </summary>
    public Color? SelectedColor { get; set; } = Color.Yellow;

    /// <summary>
    /// Gets or sets whether choosing an entry collapses the list. Defaults to <see langword="true"/>.
    /// </summary>
    /// <remarks>
    /// Set to <see langword="false"/> for a list you expect to use repeatedly - spawning shapes, say,
    /// where reopening it before every press would be tedious. The list then stays up until the
    /// toggle key or <see cref="Keys.Escape"/> closes it, and the highlight follows the last choice.
    /// </remarks>
    public bool CloseOnSelect { get; set; } = true;

    /// <summary>
    /// Gets or sets the index of the selected entry, or -1 when nothing has been selected yet.
    /// </summary>
    /// <remarks>
    /// Assigning this does not run the entry's action - it is for making the display agree with state
    /// you have already set up elsewhere. Out-of-range values read back as -1. The range is checked on
    /// read rather than on write so that an object initializer can set this before <see cref="Items"/>,
    /// which it will whenever the members are written in that order.
    /// </remarks>
    public int SelectedIndex
    {
        get => _selectedIndex >= 0 && _selectedIndex < Items.Count ? _selectedIndex : -1;
        set => _selectedIndex = value;
    }

    private int _selectedIndex = -1;

    /// <summary>
    /// Gets the selected entry, or <see langword="null"/> when nothing has been selected yet.
    /// </summary>
    public DebugTextDropdownItem? Selected => SelectedIndex < 0 ? null : Items[SelectedIndex];

    /// <summary>
    /// Gets or sets a value indicating whether the list is currently expanded.
    /// </summary>
    /// <remarks>
    /// Settable so that several dropdowns can coordinate - close the others when one opens, and their
    /// entry keys are then free to overlap.
    /// </remarks>
    public bool IsOpen { get; set; }

    /// <summary>
    /// Reads the keyboard and updates the dropdown, running the selected entry's action if one is chosen.
    /// </summary>
    /// <param name="input">The input manager to read from.</param>
    /// <returns>
    /// <see langword="true"/> when a key belonging to this dropdown was pressed, so a caller with
    /// several dropdowns can stop looking once one of them has claimed the key.
    /// </returns>
    /// <remarks>
    /// Call once per frame. While expanded, <see cref="Keys.Escape"/> collapses the list without
    /// selecting anything.
    /// </remarks>
    public bool Update(InputManager input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (!input.HasKeyboard) return false;

        if (input.IsKeyPressed(ToggleKey))
        {
            IsOpen = !IsOpen;

            return true;
        }

        if (!IsOpen) return false;

        if (input.IsKeyPressed(Keys.Escape))
        {
            IsOpen = false;

            return true;
        }

        for (var i = 0; i < Items.Count; i++)
        {
            if (!input.IsKeyPressed(Items[i].Key)) continue;

            SelectedIndex = i;

            // Collapsed before the action runs, so an action that throws cannot leave the list stuck
            // open, and one that wants to override the choice can set IsOpen itself
            if (CloseOnSelect) IsOpen = false;

            Items[i].Action?.Invoke();

            return true;
        }

        return false;
    }

    /// <summary>
    /// Draws the dropdown at <see cref="Position"/>.
    /// </summary>
    /// <param name="debugTextSystem">The debug text system to draw with.</param>
    public void Draw(Profiling.DebugTextSystem debugTextSystem)
    {
        ArgumentNullException.ThrowIfNull(debugTextSystem);

        var y = Position.Y;

        foreach (var line in GetLines())
        {
            debugTextSystem.Print(line.Text, new Int2(Position.X, y), line.Color);

            y += LineIncrement;
        }
    }

    /// <summary>
    /// Builds the lines the dropdown currently occupies: one when collapsed, one per entry plus a
    /// title when expanded.
    /// </summary>
    /// <returns>The lines, in display order.</returns>
    /// <remarks>
    /// Use this to hand the dropdown to a <see cref="DebugOverlay"/> section rather than positioning it
    /// yourself.
    /// </remarks>
    public IReadOnlyList<TextElement> GetLines()
    {
        var key = DescribeKey(ToggleKey);

        if (!IsOpen)
        {
            var selection = Selected is null ? string.Empty : $": {Selected.Text}";

            return [new($"{key} - {Title}{selection} {CollapsedMarker}", TitleColor)];
        }

        var lines = new List<TextElement>(Items.Count + 1)
        {
            new($"{key} - {Title} {ExpandedMarker}", TitleColor)
        };

        for (var i = 0; i < Items.Count; i++)
        {
            var item = Items[i];
            var colour = i == SelectedIndex ? SelectedColor ?? item.Color : item.Color;

            lines.Add(new($"  {DescribeKey(item.Key)} - {item.Text}", colour));
        }

        return lines;
    }

    /// <summary>
    /// Produces a readable name for a key, so <see cref="Keys.D2"/> shows as "2" rather than "D2".
    /// </summary>
    private static string DescribeKey(Keys key) => key switch
    {
        >= Keys.D0 and <= Keys.D9 => ((char)('0' + (key - Keys.D0))).ToString(),
        >= Keys.NumPad0 and <= Keys.NumPad9 => ((char)('0' + (key - Keys.NumPad0))).ToString(),
        _ => key.ToString()
    };
}