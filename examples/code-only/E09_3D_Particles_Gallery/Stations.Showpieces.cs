using Stride.Core.Mathematics;
using Stride.Particles;
using Stride.Particles.Initializers;
using Stride.Particles.Materials;
using Stride.Particles.Modules;
using Stride.Particles.ShapeBuilders;
using Stride.Particles.Spawners;
using Stride.Particles.Updaters;
using Stride.Particles.Updaters.FieldShapes;

namespace E09_3D_Particles_Gallery;

/// <summary>Several building blocks at once: the effects the system is actually for.</summary>
public static class ShowStations
{
    /// <summary>
    /// A campfire from three emitters in one system: flames from the bonfire flipbook, embers that
    /// rise and fade, and smoke above them that drifts and thins. Each emitter has its own
    /// lifetime, shape and material; they share only the entity.
    /// </summary>
    public static void Campfire(ParticleStation s)
    {
        var v = s.Pick("full", "flames only", "embers and smoke only");

        var flames = new ParticleEmitter
        {
            ParticleLifetime = new Vector2(0.6f, 0.9f),
            SimulationSpace = EmitterSimulationSpace.World,
            ShapeBuilder = new ShapeBuilderBillboard(),
            Material = ParticleMaterials.Flipbook(s.Textures.Bonfire, 8, 8, 64, new Color4(1f, 0.45f, 0.14f, 1f), additive: 0.8f),
        };

        flames.Spawners.Add(new SpawnerPerSecond { SpawnCount = 24 });
        flames.Initializers.Add(new InitialSizeSeed { RandomSize = new Vector2(1.4f, 2f) });
        flames.Initializers.Add(new InitialPositionSeed { PositionMin = new Vector3(-0.4f, 0f, -0.4f), PositionMax = new Vector3(0.4f, 0.2f, 0.4f) });
        flames.Initializers.Add(new InitialVelocitySeed { VelocityMin = new Vector3(-0.2f, 0.8f, -0.2f), VelocityMax = new Vector3(0.2f, 1.4f, 0.2f) });
        flames.Updaters.Add(new UpdaterSizeOverTime { SamplerMain = Curves.Float((0f, 1f), (0.4f, 1.8f), (1f, 0.9f)) });

        var embers = new ParticleEmitter
        {
            ParticleLifetime = new Vector2(1.5f, 3f),
            SimulationSpace = EmitterSimulationSpace.World,
            ShapeBuilder = new ShapeBuilderBillboard(),
            Material = ParticleMaterials.Textured(s.Textures.Dot, new Color4(1f, 0.5f, 0.15f, 1f), additive: 1f),
        };

        embers.Spawners.Add(new SpawnerPerSecond { SpawnCount = 30 });
        embers.Initializers.Add(new InitialSizeSeed { RandomSize = new Vector2(0.05f, 0.12f) });
        embers.Initializers.Add(new InitialPositionSeed { PositionMin = new Vector3(-0.5f, 0f, -0.5f), PositionMax = new Vector3(0.5f, 0.5f, 0.5f) });
        embers.Initializers.Add(new InitialVelocitySeed { VelocityMin = new Vector3(-0.8f, 1.5f, -0.8f), VelocityMax = new Vector3(0.8f, 3.5f, 0.8f) });
        embers.Updaters.Add(new UpdaterForceField
        {
            FieldShape = new Cylinder { Radius = 2f, HalfHeight = 4f },
            FieldFalloff = new FieldFalloff { StrengthInside = 1f, FalloffStart = 0.3f, StrengthOutside = 0f, FalloffEnd = 1f },
            ForceVortex = 1.5f,
            ForceRepulsive = 0f,
            ForceDirected = 1f,
            EnergyConservation = 0.5f,
        });
        embers.Updaters.Add(new UpdaterColorOverTime { SamplerMain = Curves.Color((0f, new Color4(1f, 0.6f, 0.2f, 1f)), (0.7f, new Color4(1f, 0.27f, 0.07f, 1f)), (1f, new Color4(0.5f, 0.1f, 0f, 0f))) });

        var smoke = new ParticleEmitter
        {
            ParticleLifetime = new Vector2(3f, 5f),
            SimulationSpace = EmitterSimulationSpace.World,
            SortingPolicy = EmitterSortingPolicy.ByDepth,
            ShapeBuilder = new ShapeBuilderBillboard(),
            Material = ParticleMaterials.Smoke(s.Textures, new Color4(0.35f, 0.33f, 0.32f, 1f), softEdge: 0.5f),
        };
        smoke.Spawners.Add(new SpawnerPerSecond { SpawnCount = 8 });
        smoke.Initializers.Add(new InitialSizeSeed { RandomSize = new Vector2(1.2f, 1.8f) });
        smoke.Initializers.Add(new InitialPositionSeed { PositionMin = new Vector3(-0.3f, 1.2f, -0.3f), PositionMax = new Vector3(0.3f, 1.6f, 0.3f) });
        smoke.Initializers.Add(new InitialVelocitySeed { VelocityMin = new Vector3(-0.3f, 0.8f, -0.3f), VelocityMax = new Vector3(0.3f, 1.4f, 0.3f) });
        smoke.Initializers.Add(new InitialRotationSeed { AngularRotation = new Vector2(-180f, 180f) });
        smoke.Updaters.Add(new UpdaterSizeOverTime { SamplerMain = Curves.Float((0f, 1f), (1f, 3f)) });
        smoke.Updaters.Add(new UpdaterColorOverTime { SamplerMain = Curves.Color((0f, new Color4(1f, 1f, 1f, 0f)), (0.25f, new Color4(1f, 1f, 1f, 0.55f)), (1f, new Color4(1f, 1f, 1f, 0f))) });

        ParticleEmitter[] emitters = v switch { 0 => [smoke, embers, flames], 1 => [flames], _ => [smoke, embers] };

        s.Place(new Vector3(0f, 0.2f, 0f), 3f, emitters);
    }

