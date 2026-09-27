using Stride.Core.Mathematics;
using Stride.Particles;
using Stride.Particles.Initializers;
using Stride.Particles.Materials;
using Stride.Particles.Modules;
using Stride.Particles.ShapeBuilders;
using Stride.Particles.Spawners;
using Stride.Particles.Updaters;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;

namespace E09_3D_Particles_Gallery;

/// <summary>What a particle looks like: curves over its life, textures, blending, the material graph.</summary>
public static class LookStations
{
    /// <summary>
    /// Size from a curve over the particle's life: grow, shrink, or pulse. The curve is keyframes
    /// on a 0 to 1 timeline, the same animation curve type the rest of the engine uses, and its
    /// value is the size itself in world units - the initial size only matters until the first update.
    /// </summary>
    public static void SizeOverLife(ParticleStation s)
    {
        var v = s.Pick("grow", "shrink", "pulse");

        var emitter = new ParticleEmitter
        {
            ParticleLifetime = new Vector2(2f, 2f),
            SimulationSpace = EmitterSimulationSpace.World,
            ShapeBuilder = new ShapeBuilderBillboard(),
            Material = ParticleMaterials.Textured(s.Textures.Radial, new Color4(0.5f, 0.8f, 1f, 1f), additive: 1f),
        };

        emitter.Spawners.Add(new SpawnerPerSecond { SpawnCount = 20 });
        emitter.Initializers.Add(new InitialSizeSeed { RandomSize = new Vector2(0.5f, 0.7f) });
        emitter.Initializers.Add(new InitialPositionSeed { PositionMin = new Vector3(-1.5f, 0f, -0.5f), PositionMax = new Vector3(1.5f, 0f, 0.5f) });
        emitter.Initializers.Add(new InitialVelocitySeed { VelocityMin = new Vector3(0f, 1f, 0f), VelocityMax = new Vector3(0f, 1.5f, 0f) });
        emitter.Updaters.Add(new UpdaterSizeOverTime
        {
            SamplerMain = v switch
            {
                0 => Curves.Float((0f, 0.1f), (1f, 1.5f)),
                1 => Curves.Float((0f, 1.5f), (1f, 0f)),
                _ => Curves.Float((0f, 0.2f), (0.25f, 1.2f), (0.5f, 0.3f), (0.75f, 1.2f), (1f, 0f)),
            },
        });

        s.Place(new Vector3(0f, 0.5f, 0f), emitter);
    }

    /// <summary>
    /// Colour from a curve over the particle's life, alpha included: a fade, a fire ramp from white
    /// through yellow and red to black, a rainbow. In HDR the values can go above one for glare.
    /// </summary>
    public static void ColorOverLife(ParticleStation s)
    {
        var v = s.Pick("fade out", "fire ramp", "rainbow");

        var emitter = new ParticleEmitter
        {
            ParticleLifetime = new Vector2(2f, 2.5f),
            SimulationSpace = EmitterSimulationSpace.World,
            ShapeBuilder = new ShapeBuilderBillboard(),
            Material = ParticleMaterials.Smoke(s.Textures, additive: v == 1 ? 1f : 0f),
        };

        emitter.Spawners.Add(new SpawnerPerSecond { SpawnCount = 25 });
        emitter.Initializers.Add(new InitialSizeSeed { RandomSize = new Vector2(0.5f, 0.9f) });
        emitter.Initializers.Add(new InitialPositionSeed { PositionMin = new Vector3(-0.5f, 0f, -0.5f), PositionMax = new Vector3(0.5f, 0f, 0.5f) });
        emitter.Initializers.Add(new InitialVelocitySeed { VelocityMin = new Vector3(-0.3f, 1.2f, -0.3f), VelocityMax = new Vector3(0.3f, 2f, 0.3f) });
        emitter.Updaters.Add(new UpdaterColorOverTime
        {
            SamplerMain = v switch
            {
                0 => Curves.Color((0f, new Color4(0.8f, 0.9f, 1f, 1f)), (1f, new Color4(0.8f, 0.9f, 1f, 0f))),
                1 => Curves.Color((0f, new Color4(2f, 2f, 1.6f, 1f)), (0.2f, new Color4(2f, 1.2f, 0.2f, 1f)), (0.5f, new Color4(1f, 0.2f, 0.05f, 0.8f)), (1f, new Color4(0f, 0f, 0f, 0f))),
                _ => Curves.Color((0f, new Color4(1f, 0f, 0f, 1f)), (0.33f, new Color4(0f, 1f, 0f, 1f)), (0.66f, new Color4(0f, 0f, 1f, 1f)), (1f, new Color4(1f, 0f, 1f, 0f))),
            },
        });

        s.Place(new Vector3(0f, 0.3f, 0f), 1f, emitter);
    }

