namespace E13_SignalR_Shared;

/// <summary>
/// One thing that happened to one container. The same shape reports a release, a landing and a
/// loss; <see cref="Position"/> is set for a landing and a loss, <see cref="AirTime"/> for a landing only.
/// </summary>
public sealed record ContainerEvent(
    int Id,
    ContainerSize Size,
    ContainerPaint Paint,
    CommandOrigin Origin,
    Point3? Position = null,
    float? AirTime = null);