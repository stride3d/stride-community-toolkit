using Example17_SignalR_Shared.Core;
using Stride.Core.Mathematics;

namespace Example17_SignalR.Core;

public static class Colours
{
    public static readonly Dictionary<EntityType, Color> ColourTypes = new()
    {
        { EntityType.Success, new Color(25, 135, 84)},
        { EntityType.Danger, new Color(220, 53, 69) },
        { EntityType.Warning, new Color(255, 193, 7) },
        { EntityType.Primary, new Color(13, 110, 253) },
        { EntityType.Info, new Color(13, 202, 240) },
        { EntityType.Secondary, new Color(108, 117, 125) },
        { EntityType.Destroyer, new Color(0, 0, 0) }
    };
}