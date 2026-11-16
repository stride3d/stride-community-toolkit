namespace Example.Common.Galleries;

/// <summary>
/// What every exhibit tells the gallery about itself, whatever kind of station it stands on: the
/// words on its labels and the index board, and what it needs on the ground.
/// </summary>
/// <param name="Title">A short name, as the index board and the labels print it.</param>
/// <param name="Summary">A few words on what to look at, for the widest label.</param>
/// <param name="Method">The API member the station is mostly made of, via <c>nameof</c>, for the middle label.</param>
/// <param name="Pillars">How many of the standard pillars the station wants, 0 to 2.</param>
/// <param name="Anchor">Where the label's dotted line touches the exhibit, in station coordinates.</param>
public record ExhibitInfo(string Title, string Summary, string Method, int Pillars = 0, Vector3? Anchor = null);

/// <summary>
/// One exhibit: its words, and the two methods that make it - one run once when the ring is
/// built, one run every frame. Both receive the station, so a method draws or places things in
/// station coordinates and never learns where on the ring it stands, which is what lets it be
/// lifted into any game with a station at the origin. The order of a registry is the order of the
/// gallery; inserting one shifts every number after it.
/// </summary>
/// <typeparam name="TStation">The kind of station this gallery hands its exhibits: the base frame, or an example's own with more on it.</typeparam>
/// <param name="Title">A short name, as the index board and the labels print it.</param>
/// <param name="Summary">A few words on what to look at, for the widest label.</param>
/// <param name="Method">The API member the station is mostly made of, via <c>nameof</c>.</param>
/// <param name="Setup">Builds what the station needs once - entities, textures, props - or nothing.</param>
/// <param name="Update">Runs every frame the station is shown, in the station's own coordinates: drawing, animating, or nothing.</param>
/// <param name="Pillars">How many of the standard pillars the station wants, 0 to 2.</param>
/// <param name="Anchor">Where the label's dotted line touches the exhibit, in station coordinates.</param>
public sealed record Exhibit<TStation>(
    string Title,
    string Summary,
    string Method,
    Action<TStation>? Setup = null,
    Action<TStation>? Update = null,
    int Pillars = 0,
    Vector3? Anchor = null) : ExhibitInfo(Title, Summary, Method, Pillars, Anchor)
    where TStation : GalleryStation;