    /// <summary>
    /// Fireworks from child emitters: a rocket goes up on a per-second spawner; a trail emitter
    /// spawns from it by distance travelled; an explosion emitter spawns two hundred sparks when a
    /// rocket dies. The children take their position, and the sparks their colour, from the parent
    /// particle - all wiring, no script.
    /// </summary>
    public static void Fireworks(ParticleStation s)
    {
        var v = s.Pick("gold", "mixed colours", "big and slow");

        var big = v == 2;

        var rockets = new ParticleEmitter
        {
            EmitterName = "rockets",
            ParticleLifetime = big ? new Vector2(1.8f, 2.2f) : new Vector2(1.2f, 1.6f),
            SimulationSpace = EmitterSimulationSpace.World,
            ShapeBuilder = new ShapeBuilderBillboard(),
            Material = ParticleMaterials.Textured(s.Textures.Radial, new Color4(1f, 0.9f, 0.6f, 1f), additive: 1f),
        };

        rockets.Spawners.Add(new SpawnerPerSecond { SpawnCount = big ? 0.6f : 1.5f });
        rockets.Initializers.Add(new InitialSizeSeed { RandomSize = new Vector2(0.25f, 0.35f) });
        rockets.Initializers.Add(new InitialPositionSeed { PositionMin = new Vector3(-1.5f, 0f, -1.5f), PositionMax = new Vector3(1.5f, 0f, 1.5f) });
        rockets.Initializers.Add(new InitialVelocitySeed { VelocityMin = new Vector3(-1.5f, big ? 10f : 9f, -1.5f), VelocityMax = new Vector3(1.5f, big ? 12f : 11f, 1.5f) });
        rockets.Initializers.Add(new InitialColorSeed
        {
            ColorMin = v == 1 ? new Color4(0.2f, 0.4f, 1f, 1f) : new Color4(1f, 0.8f, 0.27f, 1f),
            ColorMax = v == 1 ? new Color4(1f, 0.27f, 0.53f, 1f) : new Color4(1f, 0.83f, 0.33f, 1f),
        });
        rockets.Updaters.Add(new UpdaterGravity { GravitationalAcceleration = new Vector3(0f, -4f, 0f) });

        var trail = new ParticleEmitter
        {
            EmitterName = "trail",
            ParticleLifetime = new Vector2(0.4f, 0.7f),
            SimulationSpace = EmitterSimulationSpace.World,
            ShapeBuilder = new ShapeBuilderBillboard(),
            Material = ParticleMaterials.Textured(s.Textures.Dot, new Color4(1f, 0.83f, 0.58f, 1f), additive: 1f),
        };

        trail.Spawners.Add(new SpawnerFromParent { ParentName = "rockets", ParentControlFlag = ParentControlFlag.Group00, ParticleSpawnTrigger = new ParticleSpawnTriggerDistance(), SpawnCount = new Vector2(2f, 4f) });
        trail.Initializers.Add(new InitialPositionParent { ParentName = "rockets", PositionMin = new Vector3(-0.05f), PositionMax = new Vector3(0.05f) });
        trail.Initializers.Add(new InitialSizeSeed { RandomSize = new Vector2(0.08f, 0.14f) });
        trail.Initializers.Add(new InitialVelocitySeed { VelocityMin = new Vector3(-0.4f, -0.5f, -0.4f), VelocityMax = new Vector3(0.4f, 0f, 0.4f) });
        trail.Updaters.Add(new UpdaterSizeOverTime { SamplerMain = Curves.Float((0f, 0.12f), (1f, 0f)) });

        var sparks = new ParticleEmitter
        {
            EmitterName = "sparks",
            ParticleLifetime = big ? new Vector2(1.5f, 2.5f) : new Vector2(0.8f, 1.6f),
            SimulationSpace = EmitterSimulationSpace.World,
            ShapeBuilder = new ShapeBuilderOrientedQuad { ScaleLength = true, LengthFactor = 2.5f },
            Material = ParticleMaterials.Textured(s.Textures.Dot, additive: 1f),
        };

        sparks.Spawners.Add(new SpawnerFromParent { ParentName = "rockets", ParentControlFlag = ParentControlFlag.Group01, ParticleSpawnTrigger = new ParticleSpawnTriggerDeath(), SpawnCount = big ? new Vector2(250f, 250f) : new Vector2(180f, 240f) });
        sparks.Initializers.Add(new InitialPositionParent { ParentName = "rockets", PositionMin = new Vector3(-0.05f), PositionMax = new Vector3(0.05f) });
        sparks.Initializers.Add(new InitialColorParent { ParentName = "rockets" });
        sparks.Initializers.Add(new InitialSizeSeed { RandomSize = new Vector2(0.06f, 0.12f) });
        sparks.Initializers.Add(new InitialVelocitySeed { VelocityMin = new Vector3(big ? -7f : -5f), VelocityMax = new Vector3(big ? 7f : 5f) });
        sparks.Updaters.Add(new UpdaterGravity { GravitationalAcceleration = new Vector3(0f, -3f, 0f) });
        sparks.Updaters.Add(new UpdaterSpeedToDirection());
        sparks.Updaters.Add(new UpdaterColorOverTime { SamplerMain = Curves.Color((0f, new Color4(1f, 1f, 1f, 1f)), (0.6f, Color4.White), (1f, new Color4(1f, 1f, 1f, 0f))) });

        s.Place(new Vector3(0f, 0.2f, 0f), rockets, trail, sparks);
    }

