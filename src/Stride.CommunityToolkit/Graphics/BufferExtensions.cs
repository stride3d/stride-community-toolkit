using Stride.Graphics;
using Buffer = Stride.Graphics.Buffer;

namespace Stride.CommunityToolkit.Graphics;

/// <summary>
/// Provides extension methods for <see cref="Buffer"/>.
/// </summary>
public static class BufferExtensions
{
    /// <summary>
    /// Uploads a span to the buffer with the span's memory pinned for the duration of the call.
    /// </summary>
    /// <typeparam name="T">The element type of the data.</typeparam>
    /// <param name="buffer">The buffer to write to.</param>
    /// <param name="commandList">The command list that performs the upload.</param>
    /// <param name="data">The data to upload. An empty span uploads nothing.</param>
    /// <param name="offsetInBytes">Where in the buffer the data is written. Supported for buffers of default usage only, as for <see cref="Buffer.SetData{TData}(CommandList, ReadOnlySpan{TData}, int)"/>.</param>
    /// <remarks>
    /// Use it for a span over managed memory, such as an array or a <see cref="List{T}"/>. On
    /// Direct3D 11 the engine's upload of a default-usage buffer hands the span's address to native
    /// code without pinning it, in Stride 4.4.0 and earlier. A
    /// garbage collection during that call may move the array, and the upload then reads the wrong
    /// memory. The other backends pin the span themselves, where pinning again costs nothing.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="buffer"/> or <paramref name="commandList"/> is <see langword="null"/>.</exception>
    public static unsafe void SetDataPinned<T>(this Buffer buffer, CommandList commandList, ReadOnlySpan<T> data, int offsetInBytes = 0) where T : unmanaged
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentNullException.ThrowIfNull(commandList);

        if (data.IsEmpty) return;

        fixed (T* pinned = data)
        {
            buffer.SetData(commandList, data, offsetInBytes);
        }
    }
}