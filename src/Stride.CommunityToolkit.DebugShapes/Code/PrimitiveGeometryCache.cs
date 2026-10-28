using Stride.Graphics;

namespace Stride.CommunityToolkit.DebugShapes;

/// <summary>
/// Holds generated primitive meshes and computed offsets to pack them into shared vertex and index buffers.
/// </summary>
internal sealed class PrimitiveGeometryCache
{
    private const float DefaultCircleRadius = 0.5f;
    private const float DefaultSphereRadius = 0.5f;
    private const float DefaultCubeSize = 1.0f;
    private const float DefaultCapsuleLength = 1.0f;
    private const float DefaultCapsuleRadius = 0.5f;
    private const float DefaultCylinderHeight = 1.0f;
    private const float DefaultCylinderRadius = 0.5f;
    private const float DefaultPlaneSize = 1.0f;
    private const float DefaultConeRadius = 0.5f;
    private const float DefaultConeHeight = 1.0f;

    private const int CircleTesselation = 16;
    private const int SphereTesselation = 8;
    private const int CapsuleTesselation = 8;
    private const int CylinderTesselation = 16;
    private const int ConeTesselation = 16;

    private (VertexPositionTexture[] Vertices, int[] Indices) _circle = default!;
    private (VertexPositionTexture[] Vertices, int[] Indices) _plane = default!;
    private (VertexPositionTexture[] Vertices, int[] Indices) _sphere = default!;
    private (VertexPositionTexture[] Vertices, int[] Indices) _cube = default!;
    private (VertexPositionTexture[] Vertices, int[] Indices) _capsule = default!;
    private (VertexPositionTexture[] Vertices, int[] Indices) _cylinder = default!;
    private (VertexPositionTexture[] Vertices, int[] Indices) _cone = default!;

    /// <summary>
    /// Vertex offsets into the packed vertex buffer for each primitive type.
    /// </summary>
    internal Primitives VertexOffsets { get; private set; }

    /// <summary>
    /// Index offsets into the packed index buffer for each primitive type.
    /// </summary>
    internal Primitives IndexOffsets { get; private set; }

    /// <summary>
    /// Half-sphere index count (derived from sphere indices).
    /// </summary>
    internal int HalfSphereIndexCount { get; private set; }

    /// <summary>
    /// Builds geometry primitives and returns a packed vertex array while computing vertex offsets.
    /// </summary>
    internal VertexPositionTexture[] BuildVertexData()
    {
        _circle = ImmediateDebugPrimitives.GenerateCircle(DefaultCircleRadius, CircleTesselation);
        _plane = ImmediateDebugPrimitives.GenerateQuad(DefaultPlaneSize, DefaultPlaneSize);
        _sphere = ImmediateDebugPrimitives.GenerateSphere(DefaultSphereRadius, SphereTesselation, uvSplitOffsetVertical: 1);
        _cube = ImmediateDebugPrimitives.GenerateCube(DefaultCubeSize);
        _capsule = ImmediateDebugPrimitives.GenerateCapsule(DefaultCapsuleLength, DefaultCapsuleRadius, CapsuleTesselation);
        _cylinder = ImmediateDebugPrimitives.GenerateCylinder(DefaultCylinderHeight, DefaultCylinderRadius, CylinderTesselation);
        _cone = ImmediateDebugPrimitives.GenerateCone(DefaultConeHeight, DefaultConeRadius, ConeTesselation, uvSplits: 8);

        var vertexData = new VertexPositionTexture[
            _circle.Vertices.Length +
            _plane.Vertices.Length +
            _sphere.Vertices.Length +
            _cube.Vertices.Length +
            _capsule.Vertices.Length +
            _cylinder.Vertices.Length +
            _cone.Vertices.Length
        ];

        var offsets = default(Primitives);
        var next = 0;

        // The half sphere is the first half of the sphere's mesh, so it shares the sphere's offset
        offsets.Circles = Append(_circle.Vertices, vertexData, ref next);
        offsets.Quads = Append(_plane.Vertices, vertexData, ref next);
        offsets.Spheres = offsets.HalfSpheres = Append(_sphere.Vertices, vertexData, ref next);
        offsets.Cubes = Append(_cube.Vertices, vertexData, ref next);
        offsets.Capsules = Append(_capsule.Vertices, vertexData, ref next);
        offsets.Cylinders = Append(_cylinder.Vertices, vertexData, ref next);
        offsets.Cones = Append(_cone.Vertices, vertexData, ref next);

        VertexOffsets = offsets;
        HalfSphereIndexCount = _sphere.Indices.Length / 2;

        return vertexData;
    }

    /// <summary>
    /// Builds a packed index array and computes index offsets for each primitive.
    /// </summary>
    internal int[] BuildIndexData()
    {
        var indexData = new int[
            _circle.Indices.Length +
            _plane.Indices.Length +
            _sphere.Indices.Length +
            _cube.Indices.Length +
            _capsule.Indices.Length +
            _cylinder.Indices.Length +
            _cone.Indices.Length
        ];

        var offsets = default(Primitives);
        var next = 0;

        offsets.Circles = Append(_circle.Indices, indexData, ref next);
        offsets.Quads = Append(_plane.Indices, indexData, ref next);
        offsets.Spheres = offsets.HalfSpheres = Append(_sphere.Indices, indexData, ref next);
        offsets.Cubes = Append(_cube.Indices, indexData, ref next);
        offsets.Capsules = Append(_capsule.Indices, indexData, ref next);
        offsets.Cylinders = Append(_cylinder.Indices, indexData, ref next);
        offsets.Cones = Append(_cone.Indices, indexData, ref next);

        IndexOffsets = offsets;

        return indexData;
    }

    /// <summary>Copies <paramref name="source"/> into <paramref name="destination"/> at the next free slot and returns where it starts.</summary>
    private static int Append<T>(T[] source, T[] destination, ref int next)
    {
        var start = next;

        Array.Copy(source, 0, destination, start, source.Length);
        next += source.Length;

        return start;
    }

    // Expose per-primitive index counts for draw calls
    internal int CircleIndexCount => _circle.Indices.Length;
    internal int QuadIndexCount => _plane.Indices.Length;
    internal int SphereIndexCount => _sphere.Indices.Length;
    internal int CubeIndexCount => _cube.Indices.Length;
    internal int CapsuleIndexCount => _capsule.Indices.Length;
    internal int CylinderIndexCount => _cylinder.Indices.Length;
    internal int ConeIndexCount => _cone.Indices.Length;
}