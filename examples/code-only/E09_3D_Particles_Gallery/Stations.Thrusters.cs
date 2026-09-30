using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Particles;
using Stride.Particles.Initializers;
using Stride.Particles.Modules;
using Stride.Particles.ShapeBuilders;
using Stride.Particles.Spawners;
using Stride.Particles.Updaters;
using Stride.Particles.Updaters.FieldShapes;

namespace E09_3D_Particles_Gallery;

/// <summary>
/// Thrusters: six engines built from the layers in <see cref="Exhaust"/>, with no shader of their
/// own. What tells one fuel from another is colour, opacity, how wide the plume opens and whether
/// it has shock diamonds. Sizes are in nozzle exit diameters, so an engine scales as a whole.
/// </summary>
public static class ThrusterStations
{
    // Where the nozzle of an engine on the test stand ends: the exhaust goes from here to the right
    private static readonly Vector3 StandNozzle = new(-4.2f, 2.4f, 0f);

    // Lying along X with its top towards the stand: what a cone or a cylinder on the test stand needs
    private static readonly Quaternion Lying = Quaternion.RotationZ(MathUtil.PiOverTwo);

    // Firing straight down: the exhaust layers fire along X, so X is turned to point at the ground
    private static readonly Quaternion Down = Quaternion.RotationZ(-MathUtil.PiOverTwo);

    /// <summary>
    /// A kerosene engine, like Falcon 9's Merlin: a pale yellow core, an opaque orange body and a
    /// deep red tail, from glowing soot. The dark streak beside it is the exhaust of the gas generator
    /// that drives the pumps. At altitude the same engine makes a wide, faint bell.
    /// </summary>
    public static void Kerosene(ParticleStation s)
    {
        const float D = 0.7f;

        var v = s.Pick("sea level: narrow and bright", "high altitude: a wide faint bell", "ignition: a green flash, then thrust");

        var engine = Engine.Of(s, KeroseneStand);
        var altitude = v == 1;

        var flare = Exhaust.Flare(s.Textures, D * 1.7f, Hue(255, 200, 120, altitude ? 0.4f : 0.7f));
        var core = Exhaust.Core(s.Textures, D * 0.75f, 24f, altitude ? 2.2f : 3.4f, Hue(255, 215, 150, 1.8f), Hue(255, 140, 60, 1.4f), rate: 360);

        var body = altitude
            ? Exhaust.Body(s.Textures.Billow, new(D * 1.2f, 5f, 12f, 5f, 30f), (Hue(255, 160, 90, 1.3f), Hue(150, 50, 45, 1f), Hue(60, 55, 150, 0.7f)), rate: 320)
            : Exhaust.Body(s.Textures.Flame, new(D * 1.1f, 2.2f, 16f, 6.5f, 3.5f), (Hue(255, 190, 110, 1.3f), Hue(255, 120, 50, 1.1f), Hue(220, 45, 15, 0.8f)), rate: 320);

        var tail = altitude
            ? Exhaust.Body(s.Textures.Billow, new(D * 2f, 4f, 9f, 6f, 38f), (Hue(140, 58, 54, 0f), Hue(110, 50, 100, 0.6f), Hue(60, 55, 150, 0.4f)), rate: 200)
            : Exhaust.Body(s.Textures.Billow, new(D * 1.5f, 2.2f, 12f, 8f, 5f), (Hue(255, 150, 100, 0f), Hue(240, 80, 30, 0.7f), Hue(170, 30, 10, 0.4f)), rate: 220);

        // The gas generator burns rich, so its exhaust is soot: alpha-blended, and it starts beside the nozzle
        var soot = Exhaust.Smoke(s.Textures, new(D * 0.35f, 4f, Hue(150, 70, 10), Hue(30, 11, 11), Opacity: 0.85f), (new Vector3(6f, -0.7f, 0.2f), new Vector3(9f, -0.1f, 0.9f)), new Vector2(0.5f, 0.8f), rate: 80, at: new Vector3(0f, -0.05f, 0.6f));

        // The igniter fluid burns green for a moment before the kerosene lights
        var flash = Exhaust.Body(s.Textures.Billow, new(D * 1.2f, 2.5f, 7f, 2.2f, 14f), (Hue(90, 255, 90, 1.3f), Hue(40, 255, 70, 1f), Hue(20, 200, 40, 0.5f)), rate: 0);
        var flashSpawner = flash.Spawners.OfType<SpawnerPerSecond>().First();

        flash.MaxParticlesOverride = 200;

        var particles = s.Place(StandNozzle, 1f, soot, tail, body, core, flare, flash);

        engine.Throttle = new Throttle()
            .Drive(soot, tail, body, core, flare)
            .Drive(Exhaust.Light(particles.Entity, new Vector3(1.6f, 0.4f, 0f), new Color(255, 170, 110), 28f));

        if (v != 2)
        {
            engine.Schedule = seconds => 0.72f + 0.28f * MathF.Sin(seconds * 0.7f);
            engine.Tick = null;

            return;
        }

        // Seven seconds, over and over: the flash, the run-up, the burn, the shutdown, a pause
        engine.Schedule = seconds => Ramp(seconds % 7f, (0.4f, 0f), (1.3f, 1f), (5.4f, 1f), (5.9f, 0f));
        engine.Tick = station =>
        {
            var rate = station.Seconds % 7f < 0.45f ? 140f : 0f;

            if (flashSpawner.SpawnCount != rate) flashSpawner.SpawnCount = rate;
        };
    }