    /// <summary>
    /// A tornado: two thousand dust particles born near the ground and thrown around a tall
    /// cylinder's axis, lifted and pulled inward as they rise. The whole thing is one force field
    /// with a vortex, a negative repulsive force and a directed force, on a warm-up so it stands
    /// there when the visitor arrives.
    /// </summary>
    public static void Tornado(ParticleStation s)
    {
        var v = s.Pick("dust", "sparks", "slow and wide");

        var slow = v == 2;

        var emitter = new ParticleEmitter
        {
            ParticleLifetime = new Vector2(4f, 6f),
            SimulationSpace = EmitterSimulationSpace.World,
            ShapeBuilder = v == 1 ? new ShapeBuilderOrientedQuad { ScaleLength = true, LengthFactor = 3f } : new ShapeBuilderBillboard(),
            Material = v == 1
                ? ParticleMaterials.Textured(s.Textures.Dot, new Color4(1f, 0.73f, 0.33f, 1f), additive: 1f)
                : ParticleMaterials.Smoke(s.Textures, new Color4(0.75f, 0.7f, 0.6f, 1f), additive: 0.15f),
        };

        emitter.Spawners.Add(new SpawnerPerSecond { SpawnCount = 400 });
        emitter.Initializers.Add(new InitialSizeSeed { RandomSize = v == 1 ? new Vector2(0.06f, 0.12f) : new Vector2(0.3f, 0.6f) });
        emitter.Initializers.Add(new InitialPositionSeed { PositionMin = new Vector3(-3f, 0f, -3f), PositionMax = new Vector3(3f, 0.3f, 3f) });
        emitter.Initializers.Add(new InitialVelocitySeed { VelocityMin = new Vector3(-0.5f, 0f, -0.5f), VelocityMax = new Vector3(0.5f, 0.5f, 0.5f) });
        emitter.Updaters.Add(new UpdaterForceField
        {
            FieldShape = new Cylinder { Radius = slow ? 4.5f : 3f, HalfHeight = 5f },
            FieldFalloff = new FieldFalloff { StrengthInside = 1f, FalloffStart = 0.15f, StrengthOutside = 0.35f, FalloffEnd = 1f },
            ForceVortex = slow ? 5f : 11f,
            ForceRepulsive = slow ? -1.5f : -3f,
            ForceDirected = slow ? 1.2f : 2.2f,
            EnergyConservation = 0.35f,
        });

        if (v == 1) emitter.Updaters.Add(new UpdaterSpeedToDirection());

        emitter.Updaters.Add(new UpdaterColorOverTime { SamplerMain = Curves.Color((0f, new Color4(1f, 1f, 1f, 0f)), (0.15f, new Color4(1f, 1f, 1f, 0.8f)), (0.8f, new Color4(1f, 1f, 1f, 0.6f)), (1f, new Color4(1f, 1f, 1f, 0f))) });

        s.Place(new Vector3(0f, 4.5f, 0f), 4f, emitter);
    }

