using Stride.Input;

namespace Stride.CommunityToolkit.Rendering.Text;

/// <summary>
/// One selectable entry in a <see cref="KeyMenu"/>.
/// </summary>
/// <param name="Key">The key that selects this entry while the key menu is open.</param>
/// <param name="Text">The label shown for this entry, and shown next to the title once it is selected.</param>
/// <param name="Action">Invoked when this entry is selected, after the key menu closes if <see cref="KeyMenu.CloseOnSelect"/> is set.</param>
/// <param name="Color">The optional colour for this entry. If <see langword="null"/>, the default text colour is used.</param>
/// <remarks>
/// Because the key is yours to choose, a key menu is not limited to ten entries - use letters once
/// the digits run out.
/// </remarks>
public record KeyMenuItem(Keys Key, string Text, Action? Action = null, Color? Color = null);