    /// <summary>
    /// The smoke texture three ways: alpha-blended, which covers what is behind it; additive, which
    /// adds light and never darkens; and halfway, which is what most fire and magic wants. One
    /// number on the material.
    /// </summary>
    public static void Smoke(ParticleStation s)
    {
        var v = s.Pick("alpha blended", "additive", "half and half");

        var emitter = new ParticleEmitter
        {
            ParticleLifetime = new Vector2(2.5f, 3.5f),
            SimulationSpace = EmitterSimulationSpace.World,
            SortingPolicy = EmitterSortingPolicy.ByDepth,
            ShapeBuilder = new ShapeBuilderBillboard(),
            Material = ParticleMaterials.Smoke(s.Textures, new Color4(0.9f, 0.6f, 0.4f, 1f), additive: v switch { 0 => 0f, 1 => 1f, _ => 0.5f }),
        };

        emitter.Spawners.Add(new SpawnerPerSecond { SpawnCount = 18 });
        emitter.Initializers.Add(new InitialSizeSeed { RandomSize = new Vector2(1f, 1.6f) });
        emitter.Initializers.Add(new InitialPositionSeed { PositionMin = new Vector3(-0.4f, 0f, -0.4f), PositionMax = new Vector3(0.4f, 0f, 0.4f) });
        emitter.Initializers.Add(new InitialVelocitySeed { VelocityMin = new Vector3(-0.3f, 0.8f, -0.3f), VelocityMax = new Vector3(0.3f, 1.4f, 0.3f) });
        emitter.Initializers.Add(new InitialRotationSeed { AngularRotation = new Vector2(-180f, 180f) });
        emitter.Updaters.Add(new UpdaterSizeOverTime { SamplerMain = Curves.Float((0f, 0.8f), (1f, 2.2f)) });
        emitter.Updaters.Add(new UpdaterColorOverTime { SamplerMain = Curves.Color((0f, new Color4(1f, 1f, 1f, 0f)), (0.2f, new Color4(1f, 1f, 1f, 0.7f)), (1f, new Color4(1f, 1f, 1f, 0f))) });

        s.Place(new Vector3(0f, 0.3f, 0f), 2f, emitter);
    }

    /// <summary>
    /// A flipbook: the texture is an 8 by 8 grid of frames and each particle plays through them
    /// over its life. The speed is frames per life; 64 shows each frame once, 128 plays the sheet
    /// twice, 16 holds each frame four times as long.
    /// </summary>
    public static void Flipbook(ParticleStation s)
    {
        var v = s.Pick("64 frames a life", "128 frames a life", "16 frames a life");

        var emitter = new ParticleEmitter
        {
            ParticleLifetime = new Vector2(1.5f, 2f),
            SimulationSpace = EmitterSimulationSpace.World,
            ShapeBuilder = new ShapeBuilderBillboard(),
            Material = ParticleMaterials.Flipbook(s.Textures.Fire, 8, 8, v switch { 0 => 64u, 1 => 128u, _ => 16u }, additive: 1f),
        };

        emitter.Spawners.Add(new SpawnerPerSecond { SpawnCount = 12 });
        emitter.Initializers.Add(new InitialSizeSeed { RandomSize = new Vector2(1.2f, 1.8f) });
        emitter.Initializers.Add(new InitialPositionSeed { PositionMin = new Vector3(-1f, 0f, -0.3f), PositionMax = new Vector3(1f, 0f, 0.3f) });
        emitter.Initializers.Add(new InitialVelocitySeed { VelocityMin = new Vector3(0f, 0.6f, 0f), VelocityMax = new Vector3(0f, 1f, 0f) });

        s.Place(new Vector3(0f, 0.9f, 0f), 1f, emitter);
    }

