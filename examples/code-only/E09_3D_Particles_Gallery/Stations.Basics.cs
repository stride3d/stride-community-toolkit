using Stride.Core.Mathematics;
using Stride.Particles;
using Stride.Particles.Modules;
using Stride.Particles.Initializers;
using Stride.Particles.ShapeBuilders;
using Stride.Particles.Spawners;
using Stride.Particles.Updaters;

namespace E09_3D_Particles_Gallery;

/// <summary>The spawners and the emitter settings: how many, when, and for how long.</summary>
public static class BasicStations
{
    /// <summary>
    /// The original example, unchanged in spirit: particles launched upward from a
    /// small area and pulled back by gravity. One spawner, three initializers, one updater. The
    /// variation is the spawn rate, so the same fountain is a trickle or a torrent.
    /// </summary>
    public static void Fountain(ParticleStation s)
    {
        var v = s.Pick("20 a second", "50 a second", "200 a second");

        var emitter = new ParticleEmitter
        {
            ParticleLifetime = new Vector2(0.9f, 1.3f),
            SimulationSpace = EmitterSimulationSpace.World,
            ShapeBuilder = new ShapeBuilderBillboard(),
            Material = ParticleMaterials.Flat(new Color4(0.3f, 0.5f, 1f, 1f)),
        };

        emitter.Spawners.Add(new SpawnerPerSecond { SpawnCount = v switch { 0 => 20, 1 => 50, _ => 200 } });
        emitter.Initializers.Add(new InitialSizeSeed { RandomSize = new Vector2(0.08f, 0.25f) });
        emitter.Initializers.Add(new InitialPositionSeed { PositionMin = new Vector3(-0.05f), PositionMax = new Vector3(0.05f) });
        emitter.Initializers.Add(new InitialVelocitySeed { VelocityMin = new Vector3(-1f, 5f, -1f), VelocityMax = new Vector3(1f, 7f, 1f) });
        emitter.Updaters.Add(new UpdaterGravity { GravitationalAcceleration = new Vector3(0f, -9.8f, 0f) });

        s.Place(new Vector3(0f, 0.2f, 0f), emitter);
    }

    /// <summary>
    /// A burst: every particle of the batch at once. Once and never again, or on a loop - a delay
    /// then a burst, over and over - which is a spawner's loop condition, not a script.
    /// </summary>
    public static void Burst(ParticleStation s)
    {
        var v = s.Pick("once", "every two seconds", "every half second");

        var emitter = new ParticleEmitter
        {
            ParticleLifetime = new Vector2(1.2f, 1.8f),
            SimulationSpace = EmitterSimulationSpace.World,
            ShapeBuilder = new ShapeBuilderBillboard(),
            Material = ParticleMaterials.Textured(s.Textures.Dot, new Color4(1f, 0.85f, 0.3f, 1f), additive: 1f),
        };

        var burst = new SpawnerBurst { SpawnCount = 300 };

        if (v > 0)
        {
            burst.LoopCondition = SpawnerLoopCondition.Looping;
            burst.Delay = new Vector2(v == 1 ? 2f : 0.5f);
            burst.Duration = new Vector2(0.01f);
        }

        emitter.Spawners.Add(burst);
        emitter.Initializers.Add(Initializers.AtEmitter());
        emitter.Initializers.Add(new InitialSizeSeed { RandomSize = new Vector2(0.12f, 0.22f) });
        emitter.Initializers.Add(new InitialVelocitySeed { VelocityMin = new Vector3(-4f, 1f, -4f), VelocityMax = new Vector3(4f, 7f, 4f) });
        emitter.Updaters.Add(new UpdaterGravity { GravitationalAcceleration = new Vector3(0f, -6f, 0f) });
        emitter.Updaters.Add(new UpdaterSizeOverTime { SamplerMain = Curves.Float((0f, 0.18f), (0.8f, 0.18f), (1f, 0f)) });

        s.Place(new Vector3(0f, 1f, 0f), emitter);
    }

    /// <summary>
    /// A count per frame rather than per second: the same on a fast machine and a slow one in
    /// particles per frame, and therefore not in particles per second. Use it when the look must
    /// hold per frame - a trail behind something drawn once per frame - and per-second otherwise.
    /// </summary>
    public static void PerFrame(ParticleStation s)
    {
        var v = s.Pick("1 a frame", "5 a frame", "20 a frame");

        var emitter = new ParticleEmitter
        {
            ParticleLifetime = new Vector2(1f, 1f),
            SimulationSpace = EmitterSimulationSpace.World,
            ShapeBuilder = new ShapeBuilderBillboard(),
            Material = ParticleMaterials.Flat(new Color4(0.6f, 1f, 0.6f, 1f)),
        };

        emitter.Spawners.Add(new SpawnerPerFrame { SpawnCount = v switch { 0 => 1, 1 => 5, _ => 20 } });
        emitter.Initializers.Add(new InitialSizeSeed { RandomSize = new Vector2(0.1f, 0.2f) });
        emitter.Initializers.Add(new InitialPositionSeed { PositionMin = new Vector3(-1.5f, 0f, -0.2f), PositionMax = new Vector3(1.5f, 0f, 0.2f) });
        emitter.Initializers.Add(new InitialVelocitySeed { VelocityMin = new Vector3(0f, 2f, 0f), VelocityMax = new Vector3(0f, 3f, 0f) });

        s.Place(new Vector3(0f, 0.3f, 0f), emitter);
    }

