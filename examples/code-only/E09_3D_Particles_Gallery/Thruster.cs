using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Particles;
using Stride.Particles.Initializers;
using Stride.Particles.Modules;
using Stride.Particles.ShapeBuilders;
using Stride.Particles.Spawners;
using Stride.Particles.Updaters;
using Stride.Rendering.Colors;
using Stride.Rendering.Lights;

namespace E09_3D_Particles_Gallery;

/// <summary>
/// The layers an exhaust is built from, each one emitter. Every layer fires along the emitter's
/// local X. A layer is described by how fast the gas leaves, how far it reaches and how wide it
/// opens, and the lifetime and the sideways speed follow from those.
/// </summary>
/// <remarks>
/// <para>
/// A colour given to a layer is the colour the plume should have, not the colour of one particle.
/// Additive particles add up: thirty blue particles over each other are white. So a layer works out
/// how many of its particles overlap and gives each one its share.
/// </para>
/// <para>
/// Colours may be brighter than 1, for HDR. A particle's own colour is stored in bytes and stops at
/// 1, so the brightness goes into the material's tint and the particles carry the hue.
/// </para>
/// </remarks>
public static class Exhaust
{
    /// <summary>A glow that sits on the nozzle: a few large, short-lived billboards.</summary>
    /// <param name="textures">The loaded textures.</param>
    /// <param name="size">The size of the glow.</param>
    /// <param name="colour">Its colour. Values above 1 are intensity in HDR.</param>
    /// <param name="at">Where it sits, in the emitter's space.</param>
    public static ParticleEmitter Flare(ParticleTextures textures, float size, Color4 colour, Vector3 at = default)
    {
        var flare = new ParticleEmitter
        {
            ParticleLifetime = new Vector2(0.05f, 0.12f),
            SimulationSpace = EmitterSimulationSpace.World,
            ShapeBuilder = new ShapeBuilderBillboard(),
            Material = ParticleMaterials.Textured(textures.Radial, colour, additive: 1f),
        };

        flare.Spawners.Add(new SpawnerPerSecond { SpawnCount = 40 });
        flare.Initializers.Add(new InitialPositionSeed { PositionMin = at, PositionMax = at });
        flare.Initializers.Add(new InitialSizeSeed { RandomSize = new Vector2(size * 0.85f, size * 1.15f) });

        return flare;
    }

    /// <summary>The hot core: quads stretched along their flight, fast and additive.</summary>
    /// <param name="textures">The loaded textures.</param>
    /// <param name="width">The width of the core at the nozzle.</param>
    /// <param name="speed">How fast the gas leaves.</param>
    /// <param name="reach">How far the core reaches.</param>
    /// <param name="colour">Its colour at the nozzle. Values above 1 are intensity in HDR.</param>
    /// <param name="fade">Its colour where it ends.</param>
    /// <param name="rate">Particles per second.</param>
    public static ParticleEmitter Core(ParticleTextures textures, float width, float speed, float reach, Color4 colour, Color4 fade, int rate = 300)
    {
        var life = reach / speed;
        var peak = Peak(colour, fade);
        var length = speed * CoreLength * width;
        var share = Share(rate, life, length, reach);

        var core = new ParticleEmitter
        {
            ParticleLifetime = new Vector2(life * 0.75f, life),
            SimulationSpace = EmitterSimulationSpace.World,
            ShapeBuilder = new ShapeBuilderOrientedQuad { ScaleLength = true, LengthFactor = CoreLength },
            Material = ParticleMaterials.Textured(textures.Radial, Tint(peak * share), additive: 1f),
        };

        core.Spawners.Add(new SpawnerPerSecond { SpawnCount = rate });
        core.Initializers.Add(new InitialPositionSeed { PositionMin = new Vector3(0f, -width * 0.2f, -width * 0.2f), PositionMax = new Vector3(0f, width * 0.2f, width * 0.2f) });
        core.Initializers.Add(new InitialVelocitySeed { VelocityMin = new Vector3(speed * 0.8f, -speed * 0.03f, -speed * 0.03f), VelocityMax = new Vector3(speed, speed * 0.03f, speed * 0.03f) });
        core.Updaters.Add(new UpdaterSpeedToDirection());
        core.Updaters.Add(new UpdaterSizeOverTime { SamplerMain = Curves.Float((0f, width * 0.8f), (0.5f, width), (1f, width * 0.3f)) });
        core.Updaters.Add(new UpdaterColorOverTime { SamplerMain = Curves.Color((0f, Under(colour, peak)), (0.6f, Under(fade, peak)), (1f, Under(fade, peak, alpha: 0f))) });

        return core;
    }

