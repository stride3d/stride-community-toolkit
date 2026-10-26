using System.Collections.Immutable;

namespace Stride.CommunityToolkit.Examples.Core;

/// <summary>
/// The teaching levels the manifest uses, in presentation order.
/// </summary>
/// <remarks>
/// These mirror <c>MetadataVocabulary.Levels</c> in the metadata generator. A level the launcher does
/// not recognise is displayed as-is and sorted last, so adding one to the generator does not require a
/// matching change here before it works.
/// </remarks>
public static class Levels
{
    /// <summary>Your first code-only Stride app.</summary>
    public const string GettingStarted = "Getting Started";

    /// <summary>One new concept on top of the base scene.</summary>
    public const string Beginner = "Beginner";

    /// <summary>A Stride subsystem used directly, or several concepts combined.</summary>
    public const string Intermediate = "Intermediate";

    /// <summary>Engine extension points, third-party integration, or multi-project work.</summary>
    public const string Advanced = "Advanced";

    /// <summary>Published but unclassified. Sorts last.</summary>
    public const string Other = "Other";

    /// <summary>
    /// All levels, in the order they should be presented.
    /// </summary>
    /// <remarks>
    /// An <see cref="ImmutableArray{T}"/> rather than a <c>string[]</c>: a public readonly array field
    /// is only readonly in the reference, and any caller could still overwrite a slot and reorder the
    /// menu for everyone.
    /// </remarks>
    public static readonly ImmutableArray<string> All =
        [GettingStarted, Beginner, Intermediate, Advanced, Other];

    /// <summary>
    /// Gets a level's position in <see cref="All"/>, sorting anything unrecognised last.
    /// </summary>
    /// <param name="level">The level name.</param>
    /// <returns>The sort index.</returns>
    public static int IndexOf(string? level)
    {
        var index = level is null ? -1 : All.IndexOf(level);

        return index < 0 ? All.Length : index;
    }
}