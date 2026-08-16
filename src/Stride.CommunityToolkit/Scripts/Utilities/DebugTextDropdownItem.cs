using Stride.Input;

namespace Stride.CommunityToolkit.Scripts.Utilities;

/// <summary>
/// One selectable entry in a <see cref="DebugTextDropdown"/>.
/// </summary>
/// <param name="Key">The key that selects this entry while the dropdown is open.</param>
/// <param name="Text">The label shown for this entry, and shown next to the title once it is selected.</param>
/// <param name="Action">Invoked when this entry is selected. The dropdown closes first, so the action is free to reopen it.</param>
/// <param name="Color">The optional colour for this entry. If <see langword="null"/>, the default text colour is used.</param>
/// <remarks>
/// Because the key is yours to choose, a dropdown is not limited to ten entries - use letters once
/// the digits run out.
/// </remarks>
public record DebugTextDropdownItem(Keys Key, string Text, Action? Action = null, Color? Color = null);
