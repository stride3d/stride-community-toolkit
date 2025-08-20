using BenchmarkDotNet.Attributes;
using Stride.Core.Collections; // for FastList baseline
using Stride.Core.Mathematics;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Runtime.InteropServices; // CollectionsMarshal

namespace Stride.CommunityToolkit.Benchmarks.FastList;

/// <summary>
/// Compares the existing FastList-based implementation with native C# alternatives:
/// 1. FastList (baseline) - current implementation style
/// 2. Raw arrays with manual resize (pow2 growth) + direct ref access
/// 3. List<T> using Add padding
/// 4. List<T> + CollectionsMarshal.AsSpan (span-based mutation to minimize bounds checks)
///
/// NOTE: Only struct element types are used (Vector3, Quaternion, Color) so clearing on shrink is skipped like fastClear=true.
/// Every benchmark re-initializes its data to isolate container + access cost.
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
    // 3. List<T> classic (indexer based)
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

    // =================================================================================================
    // 4. List<T> + CollectionsMarshal.AsSpan (span-based loops)
    //    Minimizes per-iteration bounds checks and copy-in/out for structs, approaching raw array cost.
    // =================================================================================================
    void ListSpanEnsureInit()
    {
        Grow(list_positions, N);
        Grow(list_velocities, N);
        Grow(list_rotVelocities, N);
        Grow(list_rotations, N);
        Grow(list_colors, N);

        var posSpan = CollectionsMarshal.AsSpan(list_positions);
        var velSpan = CollectionsMarshal.AsSpan(list_velocities);
        var rvelSpan = CollectionsMarshal.AsSpan(list_rotVelocities);
        var rotSpan = CollectionsMarshal.AsSpan(list_rotations);
        var colSpan = CollectionsMarshal.AsSpan(list_colors);

        for (int i = 0; i < N; i++)
        {
            posSpan[i] = new Vector3(NextSignedFloat() * 64f, NextSignedFloat() * 64f, NextSignedFloat() * 64f);
            velSpan[i] = new Vector3(NextFloat() * 4f, NextFloat() * 4f, NextFloat() * 4f);
            rvelSpan[i] = new Vector3(NextFloat(), NextFloat(), NextFloat());
            rotSpan[i] = Quaternion.Identity;
            ref var p = ref posSpan[i];
            colSpan[i] = new Color(
                (byte)(((p.X / 64f) + 1f) * 0.5f * 255f),
                (byte)(((p.Y / 64f) + 1f) * 0.5f * 255f),
                (byte)(((p.Z / 64f) + 1f) * 0.5f * 255f),
                255);
        }
    }

    [Benchmark(Description = "List<T> Span: Resize + init")] 
    public (Vector3, Quaternion) ListSpan_Init()
    {
        ListSpanEnsureInit();
        var rotSpan = CollectionsMarshal.AsSpan(list_rotations);
        var posSpan = CollectionsMarshal.AsSpan(list_positions);
        return (posSpan[N - 1], rotSpan[N - 1]);
    }

    [Benchmark(Description = "List<T> Span: Update loop")] 
    public (Vector3, Color) ListSpan_Update()
    {
        ListSpanEnsureInit();
        var posSpan = CollectionsMarshal.AsSpan(list_positions);
        var velSpan = CollectionsMarshal.AsSpan(list_velocities);
        var rvelSpan = CollectionsMarshal.AsSpan(list_rotVelocities);
        var rotSpan = CollectionsMarshal.AsSpan(list_rotations);
        var colSpan = CollectionsMarshal.AsSpan(list_colors);
        var dt = 1f / 60f; var areaSize = 64f;
        for (int i = 0; i < N; i++)
        {
            ref var pos = ref posSpan[i];
            ref var vel = ref velSpan[i];
            ref var rvel = ref rvelSpan[i];
            ref var rot = ref rotSpan[i];
            ref var col = ref colSpan[i];
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
        return (posSpan[N - 1], colSpan[N - 1]);
    }

    [Benchmark(Description = "List<T> Span: Color only")] 
    public int ListSpan_ColorOnly()
    {
        ListSpanEnsureInit();
        var posSpan = CollectionsMarshal.AsSpan(list_positions);
        var colSpan = CollectionsMarshal.AsSpan(list_colors);
        int checksum = 0; var areaSize = 64f;
        for (int i = 0; i < N; i++)
        {
            ref var pos = ref posSpan[i];
            ref var col = ref colSpan[i];
            col.R = (byte)(((pos.X / areaSize) + 1f) * 0.5f * 255f);
            col.G = (byte)(((pos.Y / areaSize) + 1f) * 0.5f * 255f);
            col.B = (byte)(((pos.Z / areaSize) + 1f) * 0.5f * 255f);
            col.A = 255;
            checksum += col.R + col.G + col.B;
        }
        return checksum;
    }
}
