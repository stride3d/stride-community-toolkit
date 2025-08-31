using Stride.Graphics;

namespace Stride.CommunityToolkit.DebugShapes.Code;

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

    private Primitives _vertexOffsets;
    private Primitives _indexOffsets;

    /// <summary>
    /// Vertex offsets into the packed vertex buffer for each primitive type.
    /// </summary>
    internal Primitives VertexOffsets => _vertexOffsets;

    /// <summary>
    /// Index offsets into the packed index buffer for each primitive type.
    /// </summary>
    internal Primitives IndexOffsets => _indexOffsets;

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

        int vertexBufferOffset = 0;
        var vertexOffsets = default(Primitives);

        Array.Copy(_circle.Vertices, 0, vertexData, vertexBufferOffset, _circle.Vertices.Length);
        vertexOffsets.Circles = vertexBufferOffset;
        vertexBufferOffset += _circle.Vertices.Length;

        Array.Copy(_plane.Vertices, 0, vertexData, vertexBufferOffset, _plane.Vertices.Length);
        vertexOffsets.Quads = vertexBufferOffset;
        vertexBufferOffset += _plane.Vertices.Length;

        Array.Copy(_sphere.Vertices, 0, vertexData, vertexBufferOffset, _sphere.Vertices.Length);
        vertexOffsets.Spheres = vertexBufferOffset;
        vertexOffsets.HalfSpheres = vertexBufferOffset;
        vertexBufferOffset += _sphere.Vertices.Length;

        Array.Copy(_cube.Vertices, 0, vertexData, vertexBufferOffset, _cube.Vertices.Length);
        vertexOffsets.Cubes = vertexBufferOffset;
        vertexBufferOffset += _cube.Vertices.Length;

        Array.Copy(_capsule.Vertices, 0, vertexData, vertexBufferOffset, _capsule.Vertices.Length);
        vertexOffsets.Capsules = vertexBufferOffset;
        vertexBufferOffset += _capsule.Vertices.Length;

        Array.Copy(_cylinder.Vertices, 0, vertexData, vertexBufferOffset, _cylinder.Vertices.Length);
        vertexOffsets.Cylinders = vertexBufferOffset;
        vertexBufferOffset += _cylinder.Vertices.Length;

        Array.Copy(_cone.Vertices, 0, vertexData, vertexBufferOffset, _cone.Vertices.Length);
        vertexOffsets.Cones = vertexBufferOffset;
        vertexBufferOffset += _cone.Vertices.Length;

        _vertexOffsets = vertexOffsets;
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

        int indexBufferOffset = 0;
        var indexOffsets = default(Primitives);

        Array.Copy(_circle.Indices, 0, indexData, indexBufferOffset, _circle.Indices.Length);
        indexOffsets.Circles = indexBufferOffset;
        indexBufferOffset += _circle.Indices.Length;

        Array.Copy(_plane.Indices, 0, indexData, indexBufferOffset, _plane.Indices.Length);
        indexOffsets.Quads = indexBufferOffset;
        indexBufferOffset += _plane.Indices.Length;

        Array.Copy(_sphere.Indices, 0, indexData, indexBufferOffset, _sphere.Indices.Length);
        indexOffsets.Spheres = indexBufferOffset;
        indexOffsets.HalfSpheres = indexBufferOffset;
        indexBufferOffset += _sphere.Indices.Length;

        Array.Copy(_cube.Indices, 0, indexData, indexBufferOffset, _cube.Indices.Length);
        indexOffsets.Cubes = indexBufferOffset;
        indexBufferOffset += _cube.Indices.Length;

        Array.Copy(_capsule.Indices, 0, indexData, indexBufferOffset, _capsule.Indices.Length);
        indexOffsets.Capsules = indexBufferOffset;
        indexBufferOffset += _capsule.Indices.Length;

        Array.Copy(_cylinder.Indices, 0, indexData, indexBufferOffset, _cylinder.Indices.Length);
        indexOffsets.Cylinders = indexBufferOffset;
        indexBufferOffset += _cylinder.Indices.Length;

        Array.Copy(_cone.Indices, 0, indexData, indexBufferOffset, _cone.Indices.Length);
        indexOffsets.Cones = indexBufferOffset;
        indexBufferOffset += _cone.Indices.Length;

        _indexOffsets = indexOffsets;
        return indexData;
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