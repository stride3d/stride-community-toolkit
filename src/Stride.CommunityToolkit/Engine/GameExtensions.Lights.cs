using Stride.Engine;
using Stride.Games;
using Stride.Rendering.Colors;
using Stride.Rendering.Lights;

namespace Stride.CommunityToolkit.Engine;

/// <summary>
/// The lighting helpers: a single directional light, six lights along the world axes, and the three-point studio rig.
/// </summary>
public static partial class GameExtensions
{
    /// <summary>
    /// Adds a directional light entity to the game's root scene with optional customization.
    /// </summary>
    /// <param name="game">The Game instance to which the directional light will be added.</param>
    /// <param name="entityName">Optional name for the new directional light entity. If null, the entity will not be named.</param>
    /// <param name="enableShadows">Whether the light casts shadows. Defaults to <see langword="true"/>.</param>
    /// <param name="intensity">Brightness of the light. Defaults to 20.</param>
    /// <returns>The created Entity object representing the directional light.</returns>
    /// <remarks>
    /// <para>
    /// This method creates a directional light with the following default settings:
    /// - Intensity: 20.0f
    /// - Position: (0, 2.0f, 0)
    /// - Rotation: X-axis rotated by -30 degrees and Y-axis rotated by -180 degrees.
    /// - Shadow Enabled: True
    /// - Shadow Size: Large
    /// - Shadow Filter: PCF (Percentage Closer Filtering) with a filter size of 5x5
    /// </para>
    /// <para>The entity will be added to the game's root scene. You can customize the light properties by accessing the returned Entity object.</para>
    /// <para>
    /// Note that <paramref name="enableShadows"/> only controls shadows <em>cast</em> by geometry onto
    /// other geometry. It does not flatten shading: a surface facing away from the light is still
    /// darkened by the diffuse N·L term, whatever this is set to. For a surface that reads as the same
    /// colour from every angle, use an emissive material such as <see cref="CreateFlatMaterial"/>.
    /// </para>
    /// </remarks>
    public static Entity AddDirectionalLight(this IGame game, string? entityName = "Directional Light", bool enableShadows = true, float intensity = 20.0f)
    {
        var entity = CreateDirectionalLight(entityName, intensity, enableShadows);

        entity.Transform.Position = new Vector3(0, 2.0f, 0);
        entity.Transform.Rotation = Quaternion.RotationX(MathUtil.DegreesToRadians(-30.0f)) *
                                    Quaternion.RotationY(MathUtil.DegreesToRadians(-180.0f));

        entity.Scene = game.GetSceneSystem().SceneInstance.RootScene;

        return entity;
    }

    /// <summary>
    /// Adds directional lighting from multiple angles to the current scene, enhancing scene illumination.
    /// </summary>
    /// <param name="game">The game instance to which the lighting will be added.</param>
    /// <param name="intensity">The intensity of the light sources.</param>
    /// <param name="showLightGizmo">Whether to show a gizmo for each light. Default is true.</param>
    /// <returns>The six light entities, already in the root scene.</returns>
    /// <remarks>
    /// <para>
    /// Creates six directional lights, one shining along each world axis, for even lighting from all directions.
    /// </para>
    /// </remarks>
    public static IReadOnlyList<Entity> AddAllDirectionsLighting(this IGame game, float intensity = 5, bool showLightGizmo = true)
    {
        var position = new Vector3(7f, 2f, 0);

        // A directional light shines along its entity's forward axis, so these six rotations aim one
        // light down each of the six world axes
        ReadOnlySpan<Quaternion> rotations =
        [
            Quaternion.Identity,
            Quaternion.RotationAxis(Vector3.UnitX, MathUtil.DegreesToRadians(180)),
            Quaternion.RotationAxis(Vector3.UnitX, MathUtil.DegreesToRadians(90)),
            Quaternion.RotationAxis(Vector3.UnitX, MathUtil.DegreesToRadians(270)),
            Quaternion.RotationAxis(Vector3.UnitY, MathUtil.DegreesToRadians(90)),
            Quaternion.RotationAxis(Vector3.UnitY, MathUtil.DegreesToRadians(270))
        ];

        var lights = new List<Entity>(rotations.Length);

        foreach (var rotation in rotations)
        {
            var entity = CreateDirectionalLight(null, intensity, enableShadows: false);

            entity.Transform.Position = position;
            entity.Transform.Rotation = rotation;
            entity.Scene = game.GetSceneSystem().SceneInstance.RootScene;

            if (showLightGizmo)
                entity.AddLightDirectionalGizmo(game.GraphicsDevice);

            lights.Add(entity);
        }

        return lights;
    }

