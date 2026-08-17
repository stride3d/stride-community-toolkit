namespace Stride.CommunityToolkit.Scripts.Utilities;

/// <summary>
/// One contributor's block of lines within a <see cref="DebugOverlay"/>.
/// </summary>
/// <remarks>
/// Sections are what let the camera controller, a game's own instructions and any number of dropdowns
/// share a single overlay with one position and one toggle key, instead of each drawing its own.
/// </remarks>
public sealed class DebugOverlaySection
{
    /// <summary>
    /// Gets the name of the section. Used to find it again, and not displayed.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the callback producing this section's lines.
    /// </summary>
    /// <remarks>
    /// Called every frame the overlay is drawn, so it can return content that changes - a body count,
    /// the state of a dropdown - without anyone having to push updates.
    /// </remarks>
    public required Func<IReadOnlyList<TextElement>> Lines { get; init; }

    /// <summary>
    /// Gets or sets the sort order. Lower values are drawn first; ties keep insertion order.
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Gets or sets whether this section is drawn. Defaults to <see langword="true"/>.
    /// </summary>
    public bool Enabled { get; set; } = true;
}
