using Stride.Input;

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
    /// <remarks>
    /// Disabling removes the section entirely, breadcrumb and all. To leave a hint on screen that the
    /// content is there, collapse it with <see cref="Collapsed"/> instead.
    /// </remarks>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the heading shown above the section, and shown on its own while collapsed.
    /// </summary>
    /// <remarks>
    /// Required for a collapsible section - a collapsed section with no title would be invisible, and
    /// there would be nothing to tell the reader which key brings it back.
    /// </remarks>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the key that collapses and expands this section. <see langword="null"/>, the
    /// default, means the section is always shown in full.
    /// </summary>
    public Keys? ToggleKey { get; set; }

    /// <summary>
    /// Gets or sets whether the section is currently collapsed to its title line.
    /// </summary>
    /// <remarks>
    /// Set this at registration for content a reader only needs occasionally - a list of camera keys
    /// stops earning its screen space once they are known, but a one-line reminder of how to get it
    /// back still does.
    /// </remarks>
    public bool Collapsed { get; set; }

    /// <summary>
    /// Gets whether this section can be collapsed, which needs both a key and a title.
    /// </summary>
    public bool IsCollapsible => ToggleKey is not null && !string.IsNullOrEmpty(Title);
}
