using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Particles;
using Stride.Particles.Initializers;
using Stride.Particles.Modules;
using Stride.Particles.ShapeBuilders;
using Stride.Particles.Spawners;
using Stride.Particles.Updaters;
using Stride.Particles.Updaters.FieldShapes;

namespace E09_3D_Particles_Gallery;

/// <summary>Set pieces: weather, a portal and a rocket engine, each several emitters and a piece of scenery.</summary>
public static class SetPieceStations
{
    /// <summary>
    /// Rain that splashes: long oriented quads fall on a collider slab, and a second emitter spawns
    /// a few droplets wherever a drop hits - the collision trigger of a child spawner. The parent
    /// dies on impact, so nothing goes through the floor.
    /// </summary>
    public static void Rain(ParticleStation s)
    {
        var v = s.Pick("drizzle", "downpour", "bouncing hail");

        var hail = v == 2;

        var drops = new ParticleEmitter
        {
            EmitterName = "drops",
            ParticleLifetime = new Vector2(1.6f, 2f),
            SimulationSpace = EmitterSimulationSpace.World,
            ShapeBuilder = hail ? new ShapeBuilderBillboard() : new ShapeBuilderOrientedQuad { ScaleLength = true, LengthFactor = 8f },
            Material = ParticleMaterials.Textured(s.Textures.Radial, new Color4(0.7f, 0.85f, 1f, 1f), additive: 0.7f),
        };

        drops.Spawners.Add(new SpawnerPerSecond { SpawnCount = v == 1 ? 400 : 120 });
        drops.Initializers.Add(new InitialSizeSeed { RandomSize = hail ? new Vector2(0.1f, 0.18f) : new Vector2(0.05f, 0.09f) });
        drops.Initializers.Add(new InitialPositionSeed { PositionMin = new Vector3(-3.5f, 0f, -3.5f), PositionMax = new Vector3(3.5f, 0.5f, 3.5f) });
        drops.Initializers.Add(new InitialVelocitySeed { VelocityMin = new Vector3(-0.5f, -12f, -0.5f), VelocityMax = new Vector3(0.5f, -9f, 0.5f) });
        drops.Updaters.Add(new UpdaterGravity { GravitationalAcceleration = new Vector3(0f, -9.8f, 0f) });
        drops.Updaters.Add(new UpdaterCollider
        {
            FieldShape = new Cube { HalfSideX = 5f, HalfSideY = 0.1f, HalfSideZ = 5f },
            Position = new Vector3(0f, -7f, 0f),
            InheritPosition = true,
            KillParticles = !hail,
            Restitution = hail ? 0.6f : 0f,
            Friction = 0.2f,
        });

        if (!hail) drops.Updaters.Add(new UpdaterSpeedToDirection());

        var splash = new ParticleEmitter
        {
            EmitterName = "splash",
            ParticleLifetime = new Vector2(0.25f, 0.45f),
            SimulationSpace = EmitterSimulationSpace.World,
            ShapeBuilder = new ShapeBuilderBillboard(),
            Material = ParticleMaterials.Textured(s.Textures.Dot, new Color4(0.8f, 0.9f, 1f, 1f), additive: 1f),
        };

        splash.Spawners.Add(new SpawnerFromParent { ParentName = "drops", ParticleSpawnTrigger = new ParticleSpawnTriggerCollision(), SpawnCount = new Vector2(3f, 6f) });
        splash.Initializers.Add(new InitialPositionParent { ParentName = "drops", PositionMin = new Vector3(-0.05f), PositionMax = new Vector3(0.05f) });
        splash.Initializers.Add(new InitialSizeSeed { RandomSize = new Vector2(0.04f, 0.08f) });
        splash.Initializers.Add(new InitialVelocitySeed { VelocityMin = new Vector3(-1.5f, 1f, -1.5f), VelocityMax = new Vector3(1.5f, 2.5f, 1.5f) });
        splash.Updaters.Add(new UpdaterGravity { GravitationalAcceleration = new Vector3(0f, -9.8f, 0f) });

        s.Place(new Vector3(0f, 7f, 0f), 2f, drops, splash);
    }

