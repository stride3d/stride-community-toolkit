using BenchmarkDotNet.Attributes;
using Stride.Core.Collections; // for FastList baseline
using Stride.Core.Mathematics;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

namespace Stride.CommunityToolkit.Benchmarks.FastList;

/// <summary>
/// Compares the existing FastList-based implementation with native C# alternatives:
/// 1. FastList (baseline) - current implementation style
/// 2. Raw arrays with manual resize (pow2 growth) + direct ref access
/// 3. List<T> using Add padding + CollectionsMarshal.AsSpan friendly layout (will show overhead of keeping Count correct)
///
/// NOTE: Only struct element types are used (Vector3, Quaternion, Color) so clearing on shrink is skipped like fastClear=true.
/// </summary>
[MemoryDiagnoser]
public class FastListAlternativeBenchmarks
{
    [Params(256, 1024, 4096)]
    public int N;

    // -------------------- Shared RNG --------------------
    int lcgState = 123456789;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    float NextFloat() => (lcgState = unchecked(lcgState * 1664525 + 1013904223)) * (1.0f / int.MaxValue);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    float NextSignedFloat() => (NextFloat() * 2f) - 1f;

    // =================================================================================================
    // 1. FastList baseline (mirrors existing FastListBenchmarks.ResizeAndInitialize_FastClear + others)
    // =================================================================================================
    FastList<Vector3> fl_positions = new(4);
    FastList<Vector3> fl_velocities = new(4);
    FastList<Vector3> fl_rotVelocities = new(4);
    FastList<Quaternion> fl_rotations = new(4);
    FastList<Color> fl_colors = new(4);

    void FastListEnsureInit()
    {
        fl_positions.Resize(N, true);
        fl_velocities.Resize(N, true);
        fl_rotVelocities.Resize(N, true);
        fl_rotations.Resize(N, true);
        fl_colors.Resize(N, true);

        for (int i = 0; i < N; i++)
        {
            fl_positions.Items[i] = new Vector3(NextSignedFloat() * 64f, NextSignedFloat() * 64f, NextSignedFloat() * 64f);
            fl_velocities.Items[i] = new Vector3(NextFloat() * 4f, NextFloat() * 4f, NextFloat() * 4f);
            fl_rotVelocities.Items[i] = new Vector3(NextFloat(), NextFloat(), NextFloat());
            fl_rotations.Items[i] = Quaternion.Identity;
            ref var c = ref fl_colors.Items[i];
            ref readonly var p = ref fl_positions.Items[i];
            c.R = (byte)(((p.X / 64f) + 1f) * 0.5f * 255f);
            c.G = (byte)(((p.Y / 64f) + 1f) * 0.5f * 255f);
            c.B = (byte)(((p.Z / 64f) + 1f) * 0.5f * 255f);
            c.A = 255;
        }
    }

    [Benchmark(Baseline = true, Description = "FastList: Resize + init")] 
    public (Vector3, Quaternion) FastList_Init()
    {
        FastListEnsureInit();
        return (fl_positions.Items[N - 1], fl_rotations.Items[N - 1]);
    }

    [Benchmark(Description = "FastList: Update loop")] 
    public (Vector3, Color) FastList_Update()
    {
        FastListEnsureInit(); // keeps filling every time (intent: isolate structure cost, not separate init)
        var dt = 1f / 60f; var areaSize = 64f;
        for (int i = 0; i < N; i++)
        {
            ref var pos = ref fl_positions.Items[i];
            ref var vel = ref fl_velocities.Items[i];
            ref var rvel = ref fl_rotVelocities.Items[i];
            ref var rot = ref fl_rotations.Items[i];
            ref var col = ref fl_colors.Items[i];
            if (pos.X > areaSize || pos.X < -areaSize) vel.X = -vel.X;
            if (pos.Y > areaSize || pos.Y < -areaSize) vel.Y = -vel.Y;
            if (pos.Z > areaSize || pos.Z < -areaSize) vel.Z = -vel.Z;
            pos += vel * dt;
            rot *= Quaternion.RotationX(rvel.X * dt) * Quaternion.RotationY(rvel.Y * dt) * Quaternion.RotationZ(rvel.Z * dt);
            col.R = (byte)(((pos.X / areaSize) + 1f) * 0.5f * 255f);
            col.G = (byte)(((pos.Y / areaSize) + 1f) * 0.5f * 255f);
            col.B = (byte)(((pos.Z / areaSize) + 1f) * 0.5f * 255f);
            col.A = 255;
        }
        return (fl_positions.Items[N - 1], fl_colors.Items[N - 1]);
    }

