using E13_SignalR_Shared;

namespace E13_SignalR.Station;

/// <summary>
/// What the web console can make the game do, applied through the same deck and console calls the
/// keyboard makes, so the two consoles cannot drift apart. <see cref="Net.StationLink"/> queues these as
/// they arrive on SignalR's threads and replays them on the game thread, the only thread that may touch
/// the deck. Web releases are stamped with the web origin.
/// </summary>
public sealed class StationCommands(Deck deck, StationConsole console)
{
    public void Release(ReleaseRequest request) => deck.Release(request, CommandOrigin.Web);

    public void ReleaseBatch(int count) => deck.ReleaseBatch(Math.Clamp(count, 1, 50), CommandOrigin.Web);

    public void Clear() => deck.Clear();

    public void Shake() => deck.Shake();

    public void SetScheme(string name) => console.Select(name);

    public void Hail(string text) => console.ShowHail(text);
}