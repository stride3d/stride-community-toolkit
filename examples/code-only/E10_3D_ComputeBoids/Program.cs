using E10_3D_ComputeBoids;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.Instancing;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Scripts.Utilities;
using Stride.CommunityToolkit.Skyboxes;
using Stride.CommunityToolkit.Windows;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;
using Stride.Input;
using Stride.Rendering;
using Stride.Rendering.Compositing;
using Stride.Rendering.ComputeEffect;
using Buffer = Stride.Graphics.Buffer;

// A flock of a few thousand boids that lives entirely on the GPU. A compute shader steers every
// boid against every other boid each frame and writes the world matrices straight into the
// buffers an instanced mesh is drawn from. The CPU sets up the buffers once and then never
// touches a boid again: no arrays, no upload, nothing per frame but a dispatch and a draw.
//
// Two engine pieces meet here. ComputeEffectShader runs any shader that inherits ComputeShaderBase
// and overrides Compute(); InstancingUserBuffer draws one model for every matrix in a buffer the
// caller owns. Point the first at the second and the simulation and the rendering never leave the
// card.
//
// SPACE freezes the flock, R scatters it again, 1-3 change how many boids there are.

int[] boidCounts = [2048, 4096, 8192];

WindowsDpiManager.EnablePerMonitorV2();

using var game = new Game();

// Compute shaders with more than one writable buffer need shader model 5, which is Direct3D
// feature level 11. A code-only game has no settings asset and starts at the engine's default,
// level 10, where the shader compiles for cs_4_0 - one UAV slot, so the second buffer is refused.
// UseGameSettings is how a code-only game asks for a profile.
game.UseGameSettings(settings => settings.GetOrCreateConfiguration<RenderingSettings>().DefaultGraphicsProfile = GraphicsProfile.Level_11_0);

BoidsSimulation? simulation = null;
BoidsComputeRenderer? computeRenderer = null;
Entity? flock = null;
Model? boidModel = null;
var countIndex = 1;

game.Run(start: Start, update: Update);

void Start(Scene scene)
{
    game.Window.AllowUserResizing = true;

    game.SetupBase3DScene();
    game.AddSkybox();
    game.AddProfiler();

    // Code-only projects have to register the instancing render feature themselves; without it an
    // InstancingComponent draws its model once, at the entity, and the flock is one cone
    game.AddInstancingSupport();

    // Look at the flock from a little way back; the base scene's controller takes over from here
    var camera = game.GetCameraEntity();

    camera.Transform.Position = new Vector3(0, 9f, 26f);
    camera.Transform.Rotation = Quaternion.RotationX(MathUtil.DegreesToRadians(-8f));

    // The boid: a small cone, tip along +Y, which the shader points along the heading. The entity
    // is only a way to get the model; it never enters the scene.
    boidModel = game.Create3DPrimitive(PrimitiveModelType.Cone, new Primitive3DEntityOptions
    {
        Size = new Vector3(0.14f, 0.4f, 0.14f),
        Material = game.CreateMaterial(new Color(255, 150, 40)),
    }).Get<ModelComponent>().Model;

    Rebuild(scene);

    // The compute pass goes first in the compositor's list, so the matrices are written before the
    // scene that draws them is rendered - same command list, same frame
    computeRenderer = new BoidsComputeRenderer { Simulation = () => simulation };

    var compositor = game.SceneSystem.GraphicsCompositor!;

    if (compositor.Game is SceneRendererCollection collection)
    {
        collection.Children.Insert(0, computeRenderer);
    }
    else
    {
        compositor.Game = new SceneRendererCollection { computeRenderer, compositor.Game };
    }

    var overlay = DebugOverlay.GetOrCreate(game);

    overlay.AddSection("Boids", OverlayLines);
}

