using System.Text.Json;

namespace CubeCollapse.Gameplay;

/// <summary>
/// A store that keeps progress in a JSON file, so a new launch resumes at the last level reached.
/// Ready to use, not wired up: pass one where the game is constructed instead of the default
/// <see cref="FreshProgressStore"/>.
/// </summary>
/// <param name="path">The file to read and write, created (directories included) on first save.</param>
public sealed class JsonProgressStore(string path) : IProgressStore
{
    /// <inheritdoc />
    /// <remarks>
    /// A missing or unreadable file is a fresh start, not an error: progress is a convenience.
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