    /// <summary>
    /// Scrolling texture coordinates: a rectangle of the texture slides from a start to an end
    /// over the particle's life, so a long oriented quad reads as a beam with movement in it.
    /// </summary>
    public static void Scroll(ParticleStation s)
    {
        var v = s.Pick("scroll up", "scroll down", "zoom in");

        var scroll = v switch
        {
            0 => new UVBuilderScroll { StartFrame = new Vector4(0f, 0f, 1f, 1f), EndFrame = new Vector4(0f, 2f, 1f, 3f) },
            1 => new UVBuilderScroll { StartFrame = new Vector4(0f, 2f, 1f, 3f), EndFrame = new Vector4(0f, 0f, 1f, 1f) },
            _ => new UVBuilderScroll { StartFrame = new Vector4(0f, 0f, 1f, 1f), EndFrame = new Vector4(0.4f, 0.4f, 0.6f, 0.6f) },
        };

        var emitter = new ParticleEmitter
        {
            ParticleLifetime = new Vector2(2f, 2f),
            SimulationSpace = EmitterSimulationSpace.World,
            ShapeBuilder = new ShapeBuilderOrientedQuad { ScaleLength = true, LengthFactor = 6f },
            Material = ParticleMaterials.Textured(s.Textures.Radial, new Color4(0.5f, 1f, 0.8f, 1f), additive: 1f, uv: scroll),
        };

        emitter.Spawners.Add(new SpawnerPerSecond { SpawnCount = 8 });
        emitter.Initializers.Add(new InitialSizeSeed { RandomSize = new Vector2(0.4f, 0.6f) });
        emitter.Initializers.Add(new InitialPositionSeed { PositionMin = new Vector3(-2f, 0f, -0.5f), PositionMax = new Vector3(2f, 0f, 0.5f) });
        emitter.Initializers.Add(new InitialVelocitySeed { VelocityMin = new Vector3(0f, 1.5f, 0f), VelocityMax = new Vector3(0f, 2f, 0f) });
        emitter.Updaters.Add(new UpdaterSpeedToDirection());

        s.Place(new Vector3(0f, 0.3f, 0f), emitter);
    }

    /// <summary>
    /// Soft particles: where a particle's quad cuts through geometry it fades over a distance
    /// instead of showing a hard line. The pillar is here for the smoke to drift through; at zero
    /// the intersection is a knife edge, at half a unit it melts.
    /// </summary>
    public static void Soft(ParticleStation s)
    {
        var v = s.Pick("hard, 0", "soft, 0.5", "very soft, 2");

        var emitter = new ParticleEmitter
        {
            ParticleLifetime = new Vector2(3f, 4f),
            SimulationSpace = EmitterSimulationSpace.World,
            SortingPolicy = EmitterSortingPolicy.ByDepth,
            ShapeBuilder = new ShapeBuilderBillboard(),
            Material = ParticleMaterials.Smoke(s.Textures, new Color4(0.85f, 0.9f, 1f, 1f), softEdge: v switch { 0 => 0f, 1 => 0.5f, _ => 2f }),
        };

        emitter.Spawners.Add(new SpawnerPerSecond { SpawnCount = 14 });
        emitter.Initializers.Add(new InitialSizeSeed { RandomSize = new Vector2(1.6f, 2.4f) });
        emitter.Initializers.Add(new InitialPositionSeed { PositionMin = new Vector3(-1f, 0f, -1f), PositionMax = new Vector3(1f, 0.5f, 1f) });
        emitter.Initializers.Add(new InitialVelocitySeed { VelocityMin = new Vector3(-0.4f, 0.5f, -0.4f), VelocityMax = new Vector3(0.4f, 1f, 0.4f) });
        emitter.Updaters.Add(new UpdaterColorOverTime { SamplerMain = Curves.Color((0f, new Color4(1f, 1f, 1f, 0f)), (0.2f, new Color4(1f, 1f, 1f, 0.8f)), (1f, new Color4(1f, 1f, 1f, 0f))) });

        // On the pillar's side, so the puffs drift through it
        s.Place(new Vector3(-3.5f, 0.5f, -1.5f), 2f, emitter);
    }

