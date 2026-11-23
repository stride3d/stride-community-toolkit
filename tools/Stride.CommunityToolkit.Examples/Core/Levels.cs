namespace Stride.CommunityToolkit.Examples.Core;

/// <summary>
/// The teaching levels the manifest uses, in presentation order.
/// </summary>
/// <remarks>
/// These mirror <c>MetadataVocabulary.Levels</c> in the metadata generator, which also sorts the
/// manifest by them. A level the launcher does not recognise is displayed as-is, so adding one to the
/// generator does not require a matching change here before it works.
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
}