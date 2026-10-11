using Stride.Engine;

namespace Stride.CommunityToolkit.Engine;

/// <summary>
/// The lights of a <see cref="SceneSetup3D"/>: one directional light, the three-light studio rig, or a light down
/// each world axis. Built through <see cref="Directional"/>, <see cref="Studio"/> or <see cref="AllDirections"/>.
/// </summary>
/// <param name="Rig">Which lights.</param>
/// <param name="Intensity">The intensity of the main light; the studio rig scales its fill and rim from it.</param>
/// <param name="Shadows">Whether the main light casts shadows.</param>
/// <param name="YawDegrees">The studio rig's turn about Y.</param>
public sealed record LightingSetup(LightingRig Rig, float Intensity, bool Shadows = true, float YawDegrees = 45f)
{
    /// <summary>One directional light from above and behind the camera, what <see cref="GameExtensions.AddDirectionalLight"/> adds.</summary>
    public static LightingSetup Directional(float intensity = 20f, bool shadows = true) => new(LightingRig.Directional, intensity, shadows);

    /// <summary>The key, fill and rim lights of <see cref="GameExtensions.AddStudioLighting"/>.</summary>
    public static LightingSetup Studio(float intensity = 15f, float yawDegrees = 45f, bool shadows = true) => new(LightingRig.Studio, intensity, shadows, yawDegrees);

    /// <summary>Six lights, one down each world axis, of <see cref="GameExtensions.AddAllDirectionLighting"/>; no shadows.</summary>
    public static LightingSetup AllDirections(float intensity = 5f) => new(LightingRig.AllDirections, intensity, Shadows: false);

    internal void Apply(Game game)
    {
        switch (Rig)
        {
            case LightingRig.Directional:
                game.AddDirectionalLight(enableShadows: Shadows, intensity: Intensity);
                break;
            case LightingRig.Studio:
                game.AddStudioLighting(Intensity, YawDegrees, Shadows);
                break;
            case LightingRig.AllDirections:
                game.AddAllDirectionLighting(Intensity, showLightGizmo: false);
                break;
        }
    }
}