using System.Runtime.InteropServices;
using Stride.Graphics.Font;

namespace Stride.CommunityToolkit.Rendering.Text;

/// <summary>
/// A font file opened for its glyph outlines: the vector shapes that solid 3D lettering is built from.
/// </summary>
/// <remarks>
/// <para>
/// Stride's sprite fonts rasterise glyphs; this reads the same files for their curves instead, through
/// the FreeType library the engine ships on every platform, so TrueType, OpenType (CFF) and font
/// collections all work. <see cref="Default"/> is an Open Sans Regular embedded in the toolkit, so
/// lettering works with no font file at hand; <see cref="Load(string)"/> opens any file, and
/// <see cref="FromFamily"/> finds an installed family through <see cref="SystemFonts"/>.
/// </para>
/// <para>
/// Outlines come back in EM units, baseline at y = 0, pen at x = 0, y up: a capital letter is about 0.7
/// high and a line of text is <see cref="LineHeight"/> apart. Scale them to the size wanted. Glyphs are
/// read once and cached; a font is safe to share between threads.
/// </para>
/// <para>
/// Kerning is read from the font's legacy kern table only, which modern fonts no longer carry (they keep
/// it in GPOS, which FreeType does not read without a shaping engine), so <see cref="GetKerning"/> is
/// 0 for most fonts, including <see cref="Default"/>.
/// </para>
/// </remarks>
public sealed unsafe class MeshFont : IDisposable
{
    private const string DefaultResourceName = "Stride.CommunityToolkit.Rendering.Text.Fonts.OpenSans-Regular.ttf";

    private static readonly Lock LibraryLock = new();
    private static readonly Lazy<nint> Library = new(InitialiseLibrary);
    private static readonly Lazy<MeshFont> DefaultFont = new(LoadDefault);

    private readonly Lock _faceLock = new();
    private readonly Dictionary<int, GlyphOutline> _glyphs = [];
    private readonly bool _isShared;
    private readonly float _scale;
    private GCHandle _data;
    private FreeType.FaceRec* _face;

    private MeshFont(byte[] data, string? name, bool isShared)
    {
        _isShared = isShared;
        _data = GCHandle.Alloc(data, GCHandleType.Pinned);

        try
        {
            lock (LibraryLock)
            {
                var error = FreeType.NewMemoryFace(Library.Value, (byte*)_data.AddrOfPinnedObject(), new CLong(data.Length), new CLong(0), out _face);

                if (error != 0) throw new InvalidDataException($"FreeType could not open the font{(name is null ? "" : $" '{name}'")} (error {error}). Is it a TrueType, OpenType or font collection file?");
            }

            if (_face->UnitsPerEm == 0) throw new InvalidDataException("The font has no outlines: its units per EM is zero. Bitmap-only fonts have no shapes to extrude.");

            _scale = 1f / _face->UnitsPerEm;

            var family = Marshal.PtrToStringUTF8((nint)_face->FamilyName) ?? name ?? "Font";
            var style = Marshal.PtrToStringUTF8((nint)_face->StyleName);

            Name = string.IsNullOrEmpty(style) || style == "Regular" ? family : $"{family} {style}";
            UnitsPerEm = _face->UnitsPerEm;
            Ascent = _face->Ascender * _scale;
            Descent = _face->Descender * _scale;
            LineHeight = _face->Height * _scale;
        }
        catch
        {
            Release();
            throw;
        }
    }

    /// <summary>Closes the native face when the font was never disposed.</summary>
    ~MeshFont() => Release();

    /// <summary>
    /// The font embedded in the toolkit, Open Sans Regular (Apache 2.0), so lettering works with no font
    /// file at hand. Shared: disposing it does nothing.
    /// </summary>
    public static MeshFont Default => DefaultFont.Value;

    /// <summary>The font's family and style, as the file names them: "Open Sans", "Segoe UI Bold".</summary>
    public string Name { get; }

    /// <summary>The font's design grid: how many of its own units make one EM. Outlines are already divided by it.</summary>
    public int UnitsPerEm { get; }