    /// <summary>
    /// Adds a three-point studio lighting rig - key, fill and rim - aimed at the scene's centre.
    /// </summary>
    /// <param name="game">The game instance to which the lighting will be added.</param>
    /// <param name="intensity">The key light's intensity. The fill and rim are derived from it. Default is 15.</param>
    /// <param name="yawDegrees">
    /// Horizontal direction the rig faces, in degrees about the world Y axis. The default of 45 faces
    /// the toolkit's default camera at (6, 6, 6); pass the yaw of your own camera to swing the whole
    /// rig around with it.
    /// </param>
    /// <param name="enableShadows">Whether the key light casts shadows. Only the key ever does. Default is true.</param>
    /// <param name="showLightGizmo">Whether to show a gizmo for each light. Default is false.</param>
    /// <returns>The three light entities - key, fill and rim - for further adjustment.</returns>
    /// <remarks>
    /// <para>
    /// This is the photographer's rig: a bright <b>key</b> light from high on one side of the camera
    /// gives every surface its main shading and the scene its shadows; a dim <b>fill</b> from low on
    /// the other side lifts the key's shadows so they read as shadow rather than black; and a
    /// <b>rim</b> from high behind the subject traces a bright edge along silhouettes, separating
    /// objects from the background. Because the three intensities differ, curved and angled surfaces
    /// shade differently on every face - which is what makes shapes look solid.
    /// </para>
    /// <para>
    /// Compare <see cref="AddAllDirectionsLighting"/>, which lights evenly from all six axes: nothing
    /// is ever dark, but everything is lit the same, so shape flattens. Prefer the studio rig for
    /// showing off models and lettering; prefer all-direction lighting when reading a surface's
    /// colour matters more than reading its form, as on a game board of colour-coded cubes.
    /// </para>
    /// <para>
    /// Directional lights ignore position, so the rig works whatever the scene's scale; positions are
    /// set anyway so the gizmos hover where each light conceptually sits.
    /// </para>
    /// </remarks>
    public static (Entity Key, Entity Fill, Entity Rim) AddStudioLighting(this IGame game, float intensity = 15f, float yawDegrees = 45f, bool enableShadows = true, bool showLightGizmo = false)
    {
        // Azimuth is measured about Y: a light at azimuth a sits on the (sin a, _, cos a) side of the
        // scene and shines toward the centre. Elevation tilts it down from the horizon.
        var key = CreateStudioLight("Studio Key Light", intensity, yawDegrees + 30f, elevationDegrees: 40f, enableShadows);
        var fill = CreateStudioLight("Studio Fill Light", intensity * 0.35f, yawDegrees - 40f, elevationDegrees: 15f, enableShadows: false);
        var rim = CreateStudioLight("Studio Rim Light", intensity * 0.7f, yawDegrees + 180f, elevationDegrees: 55f, enableShadows: false);

        foreach (var entity in (ReadOnlySpan<Entity>)[key, fill, rim])
        {
            entity.Scene = game.GetSceneSystem().SceneInstance.RootScene;

            if (showLightGizmo)
                entity.AddLightDirectionalGizmo(game.GraphicsDevice);
        }

        return (key, fill, rim);
    }

    /// <summary>
    /// A white directional light, not yet in a scene, with the large PCF-filtered shadow map every toolkit light
    /// uses when it casts shadows at all.
    /// </summary>
    private static Entity CreateDirectionalLight(string? name, float intensity, bool enableShadows) => new(name)
    {
        new LightComponent
        {
            Intensity = intensity,
            Type = new LightDirectional
            {
                Color = new ColorRgbProvider(Color.White),
                Shadow =
                {
                    Enabled = enableShadows,
                    Size = LightShadowMapSize.Large,
                    Filter = new LightShadowMapFilterTypePcf { FilterSize = LightShadowMapFilterTypePcfSize.Filter5x5 },
                    PartitionMode = new LightDirectionalShadowMap.PartitionLogarithmic(),
                    ComputeTransmittance = false
                }
            }
        }
    };

    /// <summary>
    /// Creates one light of the studio rig, aimed at the scene centre from the given compass direction.
    /// </summary>
    private static Entity CreateStudioLight(string name, float intensity, float azimuthDegrees, float elevationDegrees, bool enableShadows)
    {
        var entity = CreateDirectionalLight(name, intensity, enableShadows);

        var azimuth = MathUtil.DegreesToRadians(azimuthDegrees);
        var elevation = MathUtil.DegreesToRadians(elevationDegrees);

        // Yaw about Y first, then tilt down - the same composition the toolkit camera uses. A light
        // shines along its forward (-Z) axis, so azimuth 0 with no tilt shines from +Z toward -Z.
        entity.Transform.Rotation = Quaternion.RotationY(azimuth) * Quaternion.RotationX(-elevation);

        // Purely cosmetic for a directional light: park the gizmo where the light conceptually sits
        const float gizmoRadius = 5f;

        var (sin, cos) = MathF.SinCos(azimuth);

        entity.Transform.Position = new Vector3(sin * gizmoRadius, MathF.Tan(elevation) * gizmoRadius, cos * gizmoRadius);

        return entity;
    }
}