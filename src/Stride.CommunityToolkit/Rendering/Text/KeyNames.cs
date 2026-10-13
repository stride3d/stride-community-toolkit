using Stride.Input;

namespace Stride.CommunityToolkit.Rendering.Text;

/// <summary>
/// Formats <see cref="Keys"/> values for on-screen help text, shared by <see cref="DebugOverlay"/> and <see cref="DebugTextDropdown"/>.
/// </summary>
internal static class KeyNames
{
    /// <summary>
    /// Produces a readable name for a key, so <see cref="Keys.D2"/> shows as "2" rather than "D2".
    /// </summary>
    internal static string Describe(Keys key) => key switch
    {
        >= Keys.D0 and <= Keys.D9 => ((char)('0' + (key - Keys.D0))).ToString(),
        >= Keys.NumPad0 and <= Keys.NumPad9 => ((char)('0' + (key - Keys.NumPad0))).ToString(),
        _ => key.ToString()
    };
}