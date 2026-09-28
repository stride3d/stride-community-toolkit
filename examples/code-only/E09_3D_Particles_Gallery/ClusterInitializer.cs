using Stride.Core.Mathematics;
using Stride.Particles;
using Stride.Particles.Initializers;

namespace E09_3D_Particles_Gallery;

/// <summary>
/// An initializer of our own for a cluster of engines: every new particle is born in one of
/// several nozzles, so one emitter feeds them all. Added after an emitter's other position
/// initializer, it has the last word on the position.
/// </summary>
/// <remarks>
/// It writes the old position as well as the position. An updater that works out a direction from
/// the two, such as <see cref="Stride.Particles.Modules.UpdaterSpeedToDirection"/>, would otherwise see
/// a new particle jump from the emitter to its nozzle, and draw it for one frame as a long streak
/// across the nozzles.
/// </remarks>
public sealed class ClusterInitializer : ParticleInitializer
{
    /// <summary>Where the nozzles are, in the emitter's space.</summary>
    public IReadOnlyList<Vector3> Nozzles { get; set; } = [Vector3.Zero];

    /// <summary>The radius of a nozzle: a particle is born anywhere on its disc, which faces along X.</summary>
    public float Radius { get; set; } = 0.1f;

    /// <summary>Declares the fields the initializer writes.</summary>
    public ClusterInitializer()
    {
        RequiredFields.Add(ParticleFields.Position);
        RequiredFields.Add(ParticleFields.OldPosition);
        RequiredFields.Add(ParticleFields.RandomSeed);
    }

    /// <inheritdoc/>
    public override void Initialize(ParticlePool pool, int startIdx, int endIdx, int maxCapacity)
    {
        if (Nozzles.Count == 0 || !pool.FieldExists(ParticleFields.Position) || !pool.FieldExists(ParticleFields.OldPosition) || !pool.FieldExists(ParticleFields.RandomSeed)) return;

        var positions = pool.GetField(ParticleFields.Position);
        var oldPositions = pool.GetField(ParticleFields.OldPosition);
        var seeds = pool.GetField(ParticleFields.RandomSeed);

        for (var i = startIdx; i != endIdx; i = (i + 1) % maxCapacity)
        {
            var particle = pool.FromIndex(i);
            var seed = particle.Get(seeds);

            var nozzle = Nozzles[Math.Min((int)(seed.GetFloat(RandomOffset.Offset1A) * Nozzles.Count), Nozzles.Count - 1)];
            var angle = seed.GetFloat(RandomOffset.Offset2A) * MathF.Tau;
            var radius = MathF.Sqrt(seed.GetFloat(RandomOffset.Offset2B)) * Radius;
            var (sin, cos) = MathF.SinCos(angle);

            var position = (nozzle + new Vector3(0f, cos * radius, sin * radius)) * WorldScale.X;

            WorldRotation.Rotate(ref position);

            particle.Set(positions, position + WorldPosition);
            particle.Set(oldPositions, position + WorldPosition);
        }
    }
}