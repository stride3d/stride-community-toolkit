namespace Stride.CommunityToolkit.Engine;

/// <summary>The light rigs a <see cref="LightingSetup"/> can build.</summary>
public enum LightingRig
{
    /// <summary>One directional light with shadows.</summary>
    Directional,

    /// <summary>Key, fill and rim lights.</summary>
    Studio,

    /// <summary>A light down each of the six world axes.</summary>
    AllDirections,
}