    /// <summary>
    /// Fireflies: a few hundred soft dots pulled towards a point by <see cref="SwarmUpdater"/>, an
    /// updater written for this example. The point wanders; the swarm follows,
    /// lags, overshoots and settles, because that is what the updater's spring does.
    /// </summary>
    public static void Fireflies(ParticleStation s)
    {
        var v = s.Pick("loose swarm", "tight swarm", "many");

        var emitter = new ParticleEmitter
        {
            ParticleLifetime = new Vector2(6f, 9f),
            SimulationSpace = EmitterSimulationSpace.World,
            ShapeBuilder = new ShapeBuilderBillboard(),
            Material = ParticleMaterials.Textured(s.Textures.Dot, new Color4(0.75f, 1f, 0.25f, 1f), additive: 1f),
        };

        emitter.Spawners.Add(new SpawnerPerSecond { SpawnCount = v == 2 ? 120 : 40 });
        emitter.Initializers.Add(new InitialSizeSeed { RandomSize = new Vector2(0.06f, 0.14f) });
        emitter.Initializers.Add(new InitialPositionSeed { PositionMin = new Vector3(-3f, 0f, -3f), PositionMax = new Vector3(3f, 3f, 3f) });
        emitter.Updaters.Add(new SwarmUpdater { Strength = v == 1 ? 12f : 5f, Damping = v == 1 ? 0.2f : 0.4f, Spread = v == 1 ? 0.7f : 1.8f });
        emitter.Updaters.Add(new UpdaterSizeOverTime { SamplerMain = Curves.Float((0f, 0f), (0.1f, 0.22f), (0.5f, 0.08f), (0.6f, 0.22f), (0.9f, 0.1f), (1f, 0f)) });

        s.Place(new Vector3(0f, 2.5f, 0f), 3f, emitter);
    }

