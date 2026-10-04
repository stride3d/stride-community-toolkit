using Stride.Graphics;
using Stride.Rendering;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Buffer = System.Buffer;

namespace Stride.CommunityToolkit.Rendering.Utilities;

/// <summary>
/// Builds a <see cref="MeshDraw"/> from vertex elements of any unmanaged type, with optional indices.
/// </summary>
public class MeshBuilder : IDisposable
{
    /// <summary>
    ///     The min capacity of elements
    /// </summary>
    private const int MinCapacity = 256;

    /// <summary>
    ///     The list of vertex elements added to this mesh builder
    /// </summary>
    private readonly List<VertexElementWithOffset> _vertexElements = [];

    /// <summary>
    ///     A type erased buffer for the indices
    /// </summary>
    private byte[] _indexBuffer = [];

    /// <summary>
    ///     The size of an index can be 0, 2 (int16) or 4 (int32)
    /// </summary>
    private int _indexStride;

    /// <summary>
    ///     The backing field for <see cref="PrimitiveType" /> as we use a runtime check in the setter
    /// </summary>
    private PrimitiveType _primitiveType = PrimitiveType.TriangleList;

    /// <summary>
    ///     A type erased buffer for vertex elements
    /// </summary>
    private byte[] _vertexBuffer = [];

    /// <summary>
    ///     The size in bytes for single combination of vertex elements
    /// </summary>
    private int _vertexStride;

    /// <summary>
    ///     The selected primitive type (default: <see cref="PrimitiveType.TriangleList" />)
    /// </summary>
    public PrimitiveType PrimitiveType
    {
        get => _primitiveType;
        init => WithPrimitiveType(value);
    }

    /// <summary>
    ///     The selected index type (default: <see cref="IndexingType.None" />)
    /// </summary>
    public IndexingType IndexType
    {
        get => (IndexingType)_indexStride;
        init => WithIndexType(value);
    }

    /// <summary>
    ///     The vertex elements including offsets
    /// </summary>
    public IReadOnlyList<VertexElementWithOffset> VertexElements => _vertexElements;

    /// <summary>
    ///     The current vertex count
    /// </summary>
    public int VertexCount { get; private set; }

    /// <summary>
    ///     The current index count
    /// </summary>
    public int IndexCount { get; private set; }