    /// <summary>The body of the plume: billboards of a flipbook that widen as they fly and change colour along the way.</summary>
    /// <param name="sheet">An 8 by 8 flipbook.</param>
    /// <param name="width">The width at the nozzle.</param>
    /// <param name="flare">How many times wider the plume is where it ends.</param>
    /// <param name="speed">How fast the gas leaves.</param>
    /// <param name="reach">How far the plume reaches.</param>
    /// <param name="halfAngle">Half the angle the plume opens at, in degrees.</param>
    /// <param name="colours">The colour at the nozzle, along the body and at the tail. Values above 1 are intensity in HDR.</param>
    /// <param name="additive">0 blends, 1 adds light.</param>
    /// <param name="rate">Particles per second.</param>
    public static ParticleEmitter Body(Texture sheet, float width, float flare, float speed, float reach, float halfAngle, (Color4 Nozzle, Color4 Middle, Color4 Tail) colours, float additive = 1f, int rate = 140)
    {
        var life = reach / speed;
        var sideways = speed * MathF.Tan(MathUtil.DegreesToRadians(halfAngle));
        var peak = Peak(colours.Nozzle, colours.Middle, colours.Tail);
        var share = Share(rate, life, width * (1f + flare) / 2f, reach);

        var body = new ParticleEmitter
        {
            ParticleLifetime = new Vector2(life * 0.7f, life),
            SimulationSpace = EmitterSimulationSpace.World,
            SortingPolicy = additive < 1f ? EmitterSortingPolicy.ByDepth : EmitterSortingPolicy.None,
            ShapeBuilder = new ShapeBuilderBillboard(),
            Material = ParticleMaterials.Flipbook(sheet, 8, 8, 64, Tint(peak * share), additive),
        };

        body.Spawners.Add(new SpawnerPerSecond { SpawnCount = rate });
        body.Initializers.Add(new InitialPositionSeed { PositionMin = new Vector3(0f, -width * 0.25f, -width * 0.25f), PositionMax = new Vector3(width * 0.5f, width * 0.25f, width * 0.25f) });
        body.Initializers.Add(new InitialVelocitySeed { VelocityMin = new Vector3(speed * 0.75f, -sideways, -sideways), VelocityMax = new Vector3(speed, sideways, sideways) });
        body.Initializers.Add(new InitialRotationSeed { AngularRotation = new Vector2(-180f, 180f) });
        body.Updaters.Add(new UpdaterSizeOverTime { SamplerMain = Curves.Float((0f, width), (1f, width * flare)) });
        body.Updaters.Add(new UpdaterColorOverTime
        {
            SamplerMain = Curves.Color(
                (0f, Under(colours.Nozzle, peak)),
                (0.35f, Under(colours.Middle, peak)),
                (0.8f, Under(colours.Tail, peak)),
                (1f, Under(colours.Tail, peak, alpha: 0f))),
        });

        return body;
    }

    /// <summary>Shock diamonds: cells that stand still along the axis. See <see cref="ShockCellInitializer"/>.</summary>
    /// <param name="textures">The loaded textures.</param>
    /// <param name="cells">Where the cells are and how large.</param>
    /// <param name="colour">The colour of the first cell. Values above 1 are intensity in HDR.</param>
    /// <param name="rate">Particles per second, shared by all the cells.</param>
    public static ParticleEmitter Diamonds(ParticleTextures textures, ShockCellInitializer cells, Color4 colour, int rate = 160)
    {
        var peak = Peak(colour);

        cells.Color = Under(colour, peak);

        var diamonds = new ParticleEmitter
        {
            ParticleLifetime = new Vector2(0.06f, 0.14f),
            SimulationSpace = EmitterSimulationSpace.World,
            ShapeBuilder = new ShapeBuilderOrientedQuad { ScaleLength = true, LengthFactor = 1.5f },
            Material = ParticleMaterials.Textured(textures.Cell, Tint(peak), additive: 1f),
        };

        diamonds.Spawners.Add(new SpawnerPerSecond { SpawnCount = rate });
        diamonds.Initializers.Add(cells);

        return diamonds;
    }