    /// <summary>
    /// Nine kerosene engines under one rocket, like a Falcon 9 at liftoff: nine cores that merge
    /// into one plume, which meets the pad and spreads. One emitter feeds all nine nozzles through
    /// <see cref="ClusterInitializer"/>.
    /// </summary>
    public static void Cluster(ParticleStation s)
    {
        const float D = 0.42f;
        const float NozzleHeight = 3.8f;

        var v = s.Pick("liftoff: nine engines", "throttled down", "three engines: a boostback burn");

        var engine = Engine.Of(s, (station, model) => ClusterRocket(station, model, D, NozzleHeight));
        var three = v == 2;

        // Eight in a ring and one in the middle; the emitter's X points down, so the ring lies in Y and Z
        var nozzles = ClusterNozzles(0.78f);

        if (three) nozzles = [nozzles[0], nozzles[1], nozzles[5]];

        var spread = three ? 0.5f : 1f;

        var flare = Exhaust.Flare(s.Textures, three ? 1.6f : 2.6f, Hue(255, 200, 120, 0.6f), new Vector3(0.1f, 0f, 0f));
        var core = Exhaust.Core(s.Textures, D * 0.8f, 22f, 2.4f, Hue(255, 215, 150, 1.6f), Hue(255, 140, 60, 1.2f), rate: three ? 300 : 700);

        core.Initializers.Add(new ClusterInitializer { Nozzles = nozzles, Radius = D * 0.3f });

        var body = Exhaust.Body(s.Textures.Flame, new(1.6f * spread, 2f, 13f, 4.4f, 5f), (Hue(255, 190, 110, 1.2f), Hue(255, 120, 50, 1f), Hue(220, 45, 15, 0.7f)), rate: three ? 140 : 280);
        var tail = Exhaust.Body(s.Textures.Billow, new(2f * spread, 2.2f, 10f, 5f, 8f), (Hue(255, 150, 100, 0f), Hue(240, 80, 30, 0.6f), Hue(170, 30, 10, 0.35f)), rate: three ? 80 : 150);

        // The smoke and steam of a launch: it comes down with the exhaust and rolls out over the pad
        var cloud = Exhaust.Smoke(s.Textures, new(1.2f, 2.6f, Hue(245, 238, 230), Hue(190, 190, 195), Opacity: 0.6f, Brightness: 3.5f), (new Vector3(6f, -1.6f, -1.6f), new Vector3(9f, 1.6f, 1.6f)), new Vector2(1.8f, 2.8f), rate: three ? 35 : 70, at: new Vector3(1.5f, 0f, 0f));

        foreach (var emitter in (ParticleEmitter[])[body, tail, cloud])
        {
            emitter.Updaters.Add(Pad(NozzleHeight));
        }

        var particles = s.Place(new Vector3(0f, NozzleHeight, 0f), 2f, cloud, tail, body, core, flare);

        particles.Entity.Transform.Rotation = Down * s.FacingRotation();

        engine.Throttle = new Throttle()
            .Drive(cloud, tail, body, core, flare)
            .Drive(Exhaust.Light(particles.Entity, new Vector3(2.5f, 0f, 1.5f), new Color(255, 170, 110), 45f));

        engine.Schedule = v == 1
            ? seconds => 0.45f + 0.05f * MathF.Sin(seconds * 2f)
            : seconds => 0.92f + 0.08f * MathF.Sin(seconds * 1.3f);
        engine.Tick = null;
    }

