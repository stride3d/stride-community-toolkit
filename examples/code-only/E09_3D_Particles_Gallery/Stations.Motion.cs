using Stride.Core.Mathematics;
using Stride.Particles;
using Stride.Particles.Modules;
using Stride.Particles.Initializers;
using Stride.Particles.ShapeBuilders;
using Stride.Particles.Spawners;
using Stride.Particles.Updaters;
using Stride.Particles.Updaters.FieldShapes;

namespace E09_3D_Particles_Gallery;

/// <summary>Where particles start and what moves them: initializers and updaters.</summary>
public static class MotionStations
{
    /// <summary>
    /// Born along an arc from the emitter to a target point, in order, so one burst draws a bridge
    /// in the air. The arc's height is the variation. A transform can be the target instead of a
    /// fixed point, and then the arc follows it.
    /// </summary>
    public static void Arc(ParticleStation s)
    {
        var v = s.Pick("low arc", "high arc", "random along the arc");

        var emitter = new ParticleEmitter
        {
            ParticleLifetime = new Vector2(1.5f, 1.5f),
            SimulationSpace = EmitterSimulationSpace.World,
            ShapeBuilder = new ShapeBuilderBillboard(),
            Material = ParticleMaterials.Textured(s.Textures.Dot, new Color4(0.5f, 0.9f, 1f, 1f), additive: 1f),
        };

        // The arc spreads its steps over the particles of one spawn call, so it wants a burst: a
        // per-second spawner hands it one particle at a time, and every one lands on the same step
        emitter.Spawners.Add(new SpawnerBurst { SpawnCount = 80, LoopCondition = SpawnerLoopCondition.Looping, Delay = new Vector2(0.4f), Duration = new Vector2(0.01f) });
        emitter.Initializers.Add(new InitialSizeSeed { RandomSize = new Vector2(0.15f, 0.25f) });
        emitter.Initializers.Add(new InitialPositionArc
        {
            FallbackTarget = s.Direction(5f, 0f, 0f),
            ArcHeight = v == 1 ? 4f : 1.5f,
            Sequential = v != 2,
            PositionMin = new Vector3(-0.05f),
            PositionMax = new Vector3(0.05f),
        });

        s.Place(new Vector3(-2.5f, 0.5f, 0f), emitter);
    }

    /// <summary>
    /// A cone of directions from the direction initializer, and the speed-to-direction updater
    /// turning each oriented quad along its velocity so the stream reads as streaks. Narrow the
    /// cone and it is a jet; widen it and it is a spray.
    /// </summary>
    public static void Direction(ParticleStation s)
    {
        var v = s.Pick("narrow cone", "wide cone", "wide cone, round particles");

        var wide = v > 0;

        var emitter = new ParticleEmitter
        {
            ParticleLifetime = new Vector2(1f, 1.4f),
            SimulationSpace = EmitterSimulationSpace.World,
            ShapeBuilder = v == 2 ? new ShapeBuilderBillboard() : new ShapeBuilderOrientedQuad { ScaleLength = true, LengthFactor = 4f },
            Material = ParticleMaterials.Textured(s.Textures.Radial, new Color4(0.9f, 0.95f, 1f, 1f), additive: 1f),
        };

        emitter.Spawners.Add(new SpawnerPerSecond { SpawnCount = 80 });
        emitter.Initializers.Add(Initializers.AtEmitter());
        emitter.Initializers.Add(new InitialSizeSeed { RandomSize = new Vector2(0.12f, 0.2f) });
        // The cone is in the velocity: UpdaterSpeedToDirection turns each particle along the way it moves every
        // update, so a direction seeded on its own would be overwritten before it was seen
        emitter.Initializers.Add(new InitialVelocitySeed
        {
            VelocityMin = wide ? new Vector3(-4f, 6f, -4f) : new Vector3(-0.7f, 6f, -0.7f),
            VelocityMax = wide ? new Vector3(4f, 9f, 4f) : new Vector3(0.7f, 9f, 0.7f),
        });
        emitter.Updaters.Add(new UpdaterGravity { GravitationalAcceleration = new Vector3(0f, -9.8f, 0f) });
        emitter.Updaters.Add(new UpdaterSpeedToDirection());

        s.Place(new Vector3(0f, 0.2f, 0f), emitter);
    }

