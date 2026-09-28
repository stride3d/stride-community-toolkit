using Example.Common.Galleries;
using Stride.Particles;
using Stride.Particles.Modules;
using Stride.Particles.Initializers;
using Stride.Particles.Materials;
using Stride.Rendering.Materials.ComputeColors;
using Stride.Particles.ShapeBuilders;
using Stride.Particles.Spawners;
using Stride.Particles.Updaters;

namespace E09_3D_Particles_Gallery;

/// <summary>
/// The registry: every station in gallery order, simplest first, each one a method in one of the
/// <c>Stations.*.cs</c> files. Add an entry and the ring grows to fit.
/// </summary>
public static class Stations
{
    public static IReadOnlyList<Exhibit<ParticleStation>> All { get; } =
    [
        // The building blocks, one at a time
        new("Fountain", "the original example: a spawner, three initializers, gravity", nameof(SpawnerPerSecond), Setup: BasicStations.Fountain),
        new("Burst", "everything at once, once or on a loop", nameof(SpawnerBurst), Setup: BasicStations.Burst),
        new("Per frame", "a fixed count every frame, whatever the frame rate", nameof(SpawnerPerFrame), Setup: BasicStations.PerFrame),
        new("Lifetime and warm-up", "how long a particle lives, and starting mid-effect", nameof(ParticleEmitter.ParticleLifetime), Setup: BasicStations.Lifetime),
        new("Blend and sort", "two emitters through each other: alpha, additive, sorting", nameof(ParticleEmitter.SortingPolicy), Setup: BasicStations.BlendAndSort),
        new("Billboard, quad, oriented", "the three flat shapes on one emitter", nameof(ShapeBuilderBillboard), Setup: ShapeStations.FlatShapes),
        new("Hexagon", "a six-sided shape with a rotation curve", nameof(ShapeBuilderHexagon), Setup: ShapeStations.Hexagon),
        new("Ribbon", "particles joined into a strip behind a moving emitter", nameof(ShapeBuilderRibbon), Setup: ShapeStations.Ribbon, Update: ShapeStations.Circle),
        new("Trail", "a strip with an edge, behind a swinging emitter", nameof(ShapeBuilderTrail), Setup: ShapeStations.Trail, Update: ShapeStations.Swing),
        new("3D orientation", "quads with a rotation in space, not just on screen", nameof(Initial3DRotationSeed), Setup: ShapeStations.Orientation),
        new("Position arc", "born along an arc to a target", nameof(InitialPositionArc), Setup: MotionStations.Arc),
        new("Direction and speed", "a cone of directions, stretched along the velocity", nameof(UpdaterSpeedToDirection), Setup: MotionStations.Direction),
        new("Rotation and spin", "an initial angle and a spin over life", nameof(UpdaterRotationOverTime), Setup: MotionStations.Spin),
        new("Force field", "a vortex, a repulsor, a wind", nameof(UpdaterForceField), Setup: MotionStations.ForceField),
        new("Collider", "particles that bounce off a shape or stay inside one", nameof(UpdaterCollider), Setup: MotionStations.Collider),
        new("Comet", "spawned by distance travelled, so a fast emitter leaves more", nameof(SpawnerFromDistance), Setup: MotionStations.Comet, Update: MotionStations.Orbit),
        new("Size over life", "a curve from birth to death", nameof(UpdaterSizeOverTime), Setup: LookStations.SizeOverLife),
        new("Colour over life", "a colour ramp from birth to death", nameof(UpdaterColorOverTime), Setup: LookStations.ColorOverLife),
        new("Textured smoke", "a soft texture, blended or added", nameof(ParticleMaterialSimple.AlphaAdditive), Setup: LookStations.Smoke),
        new("Flipbook fire", "an 8 by 8 sheet of frames over the particle's life", nameof(UVBuilderFlipbook), Setup: LookStations.Flipbook),
        new("Scrolling texture", "texture coordinates that slide over the particle's life", nameof(UVBuilderScroll), Setup: LookStations.Scroll),
        new("Soft particles", "fading where a particle meets geometry, instead of a hard cut", nameof(ParticleMaterialSimple.SoftEdgeDistance), Setup: LookStations.Soft, Pillars: 1),
        new("Colour graph", "a material from nodes: texture times colour, texture plus colour", nameof(ParticleMaterialComputeColor.ComputeColor), Setup: LookStations.ColorGraph),
        new("Shader node", "a particle drawn by a shader class of your own, no texture", nameof(ComputeShaderClassColor), Setup: LookStations.ShaderNode),

        // The showpieces, each several of the above at once
        new("Campfire", "flames, embers and smoke from three emitters", nameof(ParticleSystem), Setup: ShowStations.Campfire),
        new("Fireworks", "a rocket with a trail that bursts on death - child emitters", nameof(SpawnerFromParent), Setup: ShowStations.Fireworks),
        new("Tornado", "two thousand dust particles in a cylinder vortex", nameof(UpdaterForceField), Setup: ShowStations.Tornado),
        new("Fireflies", "a swarm pulled to a moving point by an updater of our own", nameof(SwarmUpdater), Setup: ShowStations.Fireflies, Update: ShowStations.Wander),
        new("Lasers", "ribbons in local space with a scrolling additive texture", nameof(EmitterSimulationSpace.Local), Setup: ShowStations.Lasers, Update: ShowStations.Turn),
        new("Rain", "drops that splash where they hit the floor - a collision trigger", nameof(ParticleSpawnTriggerCollision), Setup: SetPieceStations.Rain),
        new("Portal", "a ring from an initializer of our own, with sparks and a glow", nameof(RingInitializer), Setup: SetPieceStations.Portal),
        new("Landing burn", "a white-blue core, an orange plume, sparks, and exhaust that rolls across the pad", nameof(UpdaterCollider), Setup: SetPieceStations.RocketEngine),

        // Thrusters: one fuel each, from the same few layers
        new("Kerosene engine", "like Falcon 9's Merlin: a bright orange plume and a dark streak of soot", nameof(Exhaust), Setup: ThrusterStations.Kerosene, Update: ThrusterStations.Run),
        new("Engine cluster", "like a Falcon 9 liftoff: nine engines, one plume, from one emitter", nameof(ClusterInitializer), Setup: ThrusterStations.Cluster, Update: ThrusterStations.Run),
        new("Methane engine", "like Starship's Raptor: a see-through blue flame with shock diamonds", nameof(ShockCellInitializer), Setup: ThrusterStations.Methane, Update: ThrusterStations.Run),
        new("Jet afterburner", "orange with a blue root, and the best-known shock diamonds", nameof(Throttle), Setup: ThrusterStations.Afterburner, Update: ThrusterStations.Run),
        new("Solid booster", "a white flame and a thick trail that stays in the sky, spawned by distance", nameof(SpawnerFromDistance), Setup: ThrusterStations.SolidBooster, Update: ThrusterStations.Run),
        new("Small thrusters", "cold gas puffs, a hypergolic burst, an ion beam", nameof(SpawnerPerSecond), Setup: ThrusterStations.SmallThrusters, Update: ThrusterStations.Run),
    ];
}