    /// <summary>
    /// A methane engine, like Starship's Raptor: a clean, translucent blue-violet flame inside a
    /// faint orange sheath, with shock diamonds that stand still. Methane makes little soot, so the
    /// flame is mostly see-through. The third variation burns hydrogen, like the Shuttle's RS-25:
    /// the flame is nearly invisible and the diamonds are the brightest thing in it.
    /// </summary>
    public static void Methane(ParticleStation s)
    {
        const float D = 0.75f;

        var v = s.Pick("full throttle", "low throttle: the diamonds move in", "hydrogen: almost invisible");

        var engine = Engine.Of(s, MethaneStand);
        var hydrogen = v == 2;

        var cells = new ShockCellInitializer
        {
            Cells = 5,
            First = D * 0.9f,
            Spacing = D * 1.35f,
            Size = D * 0.95f,
        };

        var flare = Exhaust.Flare(s.Textures, D * 1.4f, hydrogen ? Hue(100, 130, 255, 0.3f) : Hue(120, 160, 255, 0.9f));
        var diamonds = Exhaust.Diamonds(s.Textures, cells, hydrogen ? Hue(90, 120, 255, 1.6f) : Hue(160, 140, 255, 1.8f));

        var core = hydrogen
            ? Exhaust.Core(s.Textures, D * 0.6f, 28f, 3f, Hue(100, 130, 255, 0.3f), Hue(62, 84, 255, 0.2f), rate: 200)
            : Exhaust.Core(s.Textures, D * 0.65f, 26f, 3.6f, Hue(150, 180, 255, 2.2f), Hue(80, 100, 240, 1.5f));

        var body = hydrogen
            ? Exhaust.Body(s.Textures.Billow, new(D, 1.8f, 17f, 6f, 3f), (Hue(100, 130, 255, 0.3f), Hue(90, 120, 255, 0.22f), Hue(255, 139, 117, 0.08f)))
            : Exhaust.Body(s.Textures.Billow, new(D, 1.9f, 16f, 6.5f, 3f), (Hue(90, 130, 255, 1.1f), Hue(70, 100, 245, 0.9f), Hue(130, 40, 180, 0.5f)), rate: 200);

        // Fuel-rich gas along the wall burns with the air outside: a thin orange skin round the blue
        var sheath = Exhaust.Body(s.Textures.Billow, new(D * 1.3f, 2.2f, 11f, 5.5f, 5f), (Hue(241, 99, 23, 0f), Hue(241, 99, 23, hydrogen ? 0.06f : 0.4f), Hue(228, 100, 40, hydrogen ? 0.03f : 0.25f)), rate: 80);

        var particles = s.Place(StandNozzle, 1f, sheath, body, core, diamonds, flare);

        engine.Throttle = new Throttle()
            .Drive(sheath, body, core, diamonds, flare)
            .Drive(Exhaust.Light(particles.Entity, new Vector3(1.6f, 0.4f, 0f), new Color(140, 165, 255), hydrogen ? 8f : 22f));

        engine.Schedule = v == 1
            ? seconds => 0.42f + 0.04f * MathF.Sin(seconds * 2f)
            : seconds => 0.95f + 0.05f * MathF.Sin(seconds * 1.7f);
        engine.Tick = null;
    }