    /// <summary>
    /// The material is a graph of compute-colour nodes, the same nodes a mesh material's diffuse
    /// slot takes. A texture times a colour tints it; a texture plus a colour lifts it; a texture
    /// times a second texture masks one with the other. No shader is written for any of them.
    /// </summary>
    public static void ColorGraph(ParticleStation s)
    {
        var v = s.Pick("texture times colour", "texture plus colour", "texture times texture");

        IComputeColor graph = v switch
        {
            0 => new ComputeBinaryColor(new ComputeTextureColor(s.Textures.Radial), new ComputeColor(new Color4(1f, 0.4f, 0.8f, 1f)), BinaryOperator.Multiply),
            1 => new ComputeBinaryColor(new ComputeTextureColor(s.Textures.Dot), new ComputeColor(new Color4(0.1f, 0.2f, 0.5f, 0f)), BinaryOperator.Add),
            _ => new ComputeBinaryColor(new ComputeTextureColor(s.Textures.Dot), new ComputeTextureColor(s.Textures.Radial), BinaryOperator.Multiply),
        };

        var emitter = new ParticleEmitter
        {
            ParticleLifetime = new Vector2(2f, 3f),
            SimulationSpace = EmitterSimulationSpace.World,
            ShapeBuilder = new ShapeBuilderBillboard(),
            Material = new ParticleMaterialComputeColor { ComputeColor = graph, AlphaAdditive = v == 1 ? 1f : 0.3f },
        };

        emitter.Spawners.Add(new SpawnerPerSecond { SpawnCount = 16 });
        emitter.Initializers.Add(new InitialSizeSeed { RandomSize = new Vector2(0.8f, 1.3f) });
        emitter.Initializers.Add(new InitialPositionSeed { PositionMin = new Vector3(-1f, 0f, -0.5f), PositionMax = new Vector3(1f, 0f, 0.5f) });
        emitter.Initializers.Add(new InitialVelocitySeed { VelocityMin = new Vector3(-0.2f, 0.8f, -0.2f), VelocityMax = new Vector3(0.2f, 1.4f, 0.2f) });
        emitter.Updaters.Add(new UpdaterColorOverTime { SamplerMain = Curves.Color((0f, new Color4(1f, 1f, 1f, 0f)), (0.3f, Color4.White), (1f, new Color4(1f, 1f, 1f, 0f))) });

        s.Place(new Vector3(0f, 0.4f, 0f), 1f, emitter);
    }

    /// <summary>
    /// A particle drawn by a shader class instead of a texture: the material's colour node is a
    /// <c>ComputeShaderClassColor</c> naming a class from this example's <c>Effects</c> folder, which
    /// derives from <c>ComputeColor</c> and computes the quad's pixel from its texture coordinate. No
    /// image file, sharp at any size, and a generic turns one class into several looks. V switches
    /// between a star, a single ring and a target of rings.
    /// </summary>
    public static void ShaderNode(ParticleStation s)
    {
        var v = s.Pick("a star", "a ring", "a target of rings");

        IComputeColor shape = v switch
        {
            0 => new ComputeShaderClassColor { MixinReference = "GalleryParticleStar" },
            1 => new ComputeShaderClassColor { MixinReference = "GalleryParticleRings", Generics = { ["TRings"] = new ComputeColorParameterFloat { Value = 1f } } },
            _ => new ComputeShaderClassColor { MixinReference = "GalleryParticleRings", Generics = { ["TRings"] = new ComputeColorParameterFloat { Value = 4f } } },
        };

        var tint = v == 0 ? new Color4(1f, 0.85f, 0.4f, 1f) : new Color4(0.4f, 0.9f, 1f, 1f);

        var emitter = new ParticleEmitter
        {
            ParticleLifetime = new Vector2(2f, 3f),
            SimulationSpace = EmitterSimulationSpace.World,
            ShapeBuilder = new ShapeBuilderBillboard(),
            Material = new ParticleMaterialComputeColor
            {
                ComputeColor = new ComputeBinaryColor(shape, new ComputeColor(tint), BinaryOperator.Multiply),
                AlphaAdditive = 1f,
            },
        };

        emitter.Spawners.Add(new SpawnerPerSecond { SpawnCount = 10 });
        emitter.Initializers.Add(new InitialSizeSeed { RandomSize = new Vector2(0.9f, 1.6f) });
        emitter.Initializers.Add(new InitialPositionSeed { PositionMin = new Vector3(-1.5f, 0f, -0.5f), PositionMax = new Vector3(1.5f, 0.3f, 0.5f) });
        emitter.Initializers.Add(new InitialVelocitySeed { VelocityMin = new Vector3(-0.2f, 0.6f, -0.2f), VelocityMax = new Vector3(0.2f, 1.2f, 0.2f) });
        emitter.Updaters.Add(new UpdaterColorOverTime { SamplerMain = Curves.Color((0f, new Color4(1f, 1f, 1f, 0f)), (0.2f, Color4.White), (1f, new Color4(1f, 1f, 1f, 0f))) });

        s.Place(new Vector3(0f, 0.4f, 0f), 1f, emitter);
    }
}