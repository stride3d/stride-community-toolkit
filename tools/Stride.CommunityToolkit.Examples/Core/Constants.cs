namespace Stride.CommunityToolkit.Examples.Core;

/// <summary>
/// Menu commands that are not examples.
/// </summary>
public static class Constants
{
    /// <summary>
    /// Clears the console, tolerating a redirected output stream.
    /// </summary>
    /// <remarks>
    /// <see cref="Console.Clear"/> throws "The handle is invalid" when stdout is piped to a file or
    /// another process, which crashed the runner on startup before it printed anything at all.
    /// </remarks>
    public static void SafeClear()
    {
        try { Console.Clear(); } catch (IOException) { /* redirected output has nothing to clear */ }
    }

    /// <summary>Redraws the menu.</summary>
    public const string Clear = "Clear";

    /// <summary>Exits the runner.</summary>
    public const string Quit = "Quit";
}

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

    /// <summary>All levels, in the order they should be presented.</summary>
    public static readonly string[] All = [GettingStarted, Beginner, Intermediate, Advanced, Other];

    /// <summary>
    /// Gets a level's position in <see cref="All"/>, sorting anything unrecognised last.
    /// </summary>
    /// <param name="level">The level name.</param>
    /// <returns>The sort index.</returns>
    public static int IndexOf(string? level)
    {
        if (level is null)
        {
            return All.Length;
        }

        var index = Array.IndexOf(All, level);

        return index < 0 ? All.Length : index;
    }
}
