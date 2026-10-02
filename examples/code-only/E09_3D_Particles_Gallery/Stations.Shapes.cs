using Stride.Core.Mathematics;
using Stride.Particles;
using Stride.Particles.Initializers;
using Stride.Particles.Modules;
using Stride.Particles.ShapeBuilders;
using Stride.Particles.Spawners;

namespace E09_3D_Particles_Gallery;

/// <summary>The shape builders: what one particle is on screen.</summary>
public static class ShapeStations
{
    /// <summary>
    /// The same emitter under the three flat shapes. A billboard always faces the camera; a quad
    /// lies in the emitter's plane and is seen edge-on from the side; an oriented quad points along
    /// its velocity, which is what a spark or a raindrop wants.
    /// </summary>
    public static void FlatShapes(ParticleStation s)
    {
        var v = s.Pick("billboard", "quad", "oriented quad");

        var emitter = new ParticleEmitter
        {
            ParticleLifetime = new Vector2(1.5f, 2f),
            SimulationSpace = EmitterSimulationSpace.World,
            ShapeBuilder = v switch
            {
                0 => new ShapeBuilderBillboard(),
                1 => new ShapeBuilderQuad(),
                _ => new ShapeBuilderOrientedQuad { ScaleLength = true, LengthFactor = 3f },
            },
            Material = ParticleMaterials.Textured(s.Textures.Radial, new Color4(1f, 0.9f, 0.4f, 1f), additive: 1f),
        };

        emitter.Spawners.Add(new SpawnerPerSecond { SpawnCount = 40 });
        emitter.Initializers.Add(Initializers.AtEmitter());
        emitter.Initializers.Add(new InitialSizeSeed { RandomSize = new Vector2(0.25f, 0.4f) });
        emitter.Initializers.Add(new InitialVelocitySeed { VelocityMin = new Vector3(-2f, 3f, -2f), VelocityMax = new Vector3(2f, 5f, 2f) });
        emitter.Updaters.Add(new UpdaterGravity { GravitationalAcceleration = new Vector3(0f, -4f, 0f) });

        s.Place(new Vector3(0f, 0.3f, 0f), emitter);
    }

    /// <summary>
    /// A hexagon instead of a square, with its own rotation curve so every particle turns over its
    /// life. Six sides is enough for a coin, a snowflake, a bolt.
    /// </summary>
    public static void Hexagon(ParticleStation s)
    {
        var v = s.Pick("still", "one turn over life", "spinning");

        var emitter = new ParticleEmitter
        {
            ParticleLifetime = new Vector2(2f, 2.5f),
            SimulationSpace = EmitterSimulationSpace.World,
            ShapeBuilder = new ShapeBuilderHexagon
            {
                SamplerRotation = v switch { 0 => null, 1 => Curves.Float((0f, 0f), (1f, 360f)), _ => Curves.Float((0f, 0f), (1f, 1440f)) },
            },
            Material = ParticleMaterials.Flat(new Color4(0.4f, 0.9f, 1f, 1f), additive: 0.5f),
        };

        emitter.Spawners.Add(new SpawnerPerSecond { SpawnCount = 25 });
        emitter.Initializers.Add(new InitialSizeSeed { RandomSize = new Vector2(0.3f, 0.5f) });
        emitter.Initializers.Add(new InitialPositionSeed { PositionMin = new Vector3(-1.5f, 0f, -0.5f), PositionMax = new Vector3(1.5f, 0f, 0.5f) });
        emitter.Initializers.Add(new InitialVelocitySeed { VelocityMin = new Vector3(0f, 1f, 0f), VelocityMax = new Vector3(0f, 1.8f, 0f) });

        s.Place(new Vector3(0f, 0.3f, 0f), emitter);
    }

    /// <summary>
    /// A ribbon: consecutive particles joined into one strip, so a moving emitter leaves a band
    /// behind it. The particles must be sorted in spawn order or the strip jumps between them; the
    /// spawn-order initializer and the sorting policy are the two settings people forget.
    /// </summary>
    public static void Ribbon(ParticleStation s)
    {
        var v = s.Pick("no smoothing", "fast smoothing", "best smoothing");

        var emitter = new ParticleEmitter
        {
            ParticleLifetime = new Vector2(1.5f, 1.5f),
            SimulationSpace = EmitterSimulationSpace.World,
            SortingPolicy = EmitterSortingPolicy.ByOrder,
            ShapeBuilder = new ShapeBuilderRibbon
            {
                SmoothingPolicy = v switch { 0 => SmoothingPolicy.None, 1 => SmoothingPolicy.Fast, _ => SmoothingPolicy.Best },
                Segments = 3,
                TextureCoordinatePolicy = TextureCoordinatePolicy.Stretched,
                // The strip runs the texture along its length from 0 to this factor. The default is 0: every
                // vertex samples the top row of the texture, which on a radial glow is black, and the ribbon
                // is invisible with no error to say why
                TexCoordsFactor = 1f,
            },
            Material = ParticleMaterials.Textured(s.Textures.Radial, new Color4(0.4f, 1f, 0.6f, 1f), additive: 1f),
        };

        emitter.Spawners.Add(new SpawnerPerSecond { SpawnCount = 45 });
        emitter.Initializers.Add(new InitialSpawnOrder());
        emitter.Initializers.Add(new InitialSizeSeed { RandomSize = new Vector2(0.6f, 0.6f) });

        s.Place(new Vector3(0f, 2f, 0f), emitter);
    }