    /// <summary>
    /// A jet engine. Dry, its exhaust is hot air with a little soot and nearly nothing to see. With
    /// the afterburner on, fuel is sprayed into that exhaust and burns behind the engine: an orange
    /// flame with a blue root and the best-known shock diamonds there are.
    /// </summary>
    public static void Afterburner(ParticleStation s)
    {
        const float D = 0.75f;

        var v = s.Pick("afterburner on", "dry: hot air and a little soot", "lighting up, over and over");

        var engine = Engine.Of(s, JetStand);

        // Dry exhaust is always there, under the flame too
        var haze = Exhaust.Smoke(s.Textures, new(D * 0.8f, 3f, Hue(90, 85, 80), Hue(60, 60, 65), Opacity: 0.2f, Brightness: 1.5f), (new Vector3(10f, -0.3f, -0.3f), new Vector3(14f, 0.3f, 0.3f)), new Vector2(0.5f, 0.8f), rate: 60);

        var cells = new ShockCellInitializer
        {
            Cells = 7,
            First = D * 0.6f,
            Spacing = D * 0.95f,
            Size = D * 0.85f,
            Shrink = 0.9f,
            Dimming = 0.8f,
        };

        var flare = Exhaust.Flare(s.Textures, D * 1.3f, Hue(130, 150, 255, 0.9f));
        var diamonds = Exhaust.Diamonds(s.Textures, cells, Hue(255, 200, 120, 1.8f), rate: 220);
        var core = Exhaust.Core(s.Textures, D * 0.7f, 26f, 4.2f, Hue(140, 160, 255, 2f), Hue(255, 150, 70, 1.6f));
        var body = Exhaust.Body(s.Textures.Flame, new(D * 0.9f, 1.7f, 17f, 6.5f, 2.5f), (Hue(90, 110, 255, 1.2f), Hue(255, 130, 50, 1.2f), Hue(220, 60, 20, 0.6f)), rate: 200);

        var particles = s.Place(StandNozzle, 1f, haze, body, core, diamonds, flare);

        // The haze is left out: the throttle here is the afterburner's, and the engine runs either way
        engine.Throttle = new Throttle()
            .Drive(body, core, diamonds, flare)
            .Drive(Exhaust.Light(particles.Entity, new Vector3(1.6f, 0.4f, 0f), new Color(255, 180, 120), 24f));

        engine.Schedule = v switch
        {
            0 => seconds => 0.95f + 0.05f * MathF.Sin(seconds * 1.7f),
            1 => _ => 0f,
            _ => seconds => Ramp(seconds % 6f, (1f, 0f), (1.4f, 1f), (4.5f, 1f), (4.8f, 0f)),
        };
        engine.Tick = null;
    }