    /// <summary>
    /// An initial angle from a range, or a spin from a curve over the particle's life, which
    /// replaces that angle rather than adding to it. Sprites with a clear top, like the flame frames, show it best.
    /// </summary>
    public static void Spin(ParticleStation s)
    {
        var v = s.Pick("random angle, no spin", "spin one turn", "spin, slowing to a stop");

        var emitter = new ParticleEmitter
        {
            ParticleLifetime = new Vector2(2f, 2f),
            SimulationSpace = EmitterSimulationSpace.World,
            ShapeBuilder = new ShapeBuilderBillboard(),
            Material = ParticleMaterials.Flipbook(s.Textures.Flame, 8, 8, 64, additive: 1f),
        };

        emitter.Spawners.Add(new SpawnerPerSecond { SpawnCount = 10 });
        emitter.Initializers.Add(new InitialSizeSeed { RandomSize = new Vector2(0.8f, 1.2f) });
        emitter.Initializers.Add(new InitialPositionSeed { PositionMin = new Vector3(-1.5f, 0f, -0.3f), PositionMax = new Vector3(1.5f, 0f, 0.3f) });
        emitter.Initializers.Add(new InitialVelocitySeed { VelocityMin = new Vector3(0f, 0.8f, 0f), VelocityMax = new Vector3(0f, 1.2f, 0f) });
        emitter.Initializers.Add(new InitialRotationSeed { AngularRotation = new Vector2(-180f, 180f) });

        if (v > 0)
        {
            emitter.Updaters.Add(new UpdaterRotationOverTime
            {
                SamplerMain = v == 1 ? Curves.Float((0f, 0f), (1f, 360f)) : Curves.Float((0f, 0f), (0.3f, 300f), (1f, 360f)),
            });
        }

        s.Place(new Vector3(0f, 0.8f, 0f), emitter);
    }

    /// <summary>
    /// A force field: a shape in the world and forces within it - along its axis, around it, away
    /// from it, and a fixed one - falling off towards the shape's edge. A vortex, a repulsor and a
    /// wind are the same updater with different numbers.
    /// </summary>
    public static void ForceField(ParticleStation s)
    {
        var v = s.Pick("vortex", "repulsor", "wind");

        var emitter = new ParticleEmitter
        {
            ParticleLifetime = new Vector2(3f, 4f),
            SimulationSpace = EmitterSimulationSpace.World,
            ShapeBuilder = new ShapeBuilderBillboard(),
            Material = ParticleMaterials.Textured(s.Textures.Dot, new Color4(0.7f, 0.85f, 1f, 1f), additive: 1f),
        };

        emitter.Spawners.Add(new SpawnerPerSecond { SpawnCount = 120 });
        emitter.Initializers.Add(new InitialSizeSeed { RandomSize = new Vector2(0.08f, 0.16f) });
        emitter.Initializers.Add(new InitialPositionSeed { PositionMin = new Vector3(-2f, 0f, -2f), PositionMax = new Vector3(2f, 0.2f, 2f) });
        emitter.Initializers.Add(new InitialVelocitySeed { VelocityMin = new Vector3(-0.2f, 0.5f, -0.2f), VelocityMax = new Vector3(0.2f, 1.5f, 0.2f) });

        var field = new UpdaterForceField
        {
            FieldShape = new Cylinder { Radius = 3f, HalfHeight = 3f },
            FieldFalloff = new FieldFalloff { StrengthInside = 1f, FalloffStart = 0.2f, StrengthOutside = 0.2f, FalloffEnd = 1f },
            EnergyConservation = 0.2f,
        };

        switch (v)
        {
            case 0:
                field.ForceVortex = 8f;
                field.ForceRepulsive = -1f;
                field.ForceDirected = 1.5f;
                break;
            case 1:
                field.ForceVortex = 0f;
                field.ForceRepulsive = 6f;
                field.ForceDirected = 0f;
                break;
            default:
                field.ForceVortex = 0f;
                field.ForceRepulsive = 0f;
                field.ForceDirected = 0f;
                field.ForceFixed = new Vector3(4f, 0.5f, 0f);
                break;
        }

        emitter.Updaters.Add(field);

        s.Place(new Vector3(0f, 0.2f, 0f), 1f, emitter);
    }