    /// <inheritdoc />
    public void Dispose()
    {
        Clear();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     Changes the selected primitive type.
    /// </summary>
    /// <param name="primitiveType">The selected primitive type</param>
    /// <exception cref="InvalidOperationException">
    ///     If vertices were already added changing the primitive type is no longer
    ///     allowed
    /// </exception>
    public void WithPrimitiveType(PrimitiveType primitiveType)
    {
        if (VertexCount > 0)
        {
            throw new InvalidOperationException("Can not change primitive type if vertices were added already");
        }

        _primitiveType = primitiveType;
    }

    /// <summary>
    ///     Changes the selected indexing type.
    /// </summary>
    /// <param name="indexingType">The selected indexing type</param>
    /// <exception cref="InvalidOperationException">
    ///     If vertices were already added changing the indexing type is no longer
    ///     allowed
    /// </exception>
    public void WithIndexType(IndexingType indexingType)
    {
        if (VertexCount > 0)
        {
            throw new InvalidOperationException("Can not change vertex indexing type if vertices were added already");
        }

        _indexStride = (int)indexingType;
    }

    // These wrappers must forward pixelFormat. They once dropped it, which made the documented
    // escape hatch for types ConvertTypeToFormat cannot map unreachable through every one of them.

    /// <inheritdoc cref="WithElement{T}" />
    public int WithPosition<T>(int semanticIndex = 0, string semanticName = "POSITION",
        PixelFormat pixelFormat = PixelFormat.None) where T : unmanaged =>
        WithElement<T>(semanticIndex, semanticName, pixelFormat);

    /// <inheritdoc cref="WithElement{T}" />
    public int WithPositionTransformed<T>(int semanticIndex = 0, string semanticName = "SV_POSITION",
        PixelFormat pixelFormat = PixelFormat.None) where T : unmanaged =>
        WithElement<T>(semanticIndex, semanticName, pixelFormat);

    /// <inheritdoc cref="WithElement{T}" />
    public int WithNormal<T>(int semanticIndex = 0, string semanticName = "NORMAL",
        PixelFormat pixelFormat = PixelFormat.None) where T : unmanaged =>
        WithElement<T>(semanticIndex, semanticName, pixelFormat);

    /// <inheritdoc cref="WithElement{T}" />
    public int WithColor<T>(int semanticIndex = 0, string semanticName = "COLOR",
        PixelFormat pixelFormat = PixelFormat.None) where T : unmanaged =>
        WithElement<T>(semanticIndex, semanticName, pixelFormat);

    /// <inheritdoc cref="WithElement{T}" />
    public int WithTextureCoordinate<T>(int semanticIndex = 0, string semanticName = "TEXCOORD",
        PixelFormat pixelFormat = PixelFormat.None) where T : unmanaged =>
        WithElement<T>(semanticIndex, semanticName, pixelFormat);

    /// <inheritdoc cref="WithElement{T}" />
    public int WithTangent<T>(int semanticIndex = 0, string semanticName = "TANGENT",
        PixelFormat pixelFormat = PixelFormat.None) where T : unmanaged =>
        WithElement<T>(semanticIndex, semanticName, pixelFormat);

    /// <inheritdoc cref="WithElement{T}" />
    public int WithBiTangent<T>(int semanticIndex = 0, string semanticName = "BITANGENT",
        PixelFormat pixelFormat = PixelFormat.None) where T : unmanaged =>
        WithElement<T>(semanticIndex, semanticName, pixelFormat);

    /// <summary>
    ///     Registers a new vertex element
    /// </summary>
    /// <param name="semanticIndex">The semantic index</param>
    /// <param name="semanticName">The semantic name</param>
    /// <param name="pixelFormat">The pixel format (use <see cref="PixelFormat.None" /> to auto-detect)</param>
    /// <typeparam name="T">The element's value type, such as <see cref="Vector3"/></typeparam>
    /// <returns>The element index used in <see cref="GetElement{T}(int)" /> and <see cref="SetElement{T}(int,T)" /></returns>
    public int WithElement<T>(int semanticIndex, string semanticName, PixelFormat pixelFormat = PixelFormat.None)
        where T : unmanaged
    {
        if (VertexCount > 0)
        {
            throw new InvalidOperationException("Can not add elements, because vertices were added already");
        }

        if (pixelFormat == PixelFormat.None) pixelFormat = VertexElement.ConvertTypeToFormat<T>();

        var element = new VertexElement(semanticName, semanticIndex, pixelFormat, _vertexStride);
        var elementWithOffset = new VertexElementWithOffset(element, _vertexStride, Unsafe.SizeOf<T>());
        _vertexElements.Add(elementWithOffset);

        // Stride only allows offsets/sizes as a multiple of 4 so we need to add padding
        _vertexStride = (_vertexStride + Unsafe.SizeOf<T>() + (4 - 1)) & ~(4 - 1);
        return _vertexElements.Count - 1;
    }

    /// <summary>
    ///     Adds a new vertex index
    /// </summary>
    /// <param name="vertexIndex">The vertex index</param>
    /// <exception cref="ArgumentOutOfRangeException">The vertex index was outside the range of the currently added vertices</exception>
    /// <exception cref="ArgumentOutOfRangeException">The vertex index was outside the range of the selected indexing mode</exception>
    /// <exception cref="InvalidOperationException">The mesh builder isn't configured to use indices</exception>
    public void AddIndex(int vertexIndex)
    {
        // >= rather than >: an index equal to VertexCount references a vertex that does not exist,
        // which the GPU would happily read as garbage
        if (vertexIndex < 0 || vertexIndex >= VertexCount)
        {
            throw new ArgumentOutOfRangeException(nameof(vertexIndex),
                $"VertexIndex must be a value between 0 and {VertexCount - 1}");
        }

        if (_indexStride == Unsafe.SizeOf<short>() && vertexIndex > short.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(vertexIndex),
                $"VertexIndex must be a value between 0 and {short.MaxValue}. To use bigger values use indexing type {IndexingType.Int32}");
        }

        if (_indexStride == 0)
        {
            throw new InvalidOperationException("The mesh builder was not defined to use indices.");
        }

        if ((IndexCount + 1) * _indexStride <= _indexBuffer.Length)
        {
            IndexCount++;

            var indexOffset = (IndexCount - 1) * _indexStride;

            ref var address = ref Unsafe.AddByteOffset(ref MemoryMarshal.GetArrayDataReference(_indexBuffer), (nuint)indexOffset);

            if (_indexStride == 2)
                Unsafe.WriteUnaligned(ref address, (short)vertexIndex);
            else
                Unsafe.WriteUnaligned(ref address, vertexIndex);

            return;
        }

        AddIndexWithResize(vertexIndex);
    }