    [Benchmark(Description = "FastList: Color only")] 
    public int FastList_ColorOnly()
    {
        FastListEnsureInit();
        int checksum = 0; var areaSize = 64f;
        for (int i = 0; i < N; i++)
        {
            ref var pos = ref fl_positions.Items[i];
            ref var col = ref fl_colors.Items[i];
            col.R = (byte)(((pos.X / areaSize) + 1f) * 0.5f * 255f);
            col.G = (byte)(((pos.Y / areaSize) + 1f) * 0.5f * 255f);
            col.B = (byte)(((pos.Z / areaSize) + 1f) * 0.5f * 255f);
            col.A = 255;
            checksum += col.R + col.G + col.B;
        }
        return checksum;
    }

    // =================================================================================================
    // 2. Raw arrays (manual growth). Mimics fastClear=true semantics (no clearing on shrink / resize)
    // =================================================================================================
    Vector3[] arr_positions = System.Array.Empty<Vector3>();
    Vector3[] arr_velocities = System.Array.Empty<Vector3>();
    Vector3[] arr_rotVelocities = System.Array.Empty<Vector3>();
    Quaternion[] arr_rotations = System.Array.Empty<Quaternion>();
    Color[] arr_colors = System.Array.Empty<Color>();

    void ArraysEnsureInit()
    {
        Ensure(ref arr_positions, N);
        Ensure(ref arr_velocities, N);
        Ensure(ref arr_rotVelocities, N);
        Ensure(ref arr_rotations, N);
        Ensure(ref arr_colors, N);

        for (int i = 0; i < N; i++)
        {
            arr_positions[i] = new Vector3(NextSignedFloat() * 64f, NextSignedFloat() * 64f, NextSignedFloat() * 64f);
            arr_velocities[i] = new Vector3(NextFloat() * 4f, NextFloat() * 4f, NextFloat() * 4f);
            arr_rotVelocities[i] = new Vector3(NextFloat(), NextFloat(), NextFloat());
            arr_rotations[i] = Quaternion.Identity;
            ref var c = ref arr_colors[i];
            ref readonly var p = ref arr_positions[i];
            c.R = (byte)(((p.X / 64f) + 1f) * 0.5f * 255f);
            c.G = (byte)(((p.Y / 64f) + 1f) * 0.5f * 255f);
            c.B = (byte)(((p.Z / 64f) + 1f) * 0.5f * 255f);
            c.A = 255;
        }
    }

    static void Ensure<T>(ref T[] arr, int size)
    {
        if (arr.Length < size)
        {
            int newLen = arr.Length == 0 ? 4 : arr.Length;
            while (newLen < size) newLen *= 2; // power-of-two growth similar to List<T>
            Array.Resize(ref arr, newLen);
        }
    }

    [Benchmark(Description = "Arrays: Resize + init")] 
    public (Vector3, Quaternion) Arrays_Init()
    {
        ArraysEnsureInit();
        return (arr_positions[N - 1], arr_rotations[N - 1]);
    }

    [Benchmark(Description = "Arrays: Update loop")] 
    public (Vector3, Color) Arrays_Update()
    {
        ArraysEnsureInit();
        var dt = 1f / 60f; var areaSize = 64f;
        for (int i = 0; i < N; i++)
        {
            ref var pos = ref arr_positions[i];
            ref var vel = ref arr_velocities[i];
            ref var rvel = ref arr_rotVelocities[i];
            ref var rot = ref arr_rotations[i];
            ref var col = ref arr_colors[i];
            if (pos.X > areaSize || pos.X < -areaSize) vel.X = -vel.X;
            if (pos.Y > areaSize || pos.Y < -areaSize) vel.Y = -vel.Y;
            if (pos.Z > areaSize || pos.Z < -areaSize) vel.Z = -vel.Z;
            pos += vel * dt;
            rot *= Quaternion.RotationX(rvel.X * dt) * Quaternion.RotationY(rvel.Y * dt) * Quaternion.RotationZ(rvel.Z * dt);
            col.R = (byte)(((pos.X / areaSize) + 1f) * 0.5f * 255f);
            col.G = (byte)(((pos.Y / areaSize) + 1f) * 0.5f * 255f);
            col.B = (byte)(((pos.Z / areaSize) + 1f) * 0.5f * 255f);
            col.A = 255;
        }
        return (arr_positions[N - 1], arr_colors[N - 1]);
    }

    [Benchmark(Description = "Arrays: Color only")] 
    public int Arrays_ColorOnly()
    {
        ArraysEnsureInit();
        int checksum = 0; var areaSize = 64f;
        for (int i = 0; i < N; i++)
        {
            ref var pos = ref arr_positions[i];
            ref var col = ref arr_colors[i];
            col.R = (byte)(((pos.X / areaSize) + 1f) * 0.5f * 255f);
            col.G = (byte)(((pos.Y / areaSize) + 1f) * 0.5f * 255f);
            col.B = (byte)(((pos.Z / areaSize) + 1f) * 0.5f * 255f);
            col.A = 255;
            checksum += col.R + col.G + col.B;
        }
        return checksum;
    }

