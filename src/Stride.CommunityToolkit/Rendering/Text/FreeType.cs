using System.Runtime.InteropServices;

namespace Stride.CommunityToolkit.Rendering.Text;

/// <summary>
/// The handful of FreeType calls <see cref="MeshFont"/> needs, bound to the <c>freetype</c> native library
/// that <c>Stride.Graphics</c> ships for every platform. The engine binds the same library for its sprite
/// fonts, but keeps that binding internal, so these are declared again here; the struct layouts mirror
/// <c>Stride.Graphics/Font/FreeTypeNative.cs</c> field for field, which is why the two record structs
/// have as many fields as the C structs they stand for.
/// </summary>
// TODO: Ask upstream for a public outline reader, FontSystem.TryGetGlyphOutline(fontName, codepoint, out contours)
// or the like. The engine already binds FreeType for its sprite fonts; a public reader would make this
// second binding unnecessary and keep the struct layouts in one place. Non-breaking for the engine.
// Until then this file must track Stride.Graphics/Font/FreeTypeNative.cs field for field.
internal static unsafe partial class FreeType
{
    private const string LibraryName = "freetype";

    /// <summary>Outline coordinates in font units, no hinting, no bitmap: <c>FT_LOAD_NO_SCALE | NO_HINTING | NO_BITMAP | IGNORE_TRANSFORM</c>.</summary>
    internal const int LoadOutlineFlags = 0x1 | 0x2 | 0x8 | 0x800;

    /// <summary><c>FT_KERNING_UNSCALED</c>: kerning in font units.</summary>
    internal const uint KerningUnscaled = 2;

    /// <summary>The low two bits of an outline point's tag: <c>FT_CURVE_TAG_CONIC</c>.</summary>
    internal const byte TagConic = 0;

    /// <summary><c>FT_CURVE_TAG_ON</c>.</summary>
    internal const byte TagOn = 1;

    /// <summary><c>FT_CURVE_TAG_CUBIC</c>.</summary>
    internal const byte TagCubic = 2;

    [LibraryImport(LibraryName, EntryPoint = "FT_Init_FreeType")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int InitFreeType(out nint library);

    [LibraryImport(LibraryName, EntryPoint = "FT_New_Memory_Face")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int NewMemoryFace(nint library, byte* fileBase, CLong fileSize, CLong faceIndex, out FaceRec* face);

    [LibraryImport(LibraryName, EntryPoint = "FT_Done_Face")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int DoneFace(FaceRec* face);

    [LibraryImport(LibraryName, EntryPoint = "FT_Get_Char_Index")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial uint GetCharIndex(FaceRec* face, uint charCode);

    [LibraryImport(LibraryName, EntryPoint = "FT_Load_Glyph")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int LoadGlyph(FaceRec* face, uint glyphIndex, int loadFlags);

    [LibraryImport(LibraryName, EntryPoint = "FT_Get_Kerning")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int GetKerning(FaceRec* face, uint leftGlyph, uint rightGlyph, uint kernMode, out Vector kerning);

    [StructLayout(LayoutKind.Sequential)]
    internal struct Vector
    {
        public CLong X;
        public CLong Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct GlyphMetrics
    {
        public CLong Width;
        public CLong Height;
        public CLong HoriBearingX;
        public CLong HoriBearingY;
        public CLong HoriAdvance;
        public CLong VertBearingX;
        public CLong VertBearingY;
        public CLong VertAdvance;
    }

    /// <summary>
    /// <c>FT_Bitmap</c>. Never read, but kept as a struct: it holds pointers, so the C layout aligns it to a
    /// pointer after the 4-byte field before it, and writing its fields out would lose that padding.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct Bitmap
    {
        public uint Rows;
        public uint Width;
        public int Pitch;
        public byte* Buffer;
        public ushort NumGrays;
        public byte PixelMode;
        public byte PaletteMode;
        public void* Palette;
    }

    /// <summary>A glyph's outline: points in font units, a tag per point, and the index of each contour's last point.</summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct Outline
    {
        public ushort NumContours;
        public ushort NumPoints;
        public Vector* Points;
        public byte* Tags;
        public ushort* Contours;
        public int Flags;
    }

    /// <summary>
    /// <c>FT_GlyphSlotRec</c>, every field, so <see cref="Outline"/> and <see cref="Metrics"/> sit at their C offsets;
    /// the nested <c>FT_Generic</c> is written out as its two fields.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct GlyphSlotRec
    {
        public nint FreeTypeLibrary;
        public nint Face;
        public GlyphSlotRec* Next;
        public uint GlyphIndex;
        public nint GenericData;
        public nint GenericFinalizer;
        public GlyphMetrics Metrics;
        public CLong LinearHoriAdvance;
        public CLong LinearVertAdvance;
        public Vector Advance;
        public uint Format;
        public Bitmap Bitmap;
        public int BitmapLeft;
        public int BitmapTop;
        public Outline Outline;
        public uint NumSubglyphs;
        public nint Subglyphs;
        public void* ControlData;
        public CLong ControlLen;
        public CLong LsbDelta;
        public CLong RsbDelta;
        public void* Other;
        public nint Internal;
    }

    /// <summary>
    /// The public head of <c>FT_FaceRec</c>, every field up to the glyph slot; the private fields that follow are never
    /// read, and the nested <c>FT_Generic</c> and <c>FT_BBox</c> are written out as their fields.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct FaceRec
    {
        public CLong NumFaces;
        public CLong FaceIndex;
        public CLong FaceFlags;
        public CLong StyleFlags;
        public CLong NumGlyphs;
        public byte* FamilyName;
        public byte* StyleName;
        public int NumFixedSizes;
        public nint AvailableSizes;
        public int NumCharmaps;
        public nint Charmaps;
        public nint GenericData;
        public nint GenericFinalizer;
        public CLong BboxXMin;
        public CLong BboxYMin;
        public CLong BboxXMax;
        public CLong BboxYMax;
        public ushort UnitsPerEm;
        public short Ascender;
        public short Descender;
        public short Height;
        public short MaxAdvanceWidth;
        public short MaxAdvanceHeight;
        public short UnderlinePosition;
        public short UnderlineThickness;
        public GlyphSlotRec* Glyph;
        public nint Size;
        public nint Charmap;
    }
}