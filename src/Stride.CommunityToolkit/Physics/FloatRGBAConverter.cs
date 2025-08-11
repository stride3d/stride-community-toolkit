using System.Runtime.InteropServices;

namespace Stride.CommunityToolkit.Physics;

public static partial class HeightmapExtensions
{
    /// <summary>
    /// Reinterprets a 32-bit float and its 4 constituent bytes (RGBA) similar to a C-style union.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct FloatRGBAConverter
    {
        /// <summary>The floating point value view.</summary>
        [FieldOffset(0)] public float Float;

        /// <summary>Red channel (least significant byte of <see cref="Float"/>).</summary>
        [FieldOffset(0)] public byte R;

        /// <summary>Green channel.</summary>
        [FieldOffset(1)] public byte G;

        /// <summary>Blue channel.</summary>
        [FieldOffset(2)] public byte B;

        /// <summary>Alpha channel (most significant byte of <see cref="Float"/>).</summary>
        [FieldOffset(3)] public byte A;

        /// <summary>
        /// Initializes the union with a floating point value.
        /// </summary>
        public FloatRGBAConverter(float @float) : this()
        {
            Float = @float;
        }

        /// <summary>
        /// Initializes the union with explicit channel bytes.
        /// </summary>
        public FloatRGBAConverter(byte r, byte g, byte b, byte a) : this()
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }
    }
}