    /// <summary>
    /// A solid booster in flight: a near-white flame, brighter than any liquid engine, and a thick
    /// white trail of aluminium oxide that stays in the sky and widens. The trail is spawned by the
    /// distance the booster travels, in world space, so it is as dense at any speed.
    /// </summary>
    public static void SolidBooster(ParticleStation s)
    {
        const float D = 0.5f;

        var v = s.Pick("rising", "flying a loop", "rising through wind shear");

        var engine = Engine.Of(s, BoosterRocket);
        var rocket = engine.Model[0];

        var flare = Exhaust.Flare(s.Textures, D * 2.2f, Hue(255, 245, 215, 1f));
        var core = Exhaust.Core(s.Textures, D * 0.9f, 16f, 1.6f, Hue(255, 245, 215, 2.4f), Hue(255, 215, 130, 1.7f), rate: 220);
        var body = Exhaust.Body(s.Textures.Flame, new(D * 1.2f, 2f, 9f, 2.4f, 7f), (Hue(255, 240, 200, 1.9f), Hue(255, 210, 120, 1.5f), Hue(255, 150, 70, 0.7f)), rate: 140);

        // Slag: burning drops of aluminium thrown out of the flame, falling
        var slag = new ParticleEmitter
        {
            ParticleLifetime = new Vector2(0.5f, 1.1f),
            SimulationSpace = EmitterSimulationSpace.World,
            ShapeBuilder = new ShapeBuilderOrientedQuad { ScaleLength = true, LengthFactor = 0.35f },
            Material = ParticleMaterials.Textured(s.Textures.Dot, Hue(255, 210, 130, 1.8f), additive: 1f),
        };

        slag.Spawners.Add(new SpawnerPerSecond { SpawnCount = 70 });
        slag.Initializers.Add(Initializers.AtEmitter());
        slag.Initializers.Add(new InitialSizeSeed { RandomSize = new Vector2(0.04f, 0.08f) });
        slag.Initializers.Add(new InitialVelocitySeed { VelocityMin = new Vector3(5f, -2.5f, -2.5f), VelocityMax = new Vector3(10f, 2.5f, 2.5f) });
        slag.Updaters.Add(new UpdaterGravity { GravitationalAcceleration = new Vector3(0f, -9.8f, 0f) });
        slag.Updaters.Add(new UpdaterSpeedToDirection());

        // The trail: puffs left where the booster has been, that stay, grow and thin out
        var trail = new ParticleEmitter
        {
            ParticleLifetime = new Vector2(5f, 7f),
            SimulationSpace = EmitterSimulationSpace.World,
            SortingPolicy = EmitterSortingPolicy.ByDepth,
            ShapeBuilder = new ShapeBuilderBillboard(),
            // Particles are not lit: white smoke in daylight is brighter than 1
            Material = ParticleMaterials.Flipbook(s.Textures.Billow, 8, 8, 64, new Color4(4f, 4f, 4f, 1f), softEdge: 0.5f),
            MaxParticlesOverride = 900,
        };

        var trailSpawner = new SpawnerFromDistance { SpawnCount = TrailDensity };

        trail.Spawners.Add(trailSpawner);
        trail.Initializers.Add(new InitialPositionSeed { PositionMin = new Vector3(0.6f, -0.15f, -0.15f), PositionMax = new Vector3(1f, 0.15f, 0.15f) });
        trail.Initializers.Add(new InitialVelocitySeed { VelocityMin = new Vector3(0.4f, -0.25f, -0.25f), VelocityMax = new Vector3(1.4f, 0.25f, 0.25f) });
        trail.Initializers.Add(new InitialRotationSeed { AngularRotation = new Vector2(-180f, 180f) });
        trail.Updaters.Add(new UpdaterSizeOverTime { SamplerMain = Curves.Float((0f, D * 1.4f), (0.15f, D * 3f), (1f, D * 7f)) });
        trail.Updaters.Add(new UpdaterColorOverTime
        {
            SamplerMain = Curves.Color(
                (0f, new Color4(1f, 0.85f, 0.6f, 0f)),
                (0.04f, new Color4(1f, 0.95f, 0.85f, 0.85f)),
                (0.3f, new Color4(0.95f, 0.95f, 0.95f, 0.7f)),
                (1f, new Color4(0.8f, 0.8f, 0.82f, 0f))),
        });

        if (v == 2)
        {
            // Wind blows one way low down and the other way higher up, and the column bends between
            trail.Updaters.Add(Wind(height: 3f, force: 1.6f));
            trail.Updaters.Add(Wind(height: 7f, force: -1.6f));
        }

        var particles = s.Place(new Vector3(0f, 1f, 0f), trail, body, core, slag, flare);

        engine.Throttle = new Throttle()
            .Drive(body, core, slag, flare)
            .Drive(Exhaust.Light(particles.Entity, new Vector3(1f, 0f, 0f), new Color(255, 235, 200), 30f));

        var flight = v == 1 ? (Func<float, Flight>)Loop : Rise;

        engine.Schedule = seconds => flight(seconds).Burning ? 1f : 0f;
        engine.Tick = station =>
        {
            if (station.Entity is null) return;

            var now = flight(station.Seconds);

            // A booster that starts over jumps back to the pad. The trail is spawned by distance, so
            // it would draw a line of smoke along the jump: no trail in the frame of a jump.
            var density = now.Burning && !now.Jumped ? TrailDensity : 0f;

            if (trailSpawner.SpawnCount != density) trailSpawner.SpawnCount = density;

            var direction = station.Direction(now.Direction.X, now.Direction.Y, 0f);
            var position = station.At(now.Position.X, now.Position.Y, 0f);

            rocket.Transform.Position = position;
            rocket.Transform.Rotation = Quaternion.BetweenDirections(Vector3.UnitY, direction);

            // The exhaust leaves from the tail, against the direction of flight
            station.Entity.Transform.Position = position - direction * 1.1f;
            station.Entity.Transform.Rotation = Quaternion.BetweenDirections(Vector3.UnitX, -direction);
        };
    }

