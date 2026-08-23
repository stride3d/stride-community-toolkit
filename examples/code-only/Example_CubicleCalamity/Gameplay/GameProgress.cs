using System.Text.Json;

namespace Example_CubicleCalamity.Gameplay;

/// <summary>
/// What survives between launches: for now, only how far the player has climbed.
/// </summary>
/// <remarks>
/// Deliberately a dumb data bag, so it can be serialized as-is when persistence is wanted. Add to
/// it (best score, chosen palette) rather than storing such things loose.
/// </remarks>
public sealed class GameProgress
{
    /// <summary>Gets or sets the level to play, counting from 1.</summary>
    public int Level { get; set; } = 1;
}

/// <summary>
/// Where <see cref="GameProgress"/> comes from at launch and goes on level change.
/// </summary>
/// <remarks>
/// The game only ever talks to this interface, so switching from fresh-every-launch to a saved file
/// is a one-line change where the store is constructed - see <see cref="JsonProgressStore"/>.
/// </remarks>
public interface IProgressStore
{
    /// <summary>Returns the progress to start from.</summary>
    GameProgress Load();

    /// <summary>Records the given progress.</summary>
    void Save(GameProgress progress);
}

/// <summary>
/// The default store: every launch starts from level one, and nothing is written anywhere.
/// </summary>
public sealed class FreshProgressStore : IProgressStore
{
    /// <inheritdoc />
    public GameProgress Load() => new();

    /// <inheritdoc />
    public void Save(GameProgress progress)
    {
        // Deliberately nothing: this store is the decision not to persist
    }
}

/// <summary>
/// A store that keeps progress in a JSON file, so a new launch resumes at the level the last game
/// over reached. Ready to use, not wired up: swap it for the <see cref="FreshProgressStore"/> where
/// the game constructs its store.
/// </summary>
/// <param name="path">The file to read and write, created (directories included) on first save.</param>
public sealed class JsonProgressStore(string path) : IProgressStore
{
    /// <inheritdoc />
    /// <remarks>
    /// A missing or unreadable file is a fresh start, not an error - progress is a convenience, and
    /// refusing to launch over a corrupt save file would price it wrong.
    /// </remarks>
    public GameProgress Load()
    {
        try
        {
            if (!File.Exists(path)) return new GameProgress();

            return JsonSerializer.Deserialize<GameProgress>(File.ReadAllText(path)) ?? new GameProgress();
        }
        catch (Exception exception) when (exception is IOException or JsonException or UnauthorizedAccessException)
        {
            return new GameProgress();
        }
    }

    /// <inheritdoc />
    public void Save(GameProgress progress)
    {
        ArgumentNullException.ThrowIfNull(progress);

        var directory = Path.GetDirectoryName(path);

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, JsonSerializer.Serialize(progress));
    }
}