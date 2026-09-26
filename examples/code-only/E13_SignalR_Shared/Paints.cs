using System.Collections.Immutable;

namespace E13_SignalR_Shared;

/// <summary>The paint colours as hex, so the web page can show the same rust the game renders.</summary>
public static class Paints
{
    private static readonly string[] _hex =
    [
        "#B7410E", // Rust
        "#7A2E1F", // Oxide
        "#6B6B2E", // Olive
        "#4A5560", // Slate
        "#C9A227", // Mustard
        "#2E6B6B", // Teal
    ];

    /// <summary>Every paint, in enum order.</summary>
    public static ImmutableArray<ContainerPaint> All { get; } = [.. Enum.GetValues<ContainerPaint>()];

    /// <summary>The hex colour of a paint, <c>#RRGGBB</c>.</summary>
    public static string Hex(ContainerPaint paint) => _hex[(int)paint];
}