    /// <summary>
    /// The small thrusters that turn a spacecraft. Cold gas is nitrogen let out of a tank: white
    /// puffs, no flame. A hypergolic thruster burns two liquids that ignite on contact: a faint,
    /// wide, brief cone. An ion engine throws xenon ions: a steady, thin blue beam.
    /// </summary>
    public static void SmallThrusters(ParticleStation s)
    {
        const float D = 0.3f;

        var v = s.Pick("cold gas: puffs of nitrogen", "hypergolic: a faint brief cone", "ion: a steady blue beam");

        var engine = Engine.Of(s, Spacecraft);

        ParticleEmitter[] emitters = v switch
        {
            0 =>
            [
                Exhaust.Smoke(s.Textures, new(D * 0.8f, 5f, Hue(255, 255, 255), Hue(225, 235, 245), Opacity: 0.6f, Brightness: 4f), (new Vector3(9f, -1.6f, -1.6f), new Vector3(14f, 1.6f, 1.6f)), new Vector2(0.25f, 0.45f), rate: 220),
            ],
            1 =>
            [
                Exhaust.Body(s.Textures.Billow, new(D, 5f, 14f, 3.4f, 16f), (Hue(120, 200, 245, 1f), Hue(190, 190, 170, 0.5f), Hue(241, 170, 100, 0.25f)), rate: 150),
                Exhaust.Core(s.Textures, D * 0.6f, 22f, 1.2f, Hue(190, 225, 255, 1.8f), Hue(120, 200, 245, 0.9f), rate: 120),
                Exhaust.Flare(s.Textures, D * 1.6f, Hue(190, 225, 255, 0.4f)),
            ],
            _ =>
            [
                Exhaust.Body(s.Textures.Billow, new(D * 0.9f, 1.5f, 9f, 7f, 0.6f), (Hue(0, 150, 255, 1.2f), Hue(0, 150, 255, 0.9f), Hue(80, 180, 255, 0.25f)), rate: 160),
                Exhaust.Core(s.Textures, D * 0.5f, 18f, 5.5f, Hue(170, 225, 255, 2.4f), Hue(0, 150, 255, 1.6f), rate: 220),
                Exhaust.Flare(s.Textures, D * 2f, Hue(80, 180, 255, 0.6f)),
            ],
        };

        var particles = s.Place(new Vector3(-2.6f, 2.6f, 0f), emitters);

        engine.Throttle = new Throttle()
            .Drive(emitters)
            .Drive(Exhaust.Light(particles.Entity, new Vector3(1f, 0.3f, 0f), v == 0 ? Color.White : new Color(120, 190, 255), v == 0 ? 0f : 10f));

        // A thruster that turns a spacecraft fires in short pulses; an ion engine never stops
        engine.Schedule = v switch
        {
            0 => seconds => seconds % 1.2f < 0.18f ? 1f : 0f,
            1 => seconds => seconds % 1.6f < 0.3f ? 1f : 0f,
            _ => _ => 1f,
        };
        engine.Tick = null;
    }

    /// <summary>Runs a thruster station: the throttle follows its schedule, and the light flickers with the flame.</summary>
    public static void Run(ParticleStation s)
    {
        if (s.State is not Engine engine) return;

        var flicker = 0.85f + 0.15f * MathF.Sin(s.Seconds * 37f) * MathF.Sin(s.Seconds * 23f);

        engine.Throttle.Apply(engine.Schedule(s.Seconds), flicker);
        engine.Tick?.Invoke(s);
    }

    // --- Flight paths -------------------------------------------------------------------------

    private const float TrailDensity = 12f;

    /// <summary>Where a booster is at a moment: its position and direction in the station's X and Y.</summary>
    /// <param name="Position">Where it is.</param>
    /// <param name="Direction">Where it points, unit length.</param>
    /// <param name="Burning">Whether the engine is on.</param>
    /// <param name="Jumped">Whether it has just started over, somewhere else.</param>
    private readonly record struct Flight(Vector2 Position, Vector2 Direction, bool Burning, bool Jumped);

    /// <summary>Up from the pad on a gentle arc, faster and faster, then back on the pad for the next launch.</summary>
    private static Flight Rise(float seconds)
    {
        const float Cycle = 6f;
        const float Burn = 3.6f;

        var time = seconds % Cycle;

        if (time > Burn) return new Flight(new Vector2(-2.5f, 1.6f), Vector2.UnitY, Burning: false, Jumped: true);

        var position = new Vector2(-2.5f + 0.3f * time * time, 1.6f + 0.38f * time * time);
        var direction = Vector2.Normalize(new Vector2(0.6f * time, 0.76f * time + 0.01f));

        // The first moment after the pause is a jump too: the booster was not here a frame ago
        return new Flight(position, direction, Burning: true, Jumped: time < 0.05f);
    }

    /// <summary>Round a circle that stands upright, nose first.</summary>
    private static Flight Loop(float seconds)
    {
        var angle = seconds * 1.5f;
        var (sin, cos) = MathF.SinCos(angle);

        return new Flight(new Vector2(cos * 3.2f, 4.6f + sin * 3.2f), new Vector2(-sin, cos), Burning: true, Jumped: false);
    }

    /// <summary>A throttle that follows straight lines between the given moments.</summary>
    private static float Ramp(float time, params (float Time, float Value)[] keys)
    {
        if (time <= keys[0].Time) return keys[0].Value;

        for (var i = 1; i < keys.Length; i++)
        {
            if (time > keys[i].Time) continue;

            var along = (time - keys[i - 1].Time) / (keys[i].Time - keys[i - 1].Time);

            return MathUtil.Lerp(keys[i - 1].Value, keys[i].Value, along);
        }

        return keys[^1].Value;
    }