    /// <summary>
    /// A collider: a shape particles bounce off, or stay inside when it is hollow. Restitution is
    /// how much bounce survives, friction how much sideways speed is lost. No physics engine is involved;
    /// the updater tests each particle against the shape itself.
    /// </summary>
    public static void Collider(ParticleStation s)
    {
        var v = s.Pick("bouncy floor", "dead floor", "inside a hollow box");

        var emitter = new ParticleEmitter
        {
            ParticleLifetime = new Vector2(3f, 4f),
            SimulationSpace = EmitterSimulationSpace.World,
            ShapeBuilder = new ShapeBuilderBillboard(),
            Material = ParticleMaterials.Textured(s.Textures.Dot, new Color4(1f, 0.8f, 0.3f, 1f), additive: 1f),
        };

        emitter.Spawners.Add(new SpawnerPerSecond { SpawnCount = 60 });
        emitter.Initializers.Add(new InitialSizeSeed { RandomSize = new Vector2(0.1f, 0.18f) });
        emitter.Initializers.Add(new InitialPositionSeed { PositionMin = new Vector3(-0.3f, 0f, -0.3f), PositionMax = new Vector3(0.3f, 0f, 0.3f) });
        emitter.Initializers.Add(new InitialVelocitySeed { VelocityMin = new Vector3(-3f, 2f, -3f), VelocityMax = new Vector3(3f, 6f, 3f) });
        emitter.Updaters.Add(new UpdaterGravity { GravitationalAcceleration = new Vector3(0f, -9.8f, 0f) });

        var collider = v == 2
            ? new UpdaterCollider { FieldShape = new Cube { HalfSideX = 2.5f, HalfSideY = 2f, HalfSideZ = 2.5f }, IsHollow = true, Restitution = 0.8f, Friction = 0.05f }
            : new UpdaterCollider { FieldShape = new Cube { HalfSideX = 4f, HalfSideY = 0.1f, HalfSideZ = 4f }, IsHollow = false, Restitution = v == 0 ? 0.7f : 0.05f, Friction = v == 0 ? 0.1f : 0.6f };

        // The shape sits where the updater says relative to the emitter: a slab under it, or a box around it
        collider.Position = v == 2 ? new Vector3(0f, 0f, 0f) : new Vector3(0f, -2.1f, 0f);
        collider.InheritPosition = true;

        emitter.Updaters.Add(collider);

        s.Place(new Vector3(0f, 2f, 0f), emitter);
    }

    /// <summary>
    /// Spawned by distance travelled rather than by time: a fast emitter leaves as dense a trail as
    /// a slow one, and a still emitter leaves nothing. The comet's head is a second, per-second
    /// emitter, so the two rates can be compared on one path.
    /// </summary>
    public static void Comet(ParticleStation s)
    {
        var v = s.Pick("4 per unit", "16 per unit", "40 per unit");

        var tail = new ParticleEmitter
        {
            ParticleLifetime = new Vector2(1.2f, 1.6f),
            SimulationSpace = EmitterSimulationSpace.World,
            ShapeBuilder = new ShapeBuilderBillboard(),
            Material = ParticleMaterials.Textured(s.Textures.Dot, new Color4(1f, 0.6f, 0.2f, 1f), additive: 1f),
        };

        tail.Spawners.Add(new SpawnerFromDistance { SpawnCount = v switch { 0 => 4f, 1 => 16f, _ => 40f } });
        tail.Initializers.Add(Initializers.AtEmitter());
        tail.Initializers.Add(new InitialSizeSeed { RandomSize = new Vector2(0.1f, 0.22f) });
        tail.Initializers.Add(new InitialVelocitySeed { VelocityMin = new Vector3(-0.3f, -0.3f, -0.3f), VelocityMax = new Vector3(0.3f, 0.3f, 0.3f) });
        tail.Updaters.Add(new UpdaterSizeOverTime { SamplerMain = Curves.Float((0f, 0.18f), (1f, 0f)) });

        var head = new ParticleEmitter
        {
            ParticleLifetime = new Vector2(0.2f, 0.2f),
            SimulationSpace = EmitterSimulationSpace.World,
            ShapeBuilder = new ShapeBuilderBillboard(),
            Material = ParticleMaterials.Textured(s.Textures.Radial, new Color4(1f, 0.9f, 0.6f, 1f), additive: 1f),
        };

        head.Spawners.Add(new SpawnerPerSecond { SpawnCount = 30 });
        head.Initializers.Add(Initializers.AtEmitter());
        head.Initializers.Add(new InitialSizeSeed { RandomSize = new Vector2(0.6f, 0.8f) });

        s.Place(new Vector3(0f, 2f, 0f), tail, head);
    }

    /// <summary>Flies the station's emitter round an orbit that speeds up and slows down, for the comet.</summary>
    public static void Orbit(ParticleStation s)
    {
        if (s.Entity is null) return;

        // Faster on one side than the other, so distance-based spawning has something to show
        var angle = s.Seconds * 1.2f + MathF.Sin(s.Seconds * 1.2f) * 0.8f;
        var (sin, cos) = MathF.SinCos(angle);

        s.Entity.Transform.Position = s.At(cos * 3f, 2.2f + sin * 1.2f, sin * 1.2f);
    }
}