    // =================================================================================================
    // 3. List<T> (will incur additional cost to grow Count). Shows overhead vs direct array and FastList.
    // =================================================================================================
    List<Vector3> list_positions = new(4);
    List<Vector3> list_velocities = new(4);
    List<Vector3> list_rotVelocities = new(4);
    List<Quaternion> list_rotations = new(4);
    List<Color> list_colors = new(4);

    void ListEnsureInit()
    {
        Grow(list_positions, N);
        Grow(list_velocities, N);
        Grow(list_rotVelocities, N);
        Grow(list_rotations, N);
        Grow(list_colors, N);

        // Local aliases for faster range checks (JIT can hoist) – using indexer has bounds check but predictable.
        for (int i = 0; i < N; i++)
        {
            list_positions[i] = new Vector3(NextSignedFloat() * 64f, NextSignedFloat() * 64f, NextSignedFloat() * 64f);
            list_velocities[i] = new Vector3(NextFloat() * 4f, NextFloat() * 4f, NextFloat() * 4f);
            list_rotVelocities[i] = new Vector3(NextFloat(), NextFloat(), NextFloat());
            list_rotations[i] = Quaternion.Identity;
            var p = list_positions[i];
            var r = (byte)(((p.X / 64f) + 1f) * 0.5f * 255f);
            var g = (byte)(((p.Y / 64f) + 1f) * 0.5f * 255f);
            var b = (byte)(((p.Z / 64f) + 1f) * 0.5f * 255f);
            list_colors[i] = new Color(r, g, b, 255);
        }
    }

    static void Grow<T>(List<T> list, int size)
    {
        if (list.Count < size)
        {
            list.Capacity = Math.Max(list.Capacity, size);
            int toAdd = size - list.Count;
            // Add default(T) without per-element range re-growth.
            for (int i = 0; i < toAdd; i++) list.Add(default!);
        }
    }

    [Benchmark(Description = "List<T>: Resize + init")] 
    public (Vector3, Quaternion) List_Init()
    {
        ListEnsureInit();
        return (list_positions[N - 1], list_rotations[N - 1]);
    }

    [Benchmark(Description = "List<T>: Update loop")] 
    public (Vector3, Color) List_Update()
    {
        ListEnsureInit();
        var dt = 1f / 60f; var areaSize = 64f;
        for (int i = 0; i < N; i++)
        {
            var pos = list_positions[i];
            var vel = list_velocities[i];
            var rvel = list_rotVelocities[i];
            var rot = list_rotations[i];
            var col = list_colors[i];
            if (pos.X > areaSize || pos.X < -areaSize) vel.X = -vel.X;
            if (pos.Y > areaSize || pos.Y < -areaSize) vel.Y = -vel.Y;
            if (pos.Z > areaSize || pos.Z < -areaSize) vel.Z = -vel.Z;
            pos += vel * dt;
            rot *= Quaternion.RotationX(rvel.X * dt) * Quaternion.RotationY(rvel.Y * dt) * Quaternion.RotationZ(rvel.Z * dt);
            col.R = (byte)(((pos.X / areaSize) + 1f) * 0.5f * 255f);
            col.G = (byte)(((pos.Y / areaSize) + 1f) * 0.5f * 255f);
            col.B = (byte)(((pos.Z / areaSize) + 1f) * 0.5f * 255f);
            col.A = 255;
            list_positions[i] = pos;
            list_velocities[i] = vel;
            list_rotVelocities[i] = rvel;
            list_rotations[i] = rot;
            list_colors[i] = col;
        }
        return (list_positions[N - 1], list_colors[N - 1]);
    }

    [Benchmark(Description = "List<T>: Color only")] 
    public int List_ColorOnly()
    {
        ListEnsureInit();
        int checksum = 0; var areaSize = 64f;
        for (int i = 0; i < N; i++)
        {
            var pos = list_positions[i];
            var col = list_colors[i];
            col.R = (byte)(((pos.X / areaSize) + 1f) * 0.5f * 255f);
            col.G = (byte)(((pos.Y / areaSize) + 1f) * 0.5f * 255f);
            col.B = (byte)(((pos.Z / areaSize) + 1f) * 0.5f * 255f);
            col.A = 255;
            list_colors[i] = col;
            checksum += col.R + col.G + col.B;
        }
        return checksum;
    }
}
