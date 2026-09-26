using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Particles;
using Stride.Particles.Components;
using Stride.Particles.Initializers;
using Stride.Particles.Materials;
using Stride.Particles.Modules;
using Stride.Particles.ShapeBuilders;
using Stride.Particles.Spawners;
using Stride.Rendering.Materials.ComputeColors;

namespace Stride.CommunityToolkit.GoldScenes.Scenes;

/// <summary>
/// Two particle emitters from code with a fixed random seed: an alpha-blended fountain of orange
/// billboards under gravity, and an additive burst of blue quads that repeats. Pins the particle
/// pipeline the examples build on - the pool, the fixed seed, the spawners, the shape builders and
/// both blend modes through the forward renderer - at one simulated second.
/// </summary>
internal sealed class ParticlesScene : IGoldScene
{
    public void Start(Game game, Scene scene)
    {
        game.SetupBase3D();
        game.AddParticleRenderer();
        game.SetCameraPosition(new Vector3(0f, 3.5f, 10f));
        game.SetCameraRotation(new Vector3(0f, -12f, 0f));

        var ground = game.Create3DPrimitive(PrimitiveModelType.Cube, new Primitive3DEntityOptions
        {
            EntityName = "Ground",
            Material = game.CreateMaterial(new Color(38, 41, 47), metalness: 0.04f, glossiness: 0.25f),
            Size = new Vector3(20f, 0.5f, 20f),
            Position = new Vector3(0f, -0.25f, 0f),
        });

        ground.Scene = scene;

        var fountain = new ParticleEmitter
        {
            ParticleLifetime = new Vector2(1.2f, 1.8f),
            RandomSeedMethod = EmitterRandomSeedMethod.Fixed,
            SimulationSpace = EmitterSimulationSpace.World,
            ShapeBuilder = new ShapeBuilderBillboard(),
            Material = new ParticleMaterialComputeColor { ComputeColor = new ComputeColor(new Color4(1f, 0.55f, 0.15f, 0.85f)) },
        };

        fountain.Spawners.Add(new SpawnerPerSecond { SpawnCount = 150 });
        fountain.Initializers.Add(new InitialSizeSeed { RandomSize = new Vector2(0.15f, 0.3f) });
        fountain.Initializers.Add(new InitialVelocitySeed { VelocityMin = new Vector3(-1.2f, 5f, -1.2f), VelocityMax = new Vector3(1.2f, 7f, 1.2f) });
        fountain.Updaters.Add(new UpdaterGravity { GravitationalAcceleration = new Vector3(0f, -9.8f, 0f) });

        var burst = new ParticleEmitter
        {
            ParticleLifetime = new Vector2(0.9f, 0.9f),
            RandomSeedMethod = EmitterRandomSeedMethod.Fixed,
            SimulationSpace = EmitterSimulationSpace.World,
            ShapeBuilder = new ShapeBuilderQuad(),
            Material = new ParticleMaterialComputeColor { ComputeColor = new ComputeColor(new Color4(0.25f, 0.55f, 1.6f, 1f)), AlphaAdditive = 1f },
        };

        burst.Spawners.Add(new SpawnerBurst { SpawnCount = 120, LoopCondition = SpawnerLoopCondition.Looping, Delay = new Vector2(0.35f), Duration = new Vector2(0.01f) });
        burst.Initializers.Add(new InitialSizeSeed { RandomSize = new Vector2(0.25f, 0.4f) });
        burst.Initializers.Add(new InitialVelocitySeed { VelocityMin = new Vector3(-3f, 0.5f, -3f), VelocityMax = new Vector3(3f, 3f, 3f) });
        burst.Initializers.Add(new InitialRotationSeed { AngularRotation = new Vector2(0f, 360f) });
        burst.Updaters.Add(new UpdaterGravity { GravitationalAcceleration = new Vector3(0f, -4f, 0f) });

        var particles = new ParticleSystemComponent();

        particles.ParticleSystem.Emitters.Add(fountain);
        particles.ParticleSystem.Emitters.Add(burst);

        var entity = new Entity("Particles") { particles };

        entity.Transform.Position = new Vector3(0f, 0.5f, 0f);
        entity.Scene = scene;
    }

    public void Update(Game game, Scene scene, GameTime time)
    {
    }
}