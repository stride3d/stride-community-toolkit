using E13_SignalR_Shared;

namespace E13_SignalR.Station;

/// <summary>
/// The web console's commands, applied through the same deck and console calls the keyboard makes,
/// so the two consoles cannot drift apart. Web releases are stamped with the web origin.
/// </summary>
public sealed class StationCommands(Deck deck, StationConsole console) : IStationCommands
{
    public void Release(ReleaseRequest request) => deck.Release(request, CommandOrigin.Web);

    public void ReleaseBatch(int count) => deck.ReleaseBatch(Math.Clamp(count, 1, 50), CommandOrigin.Web);

    public void Clear() => deck.Clear();

    public void Shake() => deck.Shake();

    public void SetScheme(string name) => console.Select(name);

    public void Hail(string text) => console.ShowHail(text);
}