    /// <summary>
    /// How long a particle lives is the emitter's, not the spawner's, and it is a range. The
    /// third variation adds a warm-up: the system simulates two seconds before its first frame, so
    /// a steady stream is already steady when it appears instead of starting from nothing.
    /// </summary>
    public static void Lifetime(ParticleStation s)
    {
        var v = s.Pick("half a second", "one to three seconds", "two seconds, warmed up");

        var emitter = new ParticleEmitter
        {
            ParticleLifetime = v switch { 0 => new Vector2(0.5f, 0.5f), 1 => new Vector2(1f, 3f), _ => new Vector2(2f, 2f) },
            SimulationSpace = EmitterSimulationSpace.World,
            ShapeBuilder = new ShapeBuilderBillboard(),
            Material = ParticleMaterials.Smoke(s.Textures, new Color4(0.8f, 0.9f, 1f, 1f)),
        };

        emitter.Spawners.Add(new SpawnerPerSecond { SpawnCount = 30 });
        emitter.Initializers.Add(Initializers.AtEmitter());
        emitter.Initializers.Add(new InitialSizeSeed { RandomSize = new Vector2(0.3f, 0.6f) });
        emitter.Initializers.Add(new InitialVelocitySeed { VelocityMin = new Vector3(-0.3f, 1.5f, -0.3f), VelocityMax = new Vector3(0.3f, 2.5f, 0.3f) });
        emitter.Updaters.Add(new UpdaterColorOverTime { SamplerMain = Curves.Color((0f, new Color4(1f, 1f, 1f, 0f)), (0.15f, Color4.White), (1f, new Color4(1f, 1f, 1f, 0f))) });

        s.Place(new Vector3(0f, 0.2f, 0f), v == 2 ? 2f : 0f, emitter);
    }

    /// <summary>
    /// Two emitters through each other: blue alpha-blended puffs and an orange additive glow.
    /// Alpha blending needs the particles drawn back to front, which is the emitter's sorting
    /// policy; additive blending does not care about order, which is why glows and sparks are
    /// cheap. The variation is the sorting: none, by depth, by age.
    /// </summary>
    public static void BlendAndSort(ParticleStation s)
    {
        var v = s.Pick("unsorted", "sorted by depth", "sorted by age");
        var sorting = v switch { 0 => EmitterSortingPolicy.None, 1 => EmitterSortingPolicy.ByDepth, _ => EmitterSortingPolicy.ByAge };

        var puffs = new ParticleEmitter
        {
            ParticleLifetime = new Vector2(2f, 3f),
            SimulationSpace = EmitterSimulationSpace.World,
            SortingPolicy = sorting,
            ShapeBuilder = new ShapeBuilderBillboard(),
            Material = ParticleMaterials.Smoke(s.Textures, new Color4(0.35f, 0.55f, 1f, 1f)),
        };

        puffs.Spawners.Add(new SpawnerPerSecond { SpawnCount = 12 });
        puffs.Initializers.Add(new InitialSizeSeed { RandomSize = new Vector2(0.8f, 1.4f) });
        puffs.Initializers.Add(new InitialPositionSeed { PositionMin = new Vector3(-1f, 0f, -1f), PositionMax = new Vector3(1f, 0.5f, 1f) });
        puffs.Initializers.Add(new InitialVelocitySeed { VelocityMin = new Vector3(-0.2f, 0.6f, -0.2f), VelocityMax = new Vector3(0.2f, 1f, 0.2f) });

        var glow = new ParticleEmitter
        {
            ParticleLifetime = new Vector2(1f, 1.5f),
            SimulationSpace = EmitterSimulationSpace.World,
            DrawPriority = 1,
            ShapeBuilder = new ShapeBuilderBillboard(),
            Material = ParticleMaterials.Textured(s.Textures.Radial, new Color4(1f, 0.5f, 0.1f, 1f), additive: 1f),
        };

        glow.Spawners.Add(new SpawnerPerSecond { SpawnCount = 20 });
        glow.Initializers.Add(new InitialSizeSeed { RandomSize = new Vector2(0.4f, 0.9f) });
        glow.Initializers.Add(new InitialPositionSeed { PositionMin = new Vector3(-1f, 0f, -1f), PositionMax = new Vector3(1f, 0.5f, 1f) });
        glow.Initializers.Add(new InitialVelocitySeed { VelocityMin = new Vector3(-0.2f, 1f, -0.2f), VelocityMax = new Vector3(0.2f, 2f, 0.2f) });
        glow.Updaters.Add(new UpdaterSizeOverTime { SamplerMain = Curves.Float((0f, 0.15f), (0.3f, 0.8f), (1f, 0f)) });

        s.Place(new Vector3(0f, 0.6f, 0f), 1f, puffs, glow);
    }
}