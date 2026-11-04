using Stride.BepuPhysics;
using Stride.BepuPhysics.Components;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace E05_3D_PlanetWalker;

/// <summary>
/// Pulls the character on the same entity towards <see cref="Centre"/> once per physics step. The
/// character's up is its <see cref="CharacterComponent.LocalUp"/>, a direction in its own frame, so it
/// stays the default and the character is turned instead, by whoever sets its orientation each frame.
/// </summary>
/// <remarks>
/// The pull is a velocity change of the surface gravity scaled by the inverse square of the distance,
/// as a planet's would be; a character never gets far from the surface, so near enough constant. It is
/// applied here, on the physics step, rather than in a frame's update: a frame may run zero or several
/// steps, and a change per step has to be counted where the steps are.
/// </remarks>
public class PlanetPull : StartupScript, ISimulationUpdate
{
    private CharacterComponent? _character;

    /// <summary>The planet's centre.</summary>
    public Vector3 Centre { get; set; }

    /// <summary>The pull at the surface, in m/s².</summary>
    public float SurfaceGravity { get; set; } = 9.81f;

    /// <summary>The planet's radius, where the pull is <see cref="SurfaceGravity"/>.</summary>
    public float SurfaceRadius { get; set; } = 1f;

    /// <summary>The step's pull on the character, towards the centre.</summary>
    public void SimulationUpdate(BepuSimulation simulation, float simTimeStep)
    {
        _character ??= Entity.Get<CharacterComponent>();

        if (_character is null) return;

        var offset = _character.Position - Centre;
        var distance = MathF.Max(offset.Length(), 0.001f);
        var up = offset / distance;
        var acceleration = SurfaceGravity * (SurfaceRadius * SurfaceRadius) / (distance * distance);

        _character.LinearVelocity -= up * (acceleration * simTimeStep);
    }

    /// <summary>Nothing to read back after the step.</summary>
    public void AfterSimulationUpdate(BepuSimulation simulation, float simTimeStep)
    {
    }
}