    /// <summary>Moves the fireflies' target on a slow figure of eight, for <see cref="Fireflies"/>.</summary>
    public static void Wander(ParticleStation s)
    {
        if (s.Particles is not { } particles) return;

        var t = s.Seconds * 0.5f;
        var target = s.At(MathF.Sin(t) * 3f, 2.5f + MathF.Sin(t * 2f) * 1f, MathF.Sin(t * 2f) * 1.5f);

        foreach (var updater in particles.ParticleSystem.Emitters[0].Updaters)
        {
            if (updater is SwarmUpdater swarm) swarm.Target = target;
        }
    }

    /// <summary>
    /// Lasers, the engine sample's trick: ribbons in the emitter's local space, so the whole beam
    /// turns with the entity instead of trailing behind it, a scrolling additive texture for the
    /// movement along the beam, and a bright core ribbon inside a wider faint one.
    /// </summary>
    public static void Lasers(ParticleStation s)
    {
        var v = s.Pick("red", "cyan", "three beams");

        var colour = v == 1 ? new Color4(0.15f, 0.75f, 1f, 1f) : new Color4(1f, 0.15f, 0.1f, 1f);
        List<ParticleEmitter> emitters = [];
        var beams = v == 2 ? 3 : 1;

        for (var b = 0; b < beams; b++)
        {
            var tint = v == 2 ? b switch { 0 => new Color4(1f, 0.15f, 0.1f, 1f), 1 => new Color4(0.15f, 1f, 0.2f, 1f), _ => new Color4(0.15f, 0.3f, 1f, 1f) } : colour;
            var direction = Quaternion.RotationY(b * MathF.Tau / beams);

            foreach (var (width, alpha, name) in new[] { (0.9f, 0.5f, "halo"), (0.25f, 1f, "core") })
            {
                var beam = new ParticleEmitter
                {
                    EmitterName = $"{name}{b}",
                    ParticleLifetime = new Vector2(1.5f, 1.5f),
                    SimulationSpace = EmitterSimulationSpace.Local,
                    SortingPolicy = EmitterSortingPolicy.ByOrder,
                    ShapeBuilder = new ShapeBuilderRibbon { SmoothingPolicy = SmoothingPolicy.Fast, Segments = 3, TextureCoordinatePolicy = TextureCoordinatePolicy.Stretched },
                    Material = ParticleMaterials.Textured(s.Textures.Radial, tint * alpha, additive: 1f, uv: new UVBuilderScroll { StartFrame = new Vector4(0f, 0f, 1f, 1f), EndFrame = new Vector4(0f, 3f, 1f, 4f) }),
                };

                var velocity = Vector3.Transform(new Vector3(0f, 0f, 7f), direction);

                beam.Spawners.Add(new SpawnerPerFrame { SpawnCount = 1 });
                beam.Initializers.Add(new InitialSpawnOrder());
                beam.Initializers.Add(new InitialSizeSeed { RandomSize = new Vector2(width, width) });
                beam.Initializers.Add(new InitialVelocitySeed { VelocityMin = velocity, VelocityMax = velocity });

                emitters.Add(beam);
            }
        }

        s.Place(new Vector3(0f, 2.5f, 0f), 2f, [.. emitters]);
    }

    /// <summary>Turns the station's entity slowly, for the lasers, which turn with it because they live in its space.</summary>
    public static void Turn(ParticleStation s)
    {
        if (s.Entity is null) return;

        s.Entity.Transform.Rotation = s.FacingRotation() * Quaternion.RotationYawPitchRoll(s.Seconds * 0.6f, MathF.Sin(s.Seconds * 0.9f) * 0.35f, 0f);
    }
}