void Update(Scene scene, GameTime gameTime)
{
    if (simulation is null) return;

    if (game.Input.IsKeyPressed(Keys.Space)) simulation.Paused = !simulation.Paused;
    if (game.Input.IsKeyPressed(Keys.R)) simulation.Scatter(game.GraphicsContext.CommandList);

    for (var i = 0; i < boidCounts.Length; i++)
    {
        if (game.Input.IsKeyPressed(Keys.D1 + i) && i != countIndex)
        {
            countIndex = i;
            Rebuild(scene);
        }
    }
}

/// <summary>Creates the flock at the chosen size: the buffers, and the one entity that draws them all.</summary>
void Rebuild(Scene scene)
{
    flock?.Scene = null;
    simulation?.Dispose();

    simulation = new BoidsSimulation(game.GraphicsDevice, game.GraphicsContext.CommandList, boidCounts[countIndex]);

    // One entity, one model, one draw call: the InstancingUserBuffer says how many copies to draw
    // and hands over the matrix buffers the compute shader fills
    flock = new Entity("Flock")
    {
        new ModelComponent(boidModel),
        new InstancingComponent { Type = simulation.Instancing },
    };

    flock.Scene = scene;
}

IReadOnlyList<TextElement> OverlayLines()
{
    if (simulation is null) return [];

    var pairs = (long)simulation.Count * simulation.Count;

    return
    [
        new("Space", simulation.Paused ? "Resume" : "Freeze", Color.Yellow),
        new("R", "Scatter", Color.Yellow),
        new(["1", "2", "3"], "Flock size", Color.Yellow),
        new(""),
        new($"{simulation.Count} boids, {pairs / 1_000_000f:0.0} million pairs a frame", Color.LightGreen),
        new("All on the GPU: one dispatch, one instanced draw call,", Color.LightGray),
        new("nothing per frame on the CPU", Color.LightGray),
    ];
}

/// <summary>
/// The flock's GPU state: two boid buffers that swap every frame, the two matrix buffers the mesh
/// renderer reads, and the instancing type that hands them over.
/// </summary>
public sealed class BoidsSimulation : IDisposable
{
    // Where the flock lives and how it flies. All of it goes to the shader every dispatch, so
    // any of it could be a slider.
    public Vector3 Center = new(0, 6f, 0);
    public float Bounds = 9f;
    public float MaxSpeed = 6f;
    public float NeighbourRadius = 2.0f;
    public float SeparationRadius = 0.9f;
    public float SeparationWeight = 14f;
    public float AlignmentWeight = 1.6f;
    public float CohesionWeight = 0.9f;
    public float HomeWeight = 1.5f;

    public bool Paused;

    private readonly Buffer<Boid>[] _boids = new Buffer<Boid>[2];
    private int _current;

    public BoidsSimulation(GraphicsDevice device, CommandList commandList, int count)
    {
        Count = count;

        // Unordered access: a compute shader writes them. The second one starts empty; it is
        // written before it is ever read.
        _boids[0] = Buffer.Structured.New(device, Scattered(count), unorderedAccess: true);
        _boids[1] = Buffer.Structured.New<Boid>(device, count, unorderedAccess: true);

        // Shader resource for the mesh renderer to read, unordered access for the compute shader
        // to write - the same flags the engine gives its own instance buffers, plus the write
        WorldBuffer = Buffer.New<Matrix>(device, count, BufferFlags.ShaderResource | BufferFlags.StructuredBuffer | BufferFlags.UnorderedAccess);
        WorldInverseBuffer = Buffer.New<Matrix>(device, count, BufferFlags.ShaderResource | BufferFlags.StructuredBuffer | BufferFlags.UnorderedAccess);

        // The bounding box is for culling: generous, so the flock is never culled at its edges.
        // Ignore: the instance matrix is the whole transform, the entity's own is not applied.
        Instancing = new InstancingUserBuffer
        {
            InstanceCount = count,
            InstanceWorldBuffer = WorldBuffer,
            InstanceWorldInverseBuffer = WorldInverseBuffer,
            BoundingBox = new BoundingBox(Center - new Vector3(Bounds * 3f), Center + new Vector3(Bounds * 3f)),
            ModelTransformUsage = ModelTransformUsage.Ignore,
        };

        // Matrices start as identity, so the first frame draws something sane even before the
        // compute pass has run
        var identity = new Matrix[count];

        Array.Fill(identity, Matrix.Identity);

        WorldBuffer.SetData(commandList, identity);
        WorldInverseBuffer.SetData(commandList, identity);
    }