    /// <summary>
    ///     A more complex version of <see cref="AddIndex" /> including an array resize
    /// </summary>
    private void AddIndexWithResize(int vertexIndex)
    {
        var nextCapacity = Math.Max(MinCapacity * _indexStride, _indexBuffer.Length * 2);
        var nextBuffer = RentZeroed(nextCapacity, _indexBuffer);

        _indexBuffer = nextBuffer;

        IndexCount++;

        var indexOffset = (IndexCount - 1) * _indexStride;

        ref var address = ref Unsafe.AddByteOffset(ref MemoryMarshal.GetArrayDataReference(_indexBuffer), (nuint)indexOffset);

        if (_indexStride == 2)
            Unsafe.WriteUnaligned(ref address, (short)vertexIndex);
        else
            Unsafe.WriteUnaligned(ref address, vertexIndex);
    }

    /// <summary>
    ///     Reads back a previously added index.
    /// </summary>
    /// <param name="indexPosition">The position within the index list, from 0 to <see cref="IndexCount" /> - 1</param>
    /// <returns>The vertex index stored at that position</returns>
    /// <exception cref="ArgumentOutOfRangeException">The position is outside the added indices</exception>
    /// <remarks>
    ///     The read-side counterpart of <see cref="AddIndex" />, mirroring what
    ///     <see cref="GetElement{T}(int,int)" /> is to <see cref="SetElement{T}(int,int,T)" />. Winding
    ///     order lives in the indices, so without this it cannot be inspected or tested at all.
    /// </remarks>
    public int GetIndex(int indexPosition)
    {
        if (indexPosition < 0 || indexPosition >= IndexCount)
        {
            throw new ArgumentOutOfRangeException(nameof(indexPosition),
                $"Index position must be a value between 0 and {IndexCount - 1}");
        }

        ref var address = ref Unsafe.AddByteOffset(
            ref MemoryMarshal.GetArrayDataReference(_indexBuffer),
            (nuint)(indexPosition * _indexStride));

        return _indexStride == 2
            ? Unsafe.ReadUnaligned<short>(ref address)
            : Unsafe.ReadUnaligned<int>(ref address);
    }

    /// <summary>
    ///     Adds a new vertex
    /// </summary>
    /// <returns>
    ///     The vertex index (Can be used in <see cref="GetElement{T}(int,int)" /> and
    ///     <see cref="SetElement{T}(int,int,T)" />)
    /// </returns>
    public int AddVertex()
    {
        if ((VertexCount + 1) * _vertexStride <= _vertexBuffer.Length) return VertexCount++;

        return AddVertexWithResize();
    }

    /// <summary>
    ///     A more complex version of <see cref="AddVertex" /> including an array resize
    /// </summary>
    private int AddVertexWithResize()
    {
        var nextCapacity = Math.Max(MinCapacity * _vertexStride, _vertexBuffer.Length * 2);
        var nextBuffer = RentZeroed(nextCapacity, _vertexBuffer);

        _vertexBuffer = nextBuffer;

        return VertexCount++;
    }

    /// <summary>
    ///     Rents a buffer of at least the requested size, copies the old one into it, zeroes the
    ///     rest, and returns the old buffer to the pool.
    /// </summary>
    /// <remarks>
    ///     The zeroing is not cosmetic. Pooled arrays arrive holding whatever their previous renter
    ///     left in them, and two kinds of byte in the vertex buffer are never written afterwards:
    ///     the alignment padding between elements, and any element the caller forgets to set. Both
    ///     end up on the GPU, so without this they would be nondeterministic garbage instead of a
    ///     predictable zero.
    /// </remarks>
    private static byte[] RentZeroed(int capacity, byte[] previous)
    {
        var next = ArrayPool<byte>.Shared.Rent(capacity);

        if (previous.Length > 0)
        {
            Buffer.BlockCopy(previous, 0, next, 0, previous.Length);
            ArrayPool<byte>.Shared.Return(previous);
        }

        Array.Clear(next, previous.Length, next.Length - previous.Length);

        return next;
    }