    /// <summary>How far above the baseline the font reaches, in EM units.</summary>
    public float Ascent { get; }

    /// <summary>How far below the baseline the font reaches, in EM units; negative.</summary>
    public float Descent { get; }

    /// <summary>The distance between two baselines, in EM units.</summary>
    public float LineHeight { get; }

    /// <summary>
    /// Opens a font file.
    /// </summary>
    /// <param name="path">The path of a .ttf, .otf or .ttc file.</param>
    /// <returns>The font; dispose it when its outlines are no longer needed.</returns>
    /// <exception cref="ArgumentNullException">The path is null.</exception>
    /// <exception cref="FileNotFoundException">There is no file at the path.</exception>
    /// <exception cref="InvalidDataException">The file is not a font FreeType can open, or has no outlines.</exception>
    public static MeshFont Load(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        return new MeshFont(File.ReadAllBytes(path), Path.GetFileNameWithoutExtension(path), isShared: false);
    }

    /// <summary>
    /// Opens a font from its bytes: a file read elsewhere, an embedded resource, a download.
    /// </summary>
    /// <param name="data">The whole font file. It is pinned for the font's lifetime, so do not change it.</param>
    /// <param name="name">A name for error messages; the font's own family name is read from the file.</param>
    /// <returns>The font; dispose it when its outlines are no longer needed.</returns>
    /// <exception cref="ArgumentNullException">The data is null.</exception>
    /// <exception cref="InvalidDataException">The bytes are not a font FreeType can open, or the font has no outlines.</exception>
    public static MeshFont Load(byte[] data, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(data);

        return new MeshFont(data, name, isShared: false);
    }

    /// <summary>
    /// Opens an installed font by family name, found through <see cref="SystemFonts.FindFile"/>.
    /// </summary>
    /// <param name="family">The family: "Segoe UI", "Arial", "DejaVu Sans".</param>
    /// <param name="style">The weight and slant wanted.</param>
    /// <returns>The font, or <see langword="null"/> when no file of that family and style is installed.</returns>
    /// <exception cref="ArgumentNullException">The family is null.</exception>
    public static MeshFont? FromFamily(string family, FontStyle style = FontStyle.Regular)
    {
        ArgumentNullException.ThrowIfNull(family);

        var path = SystemFonts.FindFile(family, style);

        return path is null ? null : Load(path);
    }

    /// <summary>Whether the font has a glyph of its own for the code point, rather than only its "missing glyph" box.</summary>
    public bool HasGlyph(int codepoint)
    {
        ThrowIfDisposed();

        lock (_faceLock)
        {
            return FreeType.GetCharIndex(_face, (uint)codepoint) != 0;
        }
    }

    /// <summary>
    /// The outline of a character.
    /// </summary>
    /// <param name="character">The character.</param>
    /// <returns>Its outline; the font's "missing glyph" shape when it has none, see <see cref="GlyphOutline.IsMissing"/>.</returns>
    public GlyphOutline GetGlyph(char character) => GetGlyph((int)character);

    /// <summary>
    /// The outline of a code point, which covers the characters beyond the basic plane that a <see cref="char"/> cannot.
    /// </summary>
    /// <param name="codepoint">The Unicode code point.</param>
    /// <returns>Its outline; the font's "missing glyph" shape when it has none, see <see cref="GlyphOutline.IsMissing"/>.</returns>
    /// <exception cref="InvalidDataException">The font's outline for the glyph breaks FreeType's conventions.</exception>
    public GlyphOutline GetGlyph(int codepoint)
    {
        ThrowIfDisposed();

        lock (_faceLock)
        {
            if (_glyphs.TryGetValue(codepoint, out var cached)) return cached;

            var outline = ReadGlyph(codepoint);

            _glyphs[codepoint] = outline;

            return outline;
        }
    }