    public int Count { get; }

    public InstancingUserBuffer Instancing { get; }

    public Buffer<Matrix> WorldBuffer { get; }

    public Buffer<Matrix> WorldInverseBuffer { get; }

    /// <summary>Last frame's flock, read by every thread this frame.</summary>
    public Buffer<Boid> Input => _boids[_current];

    /// <summary>This frame's flock, written once per thread.</summary>
    public Buffer<Boid> Output => _boids[1 - _current];

    /// <summary>After a dispatch: what was written becomes what is read.</summary>
    public void Swap() => _current = 1 - _current;

    /// <summary>Puts every boid back to its scattered starting place and heading. The seed is fixed, so it is the same every time.</summary>
    public void Scatter(CommandList commandList) => Input.SetData(commandList, Scattered(Count));

    private Boid[] Scattered(int count)
    {
        var random = new Random(1234);
        var boids = new Boid[count];

        for (var i = 0; i < count; i++)
        {
            var position = Center + RandomInSphere(random) * Bounds;
            var velocity = RandomInSphere(random) * MaxSpeed;

            boids[i] = new Boid(position, velocity);
        }

        return boids;
    }

    private static Vector3 RandomInSphere(Random random)
    {
        while (true)
        {
            var candidate = new Vector3(random.NextSingle() * 2f - 1f, random.NextSingle() * 2f - 1f, random.NextSingle() * 2f - 1f);

            if (candidate.LengthSquared() <= 1f && candidate.LengthSquared() > 0.01f) return candidate;
        }
    }

    public void Dispose()
    {
        _boids[0].Dispose();
        _boids[1].Dispose();
        WorldBuffer.Dispose();
        WorldInverseBuffer.Dispose();
    }
}

/// <summary>One boid as the shader sees it: two float4s, the fourth components unused.</summary>
public readonly record struct Boid(Vector4 Position, Vector4 Velocity)
{
    public Boid(Vector3 position, Vector3 velocity) : this(new Vector4(position, 0f), new Vector4(velocity, 0f))
    {
    }
}

/// <summary>
/// The compute pass: a scene renderer that runs first in the compositor and dispatches the boids
/// shader once per frame. ComputeEffectShader wraps the effect, the thread counts and the pipeline
/// state; all this does is bind the buffers, set the numbers and dispatch.
/// </summary>
public sealed class BoidsComputeRenderer : SceneRendererBase
{
    // Set by the example; a scene renderer is a data contract and needs a parameterless constructor
    public Func<BoidsSimulation?> Simulation { private get; set; } = () => null;

    // One thread per boid, in groups of this many; the shader ignores the threads past the end
    private const int ThreadsPerGroup = 256;

    private ComputeEffectShader? _compute;

    protected override void InitializeCore()
    {
        base.InitializeCore();

        _compute = new ComputeEffectShader(Context)
        {
            ShaderSourceName = "BoidsShader",
            ThreadNumbers = new Int3(ThreadsPerGroup, 1, 1),
        };
    }

