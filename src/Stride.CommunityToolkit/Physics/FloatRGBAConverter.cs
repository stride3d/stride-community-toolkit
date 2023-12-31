using System.Runtime.InteropServices;

namespace Stride.CommunityToolkit.Physics;

public static partial class HeightmapExtensions
{
    /// <summary>
    /// Float from bytes and back, like the Union structure from c++
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct FloatRGBAConverter
    {
        [FieldOffset(0)]
        public float Float;

        [FieldOffset(0)]
        public byte R;

        [FieldOffset(1)]
        public byte G;

        [FieldOffset(2)]
        public byte B;

        [FieldOffset(3)]
        public byte A;

        public FloatRGBAConverter(float @float) : this()
        {
            Float = @float;
        }

        public FloatRGBAConverter(byte r, byte g, byte b, byte a) : this()
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }
    }
}