using System.Collections.Immutable;

namespace E13_SignalR_Shared;

/// <summary>
/// A console colour scheme: one accent, a dark fill, a text colour lighter than the accent and a glow
/// deeper than it. Colours are hex strings because the game wants a Stride <c>Color</c> and the page
/// wants a CSS variable, and a string is the one form both can read.
/// </summary>
public sealed record Scheme(string Name, string Accent, string Fill, string Text, string Glow);

/// <summary>
/// The schemes both consoles offer. Defined once, here, so switching to "Purple" from either side
/// means the same four colours on both.
/// </summary>
public static class Schemes
{
    public static ImmutableArray<Scheme> All { get; } =
    [
        new("Blue", "#5ABEFF", "#081628", "#CDEEFF", "#008CFF"),
        new("Red", "#FF5F5A", "#240A0C", "#FFD2CD", "#FF2828"),
        new("Green", "#50F596", "#061E14", "#C8FFE1", "#00DC78"),
        new("Purple", "#B982FF", "#180C2C", "#E6D2FF", "#963CFF"),
        new("Orange", "#FFAA46", "#281406", "#FFE4BE", "#FF8200"),
    ];

    /// <summary>The scheme both sides start in.</summary>
    public static Scheme Default => All[0];

    /// <summary>
    /// Finds a scheme by name, case-insensitively, or returns <see langword="null"/>. A name arriving
    /// over the wire is data, not a guarantee, so the caller decides what an unknown one means.
    /// </summary>
    public static Scheme? Find(string? name)
        => All.FirstOrDefault(scheme => string.Equals(scheme.Name, name, StringComparison.OrdinalIgnoreCase));
}