    // --- Pieces the stations share --------------------------------------------------------------

    /// <summary>A colour from its bytes, with an intensity for HDR and an alpha.</summary>
    private static Color4 Hue(int red, int green, int blue, float intensity = 1f, float alpha = 1f)
        => new(red / 255f * intensity, green / 255f * intensity, blue / 255f * intensity, alpha);

    /// <summary>The pad under an engine that fires down: a slab the exhaust meets and spreads over.</summary>
    /// <param name="below">How far below the nozzle the pad is.</param>
    private static UpdaterCollider Pad(float below) => new()
    {
        // The emitter's X points down, so the slab is thin along X
        FieldShape = new Cube { HalfSideX = 0.1f, HalfSideY = 9f, HalfSideZ = 9f },
        Position = new Vector3(below - 0.15f, 0f, 0f),
        InheritPosition = true,
        Restitution = 0.02f,
        Friction = 0.03f,
    };

    /// <summary>A wind at a height: a slab of air that pushes what is in it sideways.</summary>
    private static UpdaterForceField Wind(float height, float force) => new()
    {
        FieldShape = new Cube { HalfSideX = 12f, HalfSideY = 2f, HalfSideZ = 12f },
        Position = new Vector3(0f, height, 0f),
        InheritPosition = false,
        InheritRotation = false,
        FieldFalloff = new FieldFalloff { StrengthInside = 1f, FalloffStart = 0.6f, StrengthOutside = 0f, FalloffEnd = 1f },
        ForceDirected = force,
        ForceVortex = 0f,
        ForceRepulsive = 0f,
        EnergyConservation = 0.9f,
    };

    /// <summary>Eight nozzles in a ring and one in the middle, in the plane across the emitter's X.</summary>
    private static Vector3[] ClusterNozzles(float radius)
    {
        var nozzles = new Vector3[9];

        for (var i = 0; i < 8; i++)
        {
            var (sin, cos) = MathF.SinCos(i * MathF.Tau / 8f);

            nozzles[i + 1] = new Vector3(0f, cos * radius, sin * radius);
        }

        return nozzles;
    }

    // --- Scenery --------------------------------------------------------------------------------

    private static readonly Color Steel = new(70, 72, 78);
    private static readonly Color Paint = new(205, 208, 212);
    private static readonly Color Frame = new(120, 95, 60);

    private static void KeroseneStand(ParticleStation s, List<Entity> model)
    {
        model.Add(Cone(s, 0.95f, 1f, StandNozzle + new Vector3(-0.5f, 0f, 0f), Steel, Lying));
        model.Add(Tube(s, 0.55f, 1.1f, StandNozzle + new Vector3(-1.5f, 0f, 0f), Paint, Lying));
        model.Add(Tube(s, 0.14f, 1f, StandNozzle + new Vector3(-0.5f, -0.05f, 0.6f), Steel, Lying));
        model.Add(Leg(s, StandNozzle.X - 1.5f, StandNozzle.Y - 0.25f));
    }

    private static void MethaneStand(ParticleStation s, List<Entity> model)
    {
        model.Add(Cone(s, 1f, 1.1f, StandNozzle + new Vector3(-0.55f, 0f, 0f), new Color(60, 58, 64), Lying));
        model.Add(Tube(s, 0.6f, 1f, StandNozzle + new Vector3(-1.55f, 0f, 0f), Steel, Lying));
        model.Add(Leg(s, StandNozzle.X - 1.55f, StandNozzle.Y - 0.28f));
    }

    private static void JetStand(ParticleStation s, List<Entity> model)
    {
        // A jet engine is a long tube, and its nozzle is a short ring at the end of it
        model.Add(Tube(s, 0.85f, 0.5f, StandNozzle + new Vector3(-0.25f, 0f, 0f), new Color(50, 50, 55), Lying));
        model.Add(Tube(s, 1.05f, 2.2f, StandNozzle + new Vector3(-1.6f, 0f, 0f), Paint, Lying));
        model.Add(Leg(s, StandNozzle.X - 1.6f, StandNozzle.Y - 0.5f));
    }