    /// <summary>
    /// A portal: a ring of glowing hexagons from <see cref="RingInitializer"/>, an initializer
    /// written for this example, sparks thrown off the ring and pulled back by a repulsor field with
    /// a negative sign, and a soft glow behind. Stands upright and faces the visitor.
    /// </summary>
    public static void Portal(ParticleStation s)
    {
        var v = s.Pick("violet", "gold", "wide and fast");

        var wide = v == 2;
        var tint = v == 1 ? new Color4(2f, 1.5f, 0.5f, 1f) : new Color4(1.2f, 0.5f, 2f, 1f);

        var ring = new ParticleEmitter
        {
            ParticleLifetime = new Vector2(2f, 2f),
            SimulationSpace = EmitterSimulationSpace.World,
            ShapeBuilder = new ShapeBuilderHexagon { SamplerRotation = Curves.Float((0f, 0f), (1f, 180f)) },
            Material = ParticleMaterials.Flat(tint, additive: 1f),
        };

        ring.Spawners.Add(new SpawnerPerSecond { SpawnCount = wide ? 160 : 120 });
        ring.Initializers.Add(new InitialSizeSeed { RandomSize = new Vector2(0.08f, 0.14f) });
        ring.Initializers.Add(new RingInitializer { Radius = wide ? 3f : 2.2f, Speed = wide ? 3f : 1.5f, Thickness = 0.12f });
        ring.Updaters.Add(new UpdaterSizeOverTime { SamplerMain = Curves.Float((0f, 0f), (0.2f, 0.28f), (0.8f, 0.28f), (1f, 0f)) });

        var sparks = new ParticleEmitter
        {
            ParticleLifetime = new Vector2(1f, 1.8f),
            SimulationSpace = EmitterSimulationSpace.World,
            ShapeBuilder = new ShapeBuilderBillboard(),
            Material = ParticleMaterials.Textured(s.Textures.Dot, tint, additive: 1f),
        };

        sparks.Spawners.Add(new SpawnerPerSecond { SpawnCount = 90 });
        sparks.Initializers.Add(new InitialSizeSeed { RandomSize = new Vector2(0.04f, 0.1f) });
        sparks.Initializers.Add(new RingInitializer { Radius = wide ? 3f : 2.2f, Speed = 0f, Thickness = 0.05f });
        sparks.Initializers.Add(new InitialVelocitySeed { VelocityMin = new Vector3(-1.5f, -1.5f, -0.5f), VelocityMax = new Vector3(1.5f, 1.5f, 0.5f) });
        sparks.Updaters.Add(new UpdaterForceField
        {
            FieldShape = new Sphere { Radius = wide ? 4.5f : 3.5f },
            FieldFalloff = new FieldFalloff { StrengthInside = 1f, FalloffStart = 0f, StrengthOutside = 1f, FalloffEnd = 1f },
            ForceVortex = 2f,
            ForceRepulsive = -2.5f,
            ForceDirected = 0f,
            EnergyConservation = 0.3f,
        });
        sparks.Updaters.Add(new UpdaterColorOverTime { SamplerMain = Curves.Color((0f, new Color4(1.5f, 1.5f, 1.5f, 1f)), (1f, new Color4(1f, 1f, 1f, 0f))) });

        var glow = new ParticleEmitter
        {
            ParticleLifetime = new Vector2(1.5f, 1.5f),
            SimulationSpace = EmitterSimulationSpace.World,
            ShapeBuilder = new ShapeBuilderBillboard(),
            Material = ParticleMaterials.Textured(s.Textures.Radial, tint * 0.8f, additive: 1f),
        };

        glow.Spawners.Add(new SpawnerPerSecond { SpawnCount = 6 });
        glow.Initializers.Add(Initializers.AtEmitter());
        glow.Initializers.Add(new InitialSizeSeed { RandomSize = wide ? new Vector2(5f, 5.5f) : new Vector2(3.6f, 4f) });
        glow.Updaters.Add(new UpdaterColorOverTime { SamplerMain = Curves.Color((0f, new Color4(1f, 1f, 1f, 0f)), (0.5f, Color4.White), (1f, new Color4(1f, 1f, 1f, 0f))) });

        s.Place(new Vector3(0f, 3f, 0f), 2f, glow, ring, sparks);
    }
    /// <summary>
    /// A landing burn, a rocket engine firing down at the pad: a white-blue core of stretched quads at the throat,
    /// an orange plume of flame frames around it, sparks thrown clear, and exhaust smoke that hits
    /// the ground through a collider and rolls out across the pad. The variations are how much the
    /// engine smokes - a clean burn, a sooty one - and an afterburner with a longer, hotter core.
    /// The flame ends above the pad, so all of it shows; the smoke is what reaches the ground.
    /// </summary>
    public static void RocketEngine(ParticleStation s)
    {
        // How high the nozzle's exit is above the pad. Everything below is measured from the exit.
        const float Height = 4.6f;

        var v = s.Pick("clean burn, little smoke", "sooty, lots of smoke", "landing burn: dust and mach diamonds");

        var sooty = v == 1;
        var afterburner = v == 2;

        // The engine itself, once: a nozzle cone with the throat up, and the body standing on it. A cone's
        // size is its radius and its height; a cylinder's is its radius in X and its length in Z.
        if (s.State is null)
        {
            const float NozzleLength = 1f;
            const float BodyLength = 2.6f;

            var nozzle = s.Game.Create3DPrimitive(PrimitiveModelType.Cone, new Primitive3DEntityOptions
            {
                EntityName = $"Station {s.Number} nozzle",
                Material = s.Game.CreateMaterial(new Color(70, 72, 78), metalness: 0.5f, glossiness: 0.7f),
                Size = new Vector3(0.6f, NozzleLength, 0f),
                Position = s.At(0f, Height + NozzleLength / 2f, 0f),
            });

            var body = s.Game.Create3DPrimitive(PrimitiveModelType.Cylinder, new Primitive3DEntityOptions
            {
                EntityName = $"Station {s.Number} body",
                Material = s.Game.CreateMaterial(new Color(200, 205, 210), metalness: 0.3f, glossiness: 0.6f),
                Size = new Vector3(0.45f, 0f, BodyLength),
                Position = s.At(0f, Height + NozzleLength + BodyLength / 2f, 0f),
            });

            nozzle.Scene = s.Scene;
            body.Scene = s.Scene;
            s.State = (nozzle, body);
        }

        var core = new ParticleEmitter
        {
            ParticleLifetime = afterburner ? new Vector2(0.2f, 0.27f) : new Vector2(0.2f, 0.28f),
            SimulationSpace = EmitterSimulationSpace.World,
            // The length is the factor times the size times the speed, so a fast particle needs a small factor
            ShapeBuilder = new ShapeBuilderOrientedQuad { ScaleLength = true, LengthFactor = 0.3f },
            Material = ParticleMaterials.Textured(s.Textures.Radial, afterburner ? new Color4(2.5f, 2f, 3f, 1f) : new Color4(1.6f, 1.9f, 2.6f, 1f), additive: 1f),
        };

        core.Spawners.Add(new SpawnerPerSecond { SpawnCount = afterburner ? 420 : 300 });
        core.Initializers.Add(new InitialSizeSeed { RandomSize = new Vector2(0.3f, 0.45f) });
        core.Initializers.Add(new InitialPositionSeed { PositionMin = new Vector3(-0.15f, 0f, -0.15f), PositionMax = new Vector3(0.15f, 0f, 0.15f) });
        core.Initializers.Add(new InitialVelocitySeed { VelocityMin = new Vector3(-0.6f, afterburner ? -16f : -13f, -0.6f), VelocityMax = new Vector3(0.6f, afterburner ? -13f : -10f, 0.6f) });
        core.Updaters.Add(new UpdaterSpeedToDirection());
        core.Updaters.Add(new UpdaterSizeOverTime { SamplerMain = Curves.Float((0f, 0.5f), (0.7f, 0.4f), (1f, 0f)) });

        var plume = new ParticleEmitter
        {
            ParticleLifetime = new Vector2(0.4f, 0.6f),
            SimulationSpace = EmitterSimulationSpace.World,
            ShapeBuilder = new ShapeBuilderBillboard(),
            Material = ParticleMaterials.Flipbook(s.Textures.Flame, 8, 8, 64, new Color4(2.2f, 1.1f, 0.35f, 1f), additive: 1f),
        };

        plume.Spawners.Add(new SpawnerPerSecond { SpawnCount = afterburner ? 160 : 110 });
        plume.Initializers.Add(new InitialSizeSeed { RandomSize = new Vector2(0.7f, 1.1f) });
        plume.Initializers.Add(new InitialPositionSeed { PositionMin = new Vector3(-0.3f, -0.3f, -0.3f), PositionMax = new Vector3(0.3f, 0f, 0.3f) });
        plume.Initializers.Add(new InitialVelocitySeed { VelocityMin = new Vector3(-1f, -7.5f, -1f), VelocityMax = new Vector3(1f, -5f, 1f) });
        plume.Initializers.Add(new InitialRotationSeed { AngularRotation = new Vector2(170f, 190f) });
        plume.Updaters.Add(new UpdaterSizeOverTime { SamplerMain = Curves.Float((0f, 0.5f), (0.4f, 1.3f), (1f, 0.6f)) });
        plume.Updaters.Add(new UpdaterColorOverTime { SamplerMain = Curves.Color((0f, Color4.White), (0.5f, new Color4(1f, 0.7f, 0.5f, 0.8f)), (1f, new Color4(0.6f, 0.2f, 0.05f, 0f))) });

        var sparks = new ParticleEmitter
        {
            ParticleLifetime = new Vector2(0.6f, 1.3f),
            SimulationSpace = EmitterSimulationSpace.World,
            ShapeBuilder = new ShapeBuilderOrientedQuad { ScaleLength = true, LengthFactor = 3f },
            Material = ParticleMaterials.Textured(s.Textures.Dot, new Color4(2f, 1.3f, 0.5f, 1f), additive: 1f),
        };

        sparks.Spawners.Add(new SpawnerPerSecond { SpawnCount = sooty ? 220 : 120 });
        sparks.Initializers.Add(new InitialSizeSeed { RandomSize = new Vector2(0.04f, 0.09f) });
        sparks.Initializers.Add(new InitialPositionSeed { PositionMin = new Vector3(-0.2f, 0f, -0.2f), PositionMax = new Vector3(0.2f, 0f, 0.2f) });
        sparks.Initializers.Add(new InitialVelocitySeed { VelocityMin = new Vector3(-3.5f, -12f, -3.5f), VelocityMax = new Vector3(3.5f, -7f, 3.5f) });
        sparks.Updaters.Add(new UpdaterGravity { GravitationalAcceleration = new Vector3(0f, -9.8f, 0f) });
        sparks.Updaters.Add(new UpdaterSpeedToDirection());
        sparks.Updaters.Add(new UpdaterCollider
        {
            FieldShape = new Cube { HalfSideX = 7f, HalfSideY = 0.1f, HalfSideZ = 7f },
            Position = new Vector3(0f, -Height - 0.05f, 0f),
            InheritPosition = true,
            Restitution = 0.45f,
            Friction = 0.2f,
        });

        var smoke = new ParticleEmitter
        {
            ParticleLifetime = new Vector2(2.5f, 4f),
            SimulationSpace = EmitterSimulationSpace.World,
            SortingPolicy = EmitterSortingPolicy.ByDepth,
            ShapeBuilder = new ShapeBuilderBillboard(),
            Material = ParticleMaterials.Smoke(s.Textures, sooty ? new Color4(0.22f, 0.2f, 0.19f, 1f) : new Color4(0.7f, 0.7f, 0.72f, 1f), softEdge: 0.6f),
        };

        smoke.Spawners.Add(new SpawnerPerSecond { SpawnCount = v switch { 0 => 10, 1 => 70, _ => 25 } });
        smoke.Initializers.Add(new InitialSizeSeed { RandomSize = new Vector2(0.8f, 1.3f) });
        // The smoke starts where the flame ends
        smoke.Initializers.Add(new InitialPositionSeed { PositionMin = new Vector3(-0.4f, -3.2f, -0.4f), PositionMax = new Vector3(0.4f, -2f, 0.4f) });
        smoke.Initializers.Add(new InitialVelocitySeed { VelocityMin = new Vector3(-1f, -6f, -1f), VelocityMax = new Vector3(1f, -3.5f, 1f) });
        smoke.Initializers.Add(new InitialRotationSeed { AngularRotation = new Vector2(-180f, 180f) });
        smoke.Updaters.Add(new UpdaterSizeOverTime { SamplerMain = Curves.Float((0f, 0.5f), (1f, 2.8f)) });
        smoke.Updaters.Add(new UpdaterColorOverTime { SamplerMain = Curves.Color((0f, new Color4(1f, 1f, 1f, 0f)), (0.15f, new Color4(1f, 1f, 1f, sooty ? 0.85f : 0.55f)), (1f, new Color4(1f, 1f, 1f, 0f))) });

        // The exhaust meets the pad and rolls out along it: a slab collider with no bounce and little friction
        smoke.Updaters.Add(new UpdaterCollider
        {
            FieldShape = new Cube { HalfSideX = 8f, HalfSideY = 0.1f, HalfSideZ = 8f },
            Position = new Vector3(0f, -Height + 0.15f, 0f),
            InheritPosition = true,
            Restitution = 0.02f,
            Friction = 0.03f,
        });

        if (!afterburner)
        {
            s.Place(new Vector3(0f, Height, 0f), 2f, smoke, plume, core, sparks);

            return;
        }

        // Mach diamonds: short-lived bright discs strung along the core's axis, so the column shimmers
        var diamonds = new ParticleEmitter
        {
            ParticleLifetime = new Vector2(0.08f, 0.16f),
            SimulationSpace = EmitterSimulationSpace.World,
            ShapeBuilder = new ShapeBuilderBillboard(),
            Material = ParticleMaterials.Textured(s.Textures.Radial, new Color4(2f, 2.4f, 3f, 1f), additive: 1f),
        };

        diamonds.Spawners.Add(new SpawnerPerSecond { SpawnCount = 90 });
        diamonds.Initializers.Add(new InitialSizeSeed { RandomSize = new Vector2(0.35f, 0.55f) });
        diamonds.Initializers.Add(new InitialPositionSeed { PositionMin = new Vector3(-0.05f, -3.2f, -0.05f), PositionMax = new Vector3(0.05f, -0.4f, 0.05f) });

        // Dust: the landing burn stirs up the pad, a wide low cloud that the smoke's collider keeps on the ground
        var dust = new ParticleEmitter
        {
            ParticleLifetime = new Vector2(3f, 5f),
            SimulationSpace = EmitterSimulationSpace.World,
            SortingPolicy = EmitterSortingPolicy.ByDepth,
            ShapeBuilder = new ShapeBuilderBillboard(),
            Material = ParticleMaterials.Smoke(s.Textures, new Color4(0.62f, 0.56f, 0.48f, 1f), softEdge: 0.8f),
        };

        dust.Spawners.Add(new SpawnerPerSecond { SpawnCount = 40 });
        dust.Initializers.Add(new InitialSizeSeed { RandomSize = new Vector2(1.2f, 1.8f) });
        dust.Initializers.Add(new InitialPositionSeed { PositionMin = new Vector3(-1.5f, -Height + 0.1f, -1.5f), PositionMax = new Vector3(1.5f, -Height + 0.3f, 1.5f) });
        dust.Initializers.Add(new InitialVelocitySeed { VelocityMin = new Vector3(-5f, 0.2f, -5f), VelocityMax = new Vector3(5f, 1.5f, 5f) });
        dust.Initializers.Add(new InitialRotationSeed { AngularRotation = new Vector2(-180f, 180f) });
        dust.Updaters.Add(new UpdaterSizeOverTime { SamplerMain = Curves.Float((0f, 1f), (1f, 4f)) });
        dust.Updaters.Add(new UpdaterColorOverTime { SamplerMain = Curves.Color((0f, new Color4(1f, 1f, 1f, 0f)), (0.2f, new Color4(1f, 1f, 1f, 0.7f)), (1f, new Color4(1f, 1f, 1f, 0f))) });
        dust.Updaters.Add(new UpdaterForceField
        {
            FieldShape = new Cylinder { Radius = 9f, HalfHeight = 2f },
            FieldFalloff = new FieldFalloff { StrengthInside = 1f, FalloffStart = 0f, StrengthOutside = 0f, FalloffEnd = 1f },
            ForceVortex = 0f,
            ForceRepulsive = 2.5f,
            ForceDirected = 0.3f,
            EnergyConservation = 0.5f,
        });

        s.Place(new Vector3(0f, Height, 0f), 2f, dust, smoke, plume, core, sparks, diamonds);
    }
}