    protected override void DrawCore(RenderContext context, RenderDrawContext drawContext)
    {
        if (_compute is null || Simulation() is not { Paused: false } flock) return;

        _compute.ThreadGroupCounts = new Int3((flock.Count + ThreadsPerGroup - 1) / ThreadsPerGroup, 1, 1);

        // Clamped so a hitch does not fling the flock apart
        var deltaTime = MathF.Min((float)context.Time.Elapsed.TotalSeconds, 1f / 30f);

        var parameters = _compute.Parameters;

        parameters.Set(BoidsShaderKeys.DeltaTime, deltaTime);
        parameters.Set(BoidsShaderKeys.BoidCount, (uint)flock.Count);
        parameters.Set(BoidsShaderKeys.FlockCenter, flock.Center);
        parameters.Set(BoidsShaderKeys.Bounds, flock.Bounds);
        parameters.Set(BoidsShaderKeys.MaxSpeed, flock.MaxSpeed);
        parameters.Set(BoidsShaderKeys.NeighbourRadius, flock.NeighbourRadius);
        parameters.Set(BoidsShaderKeys.SeparationRadius, flock.SeparationRadius);
        parameters.Set(BoidsShaderKeys.SeparationWeight, flock.SeparationWeight);
        parameters.Set(BoidsShaderKeys.AlignmentWeight, flock.AlignmentWeight);
        parameters.Set(BoidsShaderKeys.CohesionWeight, flock.CohesionWeight);
        parameters.Set(BoidsShaderKeys.HomeWeight, flock.HomeWeight);
        parameters.Set(BoidsShaderKeys.BoidsIn, flock.Input);
        parameters.Set(BoidsShaderKeys.BoidsOut, flock.Output);
        parameters.Set(BoidsShaderKeys.InstanceWorld, flock.WorldBuffer);
        parameters.Set(BoidsShaderKeys.InstanceWorldInverse, flock.WorldInverseBuffer);

        _compute.Draw(drawContext);

        flock.Swap();
    }

    protected override void Destroy()
    {
        _compute?.Dispose();

        base.Destroy();
    }
}

/*
---example-metadata
slug: compute-boids
title:
  en: Compute Shader Boids
  cs: Hejno na compute shaderu
level: Advanced
category: Performance
complexity: 4
order: 135
description:
  en: |-
    A flock of thousands of boids that lives entirely on the GPU. A compute shader steers every boid
    against every other one each frame - keep apart, fly the same way, stay together, come home - and
    writes the world matrices straight into the buffers an instanced cone mesh is drawn from. The CPU
    fills the buffers once and never touches a boid again: one dispatch, one draw call, nothing per
    frame. Freeze the flock, scatter it, or switch between two, four and eight thousand boids.
  cs: |-
    Hejno tisíců boidů, které žije celé na GPU. Compute shader každý snímek řídí každého boida vůči
    všem ostatním - držet odstup, letět stejným směrem, držet se pohromadě, vracet se domů - a zapisuje
    světové matice přímo do bufferů, ze kterých se instancovaně kreslí kužel. CPU buffery jednou
    naplní a boida se už nikdy nedotkne: jeden dispatch, jedno vykreslovací volání, nic za snímek.
    Hejno lze zmrazit, rozprášit nebo přepnout mezi dvěma, čtyřmi a osmi tisíci boidy.
concepts:
  - Writing a compute shader in SDSL by inheriting ComputeShaderBase and overriding Compute()
  - Running it with ComputeEffectShader from a scene renderer placed first in the compositor
  - Unordered-access structured buffers, and two of them swapped each frame so reads never race writes
  - Filling an InstancingUserBuffer's matrix buffers on the GPU, so the mesh is drawn from data the CPU never sees
  - Building a world matrix and its inverse in a shader from a heading
  - "Using helpers: SetupBase3DScene"
  - "Using helpers: AddSkybox"
  - "Using helpers: Create3DPrimitive"
  - "Using helpers: DebugOverlay"
tags:
  - 3D
  - Rendering
  - Compute Shader
  - Instancing
  - GPU
  - Performance
  - Boids
  - Shader
related:
  - E10_3D_Instancing
  - E10_3D_Instancing_EntityTransform
  - E09_3D_RootRendererShader
enabled: true
screenshotFrame: 360
created: 2026-09-06
---
*/