    /// <summary>
    ///     Gets the value for the specified element index
    /// </summary>
    /// <remarks>
    ///     This overload always targets the last vertex and is a convenience version of
    ///     <see cref="GetElement{T}(int,int)" />
    /// </remarks>
    /// <param name="elementIndex">The element index</param>
    /// <typeparam name="T">The element type to retrieve</typeparam>
    /// <returns>The element at the element index</returns>
    /// <exception cref="ArgumentOutOfRangeException">The vertex index was outside the range of the currently added vertices</exception>
    /// <exception cref="ArgumentOutOfRangeException">The element index was outside the range of the currently added elements</exception>
    /// <exception cref="ArgumentException">The size of T does not match the type used when defining this element</exception>
    public T GetElement<T>(int elementIndex) where T : unmanaged => GetElementRef<T>(VertexCount - 1, elementIndex);

    /// <summary>
    ///     Gets the value for the specified element index
    /// </summary>
    /// <param name="vertexIndex">The vertex index</param>
    /// <param name="elementIndex">The element index</param>
    /// <typeparam name="T">The element type to retrieve</typeparam>
    /// <returns>The element at the element index</returns>
    /// <exception cref="ArgumentOutOfRangeException">The vertex index was outside the range of the currently added vertices</exception>
    /// <exception cref="ArgumentOutOfRangeException">The element index was outside the range of the currently added elements</exception>
    /// <exception cref="ArgumentException">The size of T does not match the type used when defining this element</exception>
    public T GetElement<T>(int vertexIndex, int elementIndex) where T : unmanaged =>
        GetElementRef<T>(vertexIndex, elementIndex);

    /// <summary>
    ///     Sets the value for the specified element index
    /// </summary>
    /// <remarks>
    ///     This overload always targets the last vertex and is a convenience version of
    ///     <see cref="SetElement{T}(int,int,T)" />
    /// </remarks>
    /// <param name="elementIndex">The element index</param>
    /// <param name="value">The value to set</param>
    /// <typeparam name="T">The element type to set</typeparam>
    /// <exception cref="ArgumentOutOfRangeException">The vertex index was outside the range of the currently added vertices</exception>
    /// <exception cref="ArgumentOutOfRangeException">The element index was outside the range of the currently added elements</exception>
    /// <exception cref="ArgumentException">The size of T does not match the type used when defining this element</exception>
    public void SetElement<T>(int elementIndex, T value) where T : unmanaged =>
        GetElementRef<T>(VertexCount - 1, elementIndex) = value;

    /// <summary>
    ///     Sets the value for the specified element index
    /// </summary>
    /// <param name="vertexIndex">The vertex index</param>
    /// <param name="elementIndex">The element index</param>
    /// <param name="value">The value to set</param>
    /// <typeparam name="T">The element type to set</typeparam>
    /// <exception cref="ArgumentOutOfRangeException">The vertex index was outside the range of the currently added vertices</exception>
    /// <exception cref="ArgumentOutOfRangeException">The element index was outside the range of the currently added elements</exception>
    /// <exception cref="ArgumentException">The size of T does not match the type used when defining this element</exception>
    public void SetElement<T>(int vertexIndex, int elementIndex, T value) where T : unmanaged =>
        GetElementRef<T>(vertexIndex, elementIndex) = value;

    /// <summary>
    ///     Base method to get or set element values in the type erased buffer
    /// </summary>
    private ref T GetElementRef<T>(int vertexIndex, int elementIndex) where T : unmanaged
    {
        // Both checks are >=. They were once >, and the off-by-one on the vertex index was the
        // dangerous half: index == VertexCount landed inside the pooled array's slack, so writing
        // one vertex past the end corrupted nothing visibly and threw nothing.
        if (elementIndex < 0 || elementIndex >= _vertexElements.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(elementIndex),
                $"Element index must be a value between 0 and {_vertexElements.Count - 1}");
        }

        if (vertexIndex < 0 || vertexIndex >= VertexCount)
        {
            throw new ArgumentOutOfRangeException(nameof(vertexIndex),
                $"Vertex index must be a value between 0 and {VertexCount - 1}");
        }

