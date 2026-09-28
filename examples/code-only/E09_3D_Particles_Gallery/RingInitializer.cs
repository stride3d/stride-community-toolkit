using Stride.Core.Mathematics;
using Stride.Particles;
using Stride.Particles.Initializers;

namespace E09_3D_Particles_Gallery;

/// <summary>
/// An initializer of our own: new particles start on a ring in the emitter's XY plane, evenly
/// spaced by spawn order, moving along the ring. What a portal, a halo or a magic circle needs and
/// none of the engine's initializers gives, in thirty lines.
/// </summary>
public sealed class RingInitializer : ParticleInitializer
{
    /// <summary>The ring's radius, in the emitter's units.</summary>
    public float Radius { get; set; } = 2f;

    /// <summary>Speed along the ring, in units per second; negative goes the other way.</summary>
    public float Speed { get; set; } = 1f;

    /// <summary>How far off the ring a particle may start, so the ring has a thickness.</summary>
    public float Thickness { get; set; } = 0.1f;

    /// <summary>An offset into the particle's random seed, so two ring initializers on one emitter do not agree.</summary>
    public uint SeedOffset { get; set; }

    /// <summary>Declares the fields the initializer writes.</summary>
    public RingInitializer()
    {
        RequiredFields.Add(ParticleFields.Position);
        RequiredFields.Add(ParticleFields.OldPosition);
        RequiredFields.Add(ParticleFields.Velocity);
        RequiredFields.Add(ParticleFields.RandomSeed);
    }

    /// <inheritdoc/>
    public override void Initialize(ParticlePool pool, int startIdx, int endIdx, int maxCapacity)
    {
        if (!pool.FieldExists(ParticleFields.Position) || !pool.FieldExists(ParticleFields.Velocity) || !pool.FieldExists(ParticleFields.RandomSeed)) return;

        var positions = pool.GetField(ParticleFields.Position);
        var oldPositions = pool.GetField(ParticleFields.OldPosition);
        var velocities = pool.GetField(ParticleFields.Velocity);
        var seeds = pool.GetField(ParticleFields.RandomSeed);

        for (var i = startIdx; i != endIdx; i = (i + 1) % maxCapacity)
        {
            var particle = pool.FromIndex(i);
            var seed = particle.Get(seeds);

            // Around the ring by seed, so a burst covers it evenly and a stream fills it over time
            var angle = seed.GetFloat(RandomOffset.Offset2A + SeedOffset) * MathF.Tau;
            var radius = Radius + (seed.GetFloat(RandomOffset.Offset2B + SeedOffset) - 0.5f) * Thickness * 2f;
            var (sin, cos) = MathF.SinCos(angle);

            var position = new Vector3(cos * radius, sin * radius, 0f) * WorldScale.X;
            var velocity = new Vector3(-sin, cos, 0f) * Speed;

            WorldRotation.Rotate(ref position);
            WorldRotation.Rotate(ref velocity);

            particle.Set(positions, position + WorldPosition);

            // The old position too: an updater that reads a direction from the two must not see a jump
            particle.Set(oldPositions, position + WorldPosition);
            particle.Set(velocities, velocity);
        }
    }
}