    /// <summary>
    /// How much closer the pen moves for a pair of characters, from the font's kern table: negative pulls
    /// them together (AV), positive pushes them apart. 0 for most modern fonts, see the remarks on the class.
    /// </summary>
    /// <param name="left">The code point on the left.</param>
    /// <param name="right">The code point on the right.</param>
    /// <returns>The adjustment to the left glyph's advance, in EM units.</returns>
    public float GetKerning(int left, int right)
    {
        ThrowIfDisposed();

        lock (_faceLock)
        {
            var leftIndex = FreeType.GetCharIndex(_face, (uint)left);
            var rightIndex = FreeType.GetCharIndex(_face, (uint)right);

            if (leftIndex == 0 || rightIndex == 0) return 0f;

            var error = FreeType.GetKerning(_face, leftIndex, rightIndex, FreeType.KerningUnscaled, out var kerning);

            return error == 0 ? (float)kerning.X.Value * _scale : 0f;
        }
    }

    /// <summary>Closes the font. <see cref="Default"/> stays open.</summary>
    public void Dispose()
    {
        if (_isShared) return;

        Release();
        GC.SuppressFinalize(this);
    }

    private GlyphOutline ReadGlyph(int codepoint)
    {
        var glyphIndex = FreeType.GetCharIndex(_face, (uint)codepoint);
        var error = FreeType.LoadGlyph(_face, glyphIndex, FreeType.LoadOutlineFlags);

        if (error != 0) throw new InvalidDataException($"FreeType could not load glyph {glyphIndex} of '{Name}' for U+{codepoint:X4} (error {error}).");

        var slot = _face->Glyph;
        var outline = slot->Outline;
        var points = new Vector2[outline.NumPoints];
        var tags = new byte[outline.NumPoints];
        var ends = new ushort[outline.NumContours];

        for (var i = 0; i < outline.NumPoints; i++)
        {
            points[i] = new Vector2((float)outline.Points[i].X.Value * _scale, (float)outline.Points[i].Y.Value * _scale);
            tags[i] = outline.Tags[i];
        }

        for (var i = 0; i < outline.NumContours; i++) ends[i] = outline.Contours[i];

        var contours = OutlineDecoder.Decode(points, tags, ends);
        var metrics = slot->Metrics;
        var advance = (float)metrics.HoriAdvance.Value * _scale;

        if (contours.Length == 0) return new GlyphOutline(codepoint, glyphIndex, advance, Vector2.Zero, Vector2.Zero, contours);

        var bearingX = (float)metrics.HoriBearingX.Value * _scale;
        var bearingY = (float)metrics.HoriBearingY.Value * _scale;
        var width = (float)metrics.Width.Value * _scale;
        var height = (float)metrics.Height.Value * _scale;

        return new GlyphOutline(codepoint, glyphIndex, advance, new Vector2(bearingX, bearingY - height), new Vector2(bearingX + width, bearingY), contours);
    }

    private void Release()
    {
        if (_face is not null)
        {
            lock (LibraryLock)
            {
                FreeType.DoneFace(_face);
            }

            _face = null;
        }

        if (_data.IsAllocated) _data.Free();
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_face is null, this);

    private static MeshFont LoadDefault()
    {
        using var stream = typeof(MeshFont).Assembly.GetManifestResourceStream(DefaultResourceName)
            ?? throw new InvalidOperationException($"The embedded font '{DefaultResourceName}' is missing from the toolkit assembly.");

        var data = new byte[stream.Length];

        stream.ReadExactly(data);

        return new MeshFont(data, "Open Sans", isShared: true);
    }

    /// <summary>The one FreeType library object, created on first use; the native library is the engine's own copy.</summary>
    private static nint InitialiseLibrary()
    {
        // Finds freetype beside the assembly or under runtimes/<rid>/native, and registers the resolver that
        // makes a bare "freetype" DllImport find it on Linux; the engine's FontManager does the same
        NativeLibraryHelper.PreloadLibrary("freetype", typeof(MeshFont));

        var error = FreeType.InitFreeType(out var library);

        if (error != 0) throw new InvalidOperationException($"FreeType could not be initialised (error {error}).");

        return library;
    }
}