    /// <summary>Smoke: alpha-blended puffs that leave slowly, grow and fade.</summary>
    /// <param name="textures">The loaded textures.</param>
    /// <param name="width">The width of a puff at birth.</param>
    /// <param name="flare">How many times wider a puff is when it dies.</param>
    /// <param name="velocity">The slowest and the fastest a puff leaves, in the emitter's space.</param>
    /// <param name="life">The shortest and the longest a puff lives, in seconds.</param>
    /// <param name="colours">The colour of a young puff and of an old one.</param>
    /// <param name="opacity">The alpha of a puff at its thickest.</param>
    /// <param name="brightness">How bright the smoke is. Particles are not lit, so smoke in daylight needs more than 1 to look white.</param>
    /// <param name="rate">Particles per second.</param>
    /// <param name="at">Where the puffs are born, in the emitter's space.</param>
    public static ParticleEmitter Smoke(ParticleTextures textures, float width, float flare, (Vector3 Min, Vector3 Max) velocity, Vector2 life, (Color4 Young, Color4 Old) colours, float opacity = 0.6f, float brightness = 1f, int rate = 40, Vector3 at = default)
    {
        var smoke = new ParticleEmitter
        {
            ParticleLifetime = life,
            SimulationSpace = EmitterSimulationSpace.World,
            SortingPolicy = EmitterSortingPolicy.ByDepth,
            ShapeBuilder = new ShapeBuilderBillboard(),
            Material = ParticleMaterials.Flipbook(textures.Billow, 8, 8, 64, Tint(brightness), softEdge: 0.5f),
        };

        smoke.Spawners.Add(new SpawnerPerSecond { SpawnCount = rate });
        smoke.Initializers.Add(new InitialPositionSeed { PositionMin = at, PositionMax = at });
        smoke.Initializers.Add(new InitialVelocitySeed { VelocityMin = velocity.Min, VelocityMax = velocity.Max });
        smoke.Initializers.Add(new InitialRotationSeed { AngularRotation = new Vector2(-180f, 180f) });
        smoke.Updaters.Add(new UpdaterSizeOverTime { SamplerMain = Curves.Float((0f, width), (1f, width * flare)) });
        smoke.Updaters.Add(new UpdaterColorOverTime
        {
            SamplerMain = Curves.Color(
                (0f, new Color4(colours.Young.R, colours.Young.G, colours.Young.B, 0f)),
                (0.12f, new Color4(colours.Young.R, colours.Young.G, colours.Young.B, opacity)),
                (1f, new Color4(colours.Old.R, colours.Old.G, colours.Old.B, 0f))),
        });

        return smoke;
    }

    // A core quad is this many times longer than wide for every unit of speed
    private const float CoreLength = 0.22f;

    // A particle's texture is soft: on average it covers about a third of its quad
    private const float Coverage = 0.03f;

    /// <summary>
    /// The share of the plume's colour one particle carries: one over the number of particles that
    /// lie over each other along the plume.
    /// </summary>
    /// <param name="rate">Particles per second.</param>
    /// <param name="life">How long a particle lives, in seconds.</param>
    /// <param name="size">The size of a particle along the plume.</param>
    /// <param name="reach">How far the plume reaches.</param>
    private static float Share(float rate, float life, float size, float reach)
        => 1f / MathF.Max(1f, rate * life * size / reach * Coverage);

    /// <summary>The brightest channel of the colours, and never less than 1: what goes into a material's tint.</summary>
    private static float Peak(params Color4[] colours) => colours.Max(colour => MathF.Max(1f, MathF.Max(colour.R, MathF.Max(colour.G, colour.B))));

    /// <summary>A colour divided by the peak, so every channel fits a particle's bytes.</summary>
    private static Color4 Under(Color4 colour, float peak, float? alpha = null) => new(colour.R / peak, colour.G / peak, colour.B / peak, alpha ?? colour.A);

