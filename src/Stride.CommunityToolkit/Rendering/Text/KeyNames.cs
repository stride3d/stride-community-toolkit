using Stride.CommunityToolkit.Rendering.Overlay;
using Stride.Input;
using System.Collections.Concurrent;

namespace Stride.CommunityToolkit.Rendering.Text;

/// <summary>
/// Formats <see cref="Keys"/> values for on-screen help text, shared by <see cref="TextOverlay"/> and <see cref="KeyMenu"/>.
/// </summary>
internal static class KeyNames
{
    // Filled on first use: the keys a game names are few and fixed, and the name is asked for every frame.
    // Concurrent, because the cache is process-wide and two games, or two tests, may ask at once
    private static readonly ConcurrentDictionary<Keys, string> Names = [];

    /// <summary>
    /// Produces a readable name for a key, so <see cref="Keys.D2"/> shows as "2" rather than "D2".
    /// </summary>
    internal static string Describe(Keys key)
    {
        if (!Names.TryGetValue(key, out var name))
        {
            Names[key] = name = key switch
            {
                >= Keys.D0 and <= Keys.D9 => ((char)('0' + (key - Keys.D0))).ToString(),
                >= Keys.NumPad0 and <= Keys.NumPad9 => ((char)('0' + (key - Keys.NumPad0))).ToString(),
                _ => key.ToString()
            };
        }

        return name;
    }
}