    /// <summary>Moves the station's emitter round a circle, for the ribbon.</summary>
    public static void Circle(ParticleStation s)
    {
        if (s.Entity is null) return;

        var (sin, cos) = MathF.SinCos(s.Seconds * 1.6f);

        // A ribbon turns its width to face the camera. Where its path runs towards the camera or away
        // from it there is no width to turn, and the strip twists into a fan of thin lines. So the path
        // stays in the plane that faces the visitor. And it turns gently: on a bend tighter than the strip is
        // wide, the inside edges of neighbouring segments cross and the overlap shows as bright specks.
        s.Entity.Transform.Position = s.At(cos * 2.6f, 3f + sin * 2f, 0f);
    }

    /// <summary>
    /// A trail is a ribbon with an edge: the strip hangs off one side of the particle's path
    /// instead of straddling it, the way a sword swing or a wing tip leaves a sheet in the air.
    /// </summary>
    public static void Trail(ParticleStation s)
    {
        var v = s.Pick("edge", "centre", "edge, no smoothing");

        var emitter = new ParticleEmitter
        {
            ParticleLifetime = new Vector2(0.8f, 0.8f),
            SimulationSpace = EmitterSimulationSpace.World,
            SortingPolicy = EmitterSortingPolicy.ByOrder,
            ShapeBuilder = new ShapeBuilderTrail
            {
                EdgePolicy = v == 1 ? EdgePolicy.Center : EdgePolicy.Edge,
                SmoothingPolicy = v == 2 ? SmoothingPolicy.None : SmoothingPolicy.Best,
                Segments = 5,
                TextureCoordinatePolicy = TextureCoordinatePolicy.Stretched,
                TexCoordsFactor = 1f, // See the ribbon: 0 by default, and 0 samples one black row
            },
            Material = ParticleMaterials.Textured(s.Textures.Radial, new Color4(1f, 0.5f, 0.9f, 1f), additive: 1f),
        };

        emitter.Spawners.Add(new SpawnerPerFrame { SpawnCount = 1 });
        emitter.Initializers.Add(new InitialSpawnOrder());
        emitter.Initializers.Add(Initializers.AtEmitter());
        emitter.Initializers.Add(new InitialSizeSeed { RandomSize = new Vector2(1.2f, 1.2f) });

        s.Place(new Vector3(0f, 2.5f, 0f), emitter);
    }

    /// <summary>Swings the station's emitter side to side like a blade, for the trail.</summary>
    public static void Swing(ParticleStation s)
    {
        if (s.Entity is null) return;

        var angle = MathF.Sin(s.Seconds * 2.4f) * 1.1f;
        var (sin, cos) = MathF.SinCos(angle);

        s.Entity.Transform.Position = s.At(sin * 3f, 2.5f + (1f - cos) * 2f, 0f);
    }

    /// <summary>
    /// Quads with a rotation in three dimensions, so a falling leaf tumbles rather than spinning
    /// flat on the screen. A random quaternion per particle, or one of two fixed ones.
    /// </summary>
    public static void Orientation(ParticleStation s)
    {
        var v = s.Pick("random tumble", "all flat", "all on edge");

        var emitter = new ParticleEmitter
        {
            ParticleLifetime = new Vector2(3f, 4f),
            SimulationSpace = EmitterSimulationSpace.World,
            ShapeBuilder = new ShapeBuilderQuad(),
            Material = ParticleMaterials.Flat(new Color4(1f, 0.7f, 0.2f, 1f)),
        };

        emitter.Spawners.Add(new SpawnerPerSecond { SpawnCount = 15 });
        emitter.Initializers.Add(new InitialSizeSeed { RandomSize = new Vector2(0.25f, 0.4f) });
        emitter.Initializers.Add(new InitialPositionSeed { PositionMin = new Vector3(-2f, 0f, -1f), PositionMax = new Vector3(2f, 0f, 1f) });
        emitter.Initializers.Add(new InitialVelocitySeed { VelocityMin = new Vector3(-0.3f, -1.2f, -0.3f), VelocityMax = new Vector3(0.3f, -0.8f, 0.3f) });

        var flat = Quaternion.RotationX(MathF.PI * 0.5f);
        var edge = Quaternion.RotationY(MathF.PI * 0.5f);

        emitter.Initializers.Add(new Initial3DRotationSeed
        {
            RotationQuaternionMin = v switch { 0 => Quaternion.RotationYawPitchRoll(-MathF.PI, -MathF.PI, -MathF.PI), 1 => flat, _ => edge },
            RotationQuaternionMax = v switch { 0 => Quaternion.RotationYawPitchRoll(MathF.PI, MathF.PI, MathF.PI), 1 => flat, _ => edge },
        });

        s.Place(new Vector3(0f, 4.5f, 0f), 2f, emitter);
    }
}