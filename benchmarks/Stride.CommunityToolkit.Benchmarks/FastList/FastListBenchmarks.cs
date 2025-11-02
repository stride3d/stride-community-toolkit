using BenchmarkDotNet.Attributes;
using Stride.Core.Collections;
using Stride.Core.Mathematics;
using System.Runtime.CompilerServices;

namespace Stride.CommunityToolkit.Benchmarks.FastList;

#pragma warning disable CS0618 // Type or member is obsolete
[MemoryDiagnoser]
public class FastListBenchmarks
{
    // Parameters roughly matching primitive counts used in ShapeUpdater
    [Params(256, 1024, 4096)]
    public int N;

    FastList<Vector3> positions = new(4);
    FastList<Vector3> velocities = new(4);
    FastList<Vector3> rotVelocities = new(4);
    FastList<Quaternion> rotations = new(4);
    FastList<Color> colors = new(4);

    // Cache to know whether data for the current N has been initialized (since BenchmarkDotNet creates a new instance per benchmark method)
    int initializedForN = -1;

    // Simple deterministic LCG to avoid System.Random overhead dominating small benches
    int lcgState = 123456789;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    float NextFloat() => (lcgState = unchecked(lcgState * 1664525 + 1013904223)) * (1.0f / int.MaxValue);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    float NextSignedFloat() => (NextFloat() * 2f) - 1f;

    [GlobalSetup]
    public void GlobalSetup()
    {
        // Ensure underlying arrays are allocated at least with initial small capacity
        positions.Resize(0, true);
        velocities.Resize(0, true);
        rotVelocities.Resize(0, true);
        rotations.Resize(0, true);
        colors.Resize(0, true);
        initializedForN = -1; // force initialization for first benchmark using EnsureInitialized
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    void EnsureInitialized()
    {
        // Lazy init so non-baseline benchmarks have valid data without depending on execution order.
        if (initializedForN == N)
            return;

        positions.Resize(N, true);
        velocities.Resize(N, true);
        rotVelocities.Resize(N, true);
        rotations.Resize(N, true);
        colors.Resize(N, true);

        for (int i = 0; i < N; i++)
        {
            positions.Items[i] = new Vector3(NextSignedFloat() * 64f, NextSignedFloat() * 64f, NextSignedFloat() * 64f);
            velocities.Items[i] = new Vector3(NextFloat() * 4f, NextFloat() * 4f, NextFloat() * 4f);
            rotVelocities.Items[i] = new Vector3(NextFloat(), NextFloat(), NextFloat());
            rotations.Items[i] = Quaternion.Identity;
            ref var c = ref colors.Items[i];
            ref readonly var p = ref positions.Items[i];
            c.R = (byte)(((p.X / 64f) + 1f) * 0.5f * 255f);
            c.G = (byte)(((p.Y / 64f) + 1f) * 0.5f * 255f);
            c.B = (byte)(((p.Z / 64f) + 1f) * 0.5f * 255f);
            c.A = 255;
        }

        initializedForN = N;
    }

    [Benchmark(Description = "Resize + fast init")] // baseline: FastList fast-clear style initialization
    public (Vector3, Quaternion) ResizeAndInitialize_FastClear()
    {
        // FastList specific Resize with fastClear=true then fill underlying arrays directly
        positions.Resize(N, true);
        velocities.Resize(N, true);
        rotVelocities.Resize(N, true);
        rotations.Resize(N, true);
        colors.Resize(N, true);

        // Fill
        for (int i = 0; i < N; i++)
        {
            positions.Items[i] = new Vector3(NextSignedFloat() * 64f, NextSignedFloat() * 64f, NextSignedFloat() * 64f);
            velocities.Items[i] = new Vector3(NextFloat() * 4f, NextFloat() * 4f, NextFloat() * 4f);
            rotVelocities.Items[i] = new Vector3(NextFloat(), NextFloat(), NextFloat());
            rotations.Items[i] = Quaternion.Identity;
            ref var c = ref colors.Items[i];
            ref readonly var p = ref positions.Items[i];
            c.R = (byte)(((p.X / 64f) + 1f) * 0.5f * 255f);
            c.G = (byte)(((p.Y / 64f) + 1f) * 0.5f * 255f);
            c.B = (byte)(((p.Z / 64f) + 1f) * 0.5f * 255f);
            c.A = 255;
        }

        // Mark initialized so subsequent Update/Color benchmarks reuse data for same N without re-initializing unless N changes
        initializedForN = N;

        // Return a couple values so the JIT cannot eliminate loops
        return (positions.Items[N - 1], rotations.Items[N - 1]);
    }

    [Benchmark(Description = "Update loop (position + rotation + color)")]
    public (Vector3, Color) UpdateLoop()
    {
        EnsureInitialized();

        var dt = 1f / 60f; // fixed step
        var areaSize = 64f;
        for (int i = 0; i < N; i++)
        {
            ref var pos = ref positions.Items[i];
            ref var vel = ref velocities.Items[i];
            ref var rvel = ref rotVelocities.Items[i];
            ref var rot = ref rotations.Items[i];
            ref var col = ref colors.Items[i];

            // boundary reflect (same logic pattern as ShapeUpdater)
            if (pos.X > areaSize || pos.X < -areaSize) vel.X = -vel.X;
            if (pos.Y > areaSize || pos.Y < -areaSize) vel.Y = -vel.Y;
            if (pos.Z > areaSize || pos.Z < -areaSize) vel.Z = -vel.Z;
            pos += vel * dt;

            rot *= Quaternion.RotationX(rvel.X * dt) *
                   Quaternion.RotationY(rvel.Y * dt) *
                   Quaternion.RotationZ(rvel.Z * dt);

            col.R = (byte)(((pos.X / areaSize) + 1f) * 0.5f * 255f);
            col.G = (byte)(((pos.Y / areaSize) + 1f) * 0.5f * 255f);
            col.B = (byte)(((pos.Z / areaSize) + 1f) * 0.5f * 255f);
            col.A = 255;
        }
        return (positions.Items[(N - 1) & (N - 1)], colors.Items[(N - 1) & (N - 1)]);
    }

    [Benchmark(Description = "Color recompute only")]
    public int ColorOnly()
    {
        EnsureInitialized();

        var areaSize = 64f;
        int checksum = 0;
        for (int i = 0; i < N; i++)
        {
            ref var pos = ref positions.Items[i];
            ref var col = ref colors.Items[i];
            col.R = (byte)(((pos.X / areaSize) + 1f) * 0.5f * 255f);
            col.G = (byte)(((pos.Y / areaSize) + 1f) * 0.5f * 255f);
            col.B = (byte)(((pos.Z / areaSize) + 1f) * 0.5f * 255f);
            col.A = 255;
            checksum += col.R + col.G + col.B;
        }
        return checksum;
    }
}
#pragma warning restore CS0618 // Type or member is obsolete