    /// <summary>White at an intensity.</summary>
    private static Color4 Tint(float intensity) => new(intensity, intensity, intensity, 1f);

    /// <summary>A point light in the plume's colour, as a child of the particle entity, so it goes when the particles go.</summary>
    /// <param name="parent">The entity carrying the particle system.</param>
    /// <param name="local">Where the light sits, in the emitter's space.</param>
    /// <param name="colour">The light's colour.</param>
    /// <param name="intensity">Its intensity at full throttle.</param>
    public static LightComponent Light(Entity parent, Vector3 local, Color colour, float intensity)
    {
        var light = new LightComponent
        {
            Intensity = intensity,
            Type = new LightPoint { Radius = 9f, Color = new ColorRgbProvider(colour) },
        };

        parent.AddChild(new Entity("Exhaust light") { Transform = { Position = local }, Components = { light } });

        return light;
    }
}

/// <summary>
/// What a throttle moves. An engine registers its emitters once, and <see cref="Apply"/> then
/// scales every spawn rate and every exhaust speed together, pulls the shock diamonds closer and
/// dims the light.
/// </summary>
public sealed class Throttle
{
    private readonly List<(SpawnerPerSecond Spawner, float Rate)> _spawners = [];
    private readonly List<(InitialVelocitySeed Velocity, Vector3 Min, Vector3 Max)> _velocities = [];
    private readonly List<(ShockCellInitializer Cells, float First, float Spacing)> _cells = [];
    private readonly List<(LightComponent Light, float Intensity)> _lights = [];

    private float _applied = 1f;

    /// <summary>The throttle last applied, from 0 to 1.</summary>
    public float Value => _applied;

    /// <summary>Registers the emitters the throttle drives. Emitters left out keep running as they are.</summary>
    public Throttle Drive(params ParticleEmitter[] emitters)
    {
        foreach (var emitter in emitters)
        {
            var rate = 0f;

            foreach (var spawner in emitter.Spawners.OfType<SpawnerPerSecond>())
            {
                _spawners.Add((spawner, spawner.SpawnCount));

                rate += spawner.SpawnCount;
            }

            // A pool sized once for full throttle: without it every change of rate resizes the pool
            emitter.MaxParticlesOverride = (int)MathF.Ceiling(rate * emitter.ParticleLifetime.Y) + 1;

            foreach (var initializer in emitter.Initializers)
            {
                if (initializer is InitialVelocitySeed velocity) _velocities.Add((velocity, velocity.VelocityMin, velocity.VelocityMax));
                if (initializer is ShockCellInitializer cells) _cells.Add((cells, cells.First, cells.Spacing));
            }
        }

        return this;
    }

    /// <summary>Registers a light the throttle dims.</summary>
    public Throttle Drive(LightComponent light)
    {
        _lights.Add((light, light.Intensity));

        return this;
    }

    /// <summary>Sets the throttle.</summary>
    /// <param name="throttle">From 0, the engine off, to 1, full thrust.</param>
    /// <param name="flicker">A factor on the light alone, for the flame's flicker.</param>
    public void Apply(float throttle, float flicker = 1f)
    {
        throttle = MathUtil.Clamp(throttle, 0f, 1f);

        foreach (var (light, intensity) in _lights)
        {
            light.Intensity = intensity * throttle * flicker;
        }

        // Emitters are touched only when the throttle has moved: a new rate makes the emitter rebuild its sorter
        if (MathF.Abs(throttle - _applied) < 0.01f) return;

        _applied = throttle;

        // A throttled plume is thinner and shorter, not just thinner
        var rate = throttle <= 0f ? 0f : 0.35f + 0.65f * throttle;
        var speed = 0.5f + 0.5f * throttle;
        var spacing = 0.4f + 0.6f * throttle;

        foreach (var (spawner, full) in _spawners)
        {
            spawner.SpawnCount = full * rate;
        }

        foreach (var (velocity, min, max) in _velocities)
        {
            velocity.VelocityMin = min * speed;
            velocity.VelocityMax = max * speed;
        }

        foreach (var (cells, first, full) in _cells)
        {
            cells.First = first * spacing;
            cells.Spacing = full * spacing;
        }
    }
}