        var element = _vertexElements[elementIndex];
        if (element.Size != Unsafe.SizeOf<T>())
        {
            throw new ArgumentException(
                $"Value has a size of {Unsafe.SizeOf<T>()}, but was defined with a size of {element.Size}", nameof(T));
        }

        var elementOffset = vertexIndex * _vertexStride + element.Offset;

        return ref Unsafe.As<byte, T>(ref Unsafe.AddByteOffset(ref MemoryMarshal.GetArrayDataReference(_vertexBuffer),
            (nuint)elementOffset));
    }

    /// <summary>
    ///     Creates a new mesh draw instance
    /// </summary>
    /// <param name="device">The graphics device</param>
    /// <param name="clear">Determines if the mesh builder should be reset after this call</param>
    /// <returns>A mesh draw instance</returns>
    /// <exception cref="ArgumentNullException">The graphics device is null</exception>
    /// <exception cref="InvalidOperationException">The primitive type was not set to a valid value</exception>
    /// <exception cref="InvalidOperationException">No vertices were added, or indexing was configured but no indices were added</exception>
    /// <remarks>
    ///     The caller owns the two GPU buffers this creates. They are not tracked by any content
    ///     manager, so rebuilding a mesh repeatedly without disposing the previous draw's buffers
    ///     leaks GPU memory.
    /// </remarks>
    public MeshDraw ToMeshDraw(GraphicsDevice device, bool clear = true)
    {
        ArgumentNullException.ThrowIfNull(device);

        if (PrimitiveType == PrimitiveType.Undefined)
            throw new InvalidOperationException("A primitive type must be set");

        // Both guards exist because Direct3D rejects zero-sized buffers with E_INVALIDARG, which
        // surfaces as an unexplained failure deep inside Buffer.New rather than as anything naming
        // the actual mistake
        if (VertexCount == 0)
            throw new InvalidOperationException("Cannot create a mesh with no vertices");

        if (_indexStride != 0 && IndexCount == 0)
        {
            throw new InvalidOperationException(
                $"Indexing type {IndexType} was configured but no indices were added. Add indices, or use {nameof(IndexingType)}.{nameof(IndexingType.None)} for a non-indexed mesh");
        }

        // The backing arrays come from the pool, so they are almost always larger than the data
        // they hold; slicing keeps the garbage tail off the GPU. Instance count is zero because
        // this is not instanced geometry - it once carried VertexCount, which poisoned the
        // declaration's precomputed hash and defeated declaration sharing across meshes.
        var vertexBuffer = Stride.Graphics.Buffer.New(
            device,
            (ReadOnlySpan<byte>)_vertexBuffer.AsSpan(0, VertexCount * _vertexStride),
            _vertexStride,
            BufferFlags.VertexBuffer);

        var vertexDeclaration = new VertexDeclaration(
            [.. _vertexElements.Select(v => v.VertexElement)],
            instanceCount: 0,
            vertexStride: _vertexStride);

        var draw = new MeshDraw
        {
            VertexBuffers = [new VertexBufferBinding(vertexBuffer, vertexDeclaration, VertexCount, _vertexStride)],
            PrimitiveType = PrimitiveType,
            StartLocation = 0
        };

        if (_indexStride == 0)
        {
            // Non-indexed: the engine's mesh render path draws DrawCount vertices directly when
            // there is no index buffer
            draw.DrawCount = VertexCount;
        }
        else
        {
            var indexBuffer = Stride.Graphics.Buffer.New(
                device,
                (ReadOnlySpan<byte>)_indexBuffer.AsSpan(0, IndexCount * _indexStride),
                _indexStride,
                BufferFlags.IndexBuffer);

            draw.IndexBuffer = new IndexBufferBinding(indexBuffer, _indexStride == Unsafe.SizeOf<int>(), IndexCount);
            draw.DrawCount = IndexCount;
        }

        if (clear) Clear();

        return draw;
    }

    /// <summary>
    ///     Clears all buffers and elements configured in this instance
    /// </summary>
    public void Clear()
    {
        _vertexElements.Clear();

        if (_vertexBuffer.Length > 0) ArrayPool<byte>.Shared.Return(_vertexBuffer);
        _vertexBuffer = [];
        VertexCount = 0;
        _vertexStride = 0;

        if (_indexBuffer.Length > 0) ArrayPool<byte>.Shared.Return(_indexBuffer);
        _indexBuffer = [];
        IndexCount = 0;
        _indexStride = 0;
    }
}