    private static void ClusterRocket(ParticleStation s, List<Entity> model, float diameter, float nozzleHeight)
    {
        model.Add(Tube(s, 2.4f, 4f, new Vector3(0f, nozzleHeight + 0.5f + 2f, 0f), Paint));

        // The emitter's Y and Z, turned to point down, are the station's X and Z
        foreach (var nozzle in ClusterNozzles(0.78f))
        {
            model.Add(Cone(s, diameter * 1.25f, 0.55f, new Vector3(nozzle.Y, nozzleHeight + 0.27f, nozzle.Z), Steel));
        }
    }

    private static void BoosterRocket(ParticleStation s, List<Entity> model)
    {
        var body = Tube(s, 0.5f, 2.2f, new Vector3(-2.5f, 1.6f, 0f), Paint);
        var nose = Cone(s, 0.5f, 0.7f, Vector3.Zero, Paint);

        // The nose rides on the body: a child of it, at the body's top
        nose.Scene = null;
        nose.Transform.Position = new Vector3(0f, 1.45f, 0f);
        nose.Transform.Rotation = Quaternion.Identity;
        body.AddChild(nose);

        model.Add(body);
    }

    private static void Spacecraft(ParticleStation s, List<Entity> model)
    {
        // A box of a spacecraft with a solar panel through it, and the thruster on its side
        model.Add(Part(s, PrimitiveModelType.Cube, new Vector3(1.6f, 1.6f, 1.6f), new Vector3(-3.7f, 2.6f, 0f), new Color(190, 170, 110)));
        model.Add(Part(s, PrimitiveModelType.Cube, new Vector3(0.08f, 3.6f, 1.2f), new Vector3(-3.7f, 2.6f, 0f), new Color(40, 60, 130)));
        model.Add(Cone(s, 0.4f, 0.4f, new Vector3(-2.75f, 2.6f, 0f), Steel, Lying));
    }

    /// <summary>A cylinder by its diameter and its length. It stands on end unless it is turned.</summary>
    private static Entity Tube(ParticleStation s, float diameter, float length, Vector3 local, Color colour, Quaternion? rotation = null)
        => Part(s, PrimitiveModelType.Cylinder, new Vector3(diameter / 2f, 0f, length), local, colour, rotation);

    /// <summary>A cone by the diameter of its base and its height. Its tip points up unless it is turned.</summary>
    private static Entity Cone(ParticleStation s, float diameter, float height, Vector3 local, Color colour, Quaternion? rotation = null)
        => Part(s, PrimitiveModelType.Cone, new Vector3(diameter / 2f, height, 0f), local, colour, rotation);

    /// <summary>The leg of a test stand: a post from the ground up to a height.</summary>
    private static Entity Leg(ParticleStation s, float x, float height)
        => Part(s, PrimitiveModelType.Cube, new Vector3(0.5f, height, 1.1f), new Vector3(x, height / 2f, 0f), Frame);

    /// <summary>
    /// One piece of scenery: a primitive in the station's coordinates, turned with the station. The
    /// size means what the primitive takes it to mean: a radius and a height for a cone, a radius in
    /// X and a length in Z for a cylinder.
    /// </summary>
    private static Entity Part(ParticleStation s, PrimitiveModelType type, Vector3 size, Vector3 local, Color colour, Quaternion? rotation = null)
    {
        var part = s.Game.Create3DPrimitive(type, new Primitive3DEntityOptions
        {
            EntityName = $"Station {s.Number} {type}",
            Material = s.Game.CreateMaterial(colour, metalness: 0.4f, glossiness: 0.6f),
            Size = size,
        });

        part.Transform.Position = s.At(local);
        part.Transform.Rotation = (rotation ?? Quaternion.Identity) * s.FacingRotation();
        part.Scene = s.Scene;

        return part;
    }

    /// <summary>
    /// What a thruster station keeps between frames: its scenery, built once, and the throttle, the
    /// schedule and the extra work of the variation that is up.
    /// </summary>
    private sealed class Engine
    {
        private Engine()
        {
        }

        internal List<Entity> Model { get; } = [];

        internal Throttle Throttle { get; set; } = new();

        /// <summary>The throttle at a moment, from the station's seconds.</summary>
        internal Func<float, float> Schedule { get; set; } = _ => 1f;

        /// <summary>What else the variation does every frame, or nothing.</summary>
        internal Action<ParticleStation>? Tick { get; set; }

        /// <summary>The station's engine, with its scenery built the first time.</summary>
        internal static Engine Of(ParticleStation s, Action<ParticleStation, List<Entity>> build)
        {
            if (s.State is Engine engine) return engine;

            engine = new Engine();

            build(s, engine.Model);

            s.State = engine;

            return engine;
        }
    }
}