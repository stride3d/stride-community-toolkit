using Stride.Rendering;
using Stride.Shaders;

namespace Stride.CommunityToolkit.Rendering;

/// <summary>
/// Draws every mesh with one material stream as its colour, the way Game Studio's view modes do:
/// the diffuse, the glossiness, the normals. The quickest way to see why a material looks wrong,
/// since a normal map loaded as sRGB or a glossiness map read from the wrong channel is obvious as
/// a colour and invisible under lighting. Added by <c>game.AddMaterialStreamView()</c>.
/// </summary>
/// <remarks>
/// <para>
/// The mechanism is the forward effect's <c>PixelStageSurfaceFilter</c> hook, a permutation the
/// editor fills with the engine's <c>MaterialSurfaceStreamShading</c> shader: it runs the material's
/// surface shading and returns one stream instead of the lit colour. The two shaders come with the
/// engine; the toolkit adds the <see cref="MaterialStreamRenderFeature"/> that sets the hook.
/// </para>
/// <para>
/// The value shown is the linear value the stream holds, drawn through the post effects like any
/// colour, so it is tone-mapped the way the lit picture is. The view applies to every mesh the
/// compositor's mesh feature draws; it is not a per-material setting.
/// </para>
/// </remarks>
public sealed class MaterialStreamView : IDisposable
{
    private readonly MeshRenderFeature _meshFeature;
    private readonly MaterialStreamRenderFeature _feature;
    private MaterialStream? _stream;

    /// <summary>Every stream, in the order <see cref="Next"/> walks them.</summary>
    public static IReadOnlyList<MaterialStream> All { get; } = Enum.GetValues<MaterialStream>();

    internal MaterialStreamView(MeshRenderFeature meshFeature, MaterialStreamRenderFeature feature)
    {
        _meshFeature = meshFeature;
        _feature = feature;
    }

    /// <summary>The stream every mesh draws, or <see langword="null"/> for lit shading. Takes effect on the next frame, after a recompile.</summary>
    public MaterialStream? Stream
    {
        get => _stream;
        set
        {
            _stream = value;
            _feature.Filter = value is { } stream ? FilterFor(stream) : null;
        }
    }

    /// <summary>What the view shows, for an overlay: the stream's name, or "Lit" when none is set.</summary>
    public string Name => _stream is { } stream ? DisplayName(stream) : "Lit";

    /// <summary>Steps to the next stream, and after the last back to lit shading.</summary>
    public void Next()
    {
        if (_stream is not { } stream)
        {
            Stream = All[0];
            return;
        }

        // The enum values are 0..n-1 in declaration order, which is the order All holds them
        var index = (int)stream + 1;

        Stream = index < All.Count ? All[index] : null;
    }

    /// <summary>Takes the view's feature out of the compositor. Meshes recompile back to lit shading on the next frame.</summary>
    public void Dispose()
    {
        Stream = null;
        _meshFeature.RenderFeatures.Remove(_feature);
    }

    /// <summary>The name Game Studio's toolbar gives the stream.</summary>
    public static string DisplayName(MaterialStream stream) => stream switch
    {
        MaterialStream.Diffuse => "Diffuse",
        MaterialStream.ColorBase => "Color base",
        MaterialStream.Specular => "Specular",
        MaterialStream.Glossiness => "Glossiness",
        MaterialStream.NormalTangent => "Normal (tangent)",
        MaterialStream.NormalWorld => "Normal (world)",
        MaterialStream.Occlusion => "Occlusion",
        MaterialStream.Cavity => "Cavity",
        MaterialStream.Emissive => "Emissive",
        _ => throw new ArgumentOutOfRangeException(nameof(stream), stream, "Not a material stream"),
    };

    /// <summary>
    /// The shader the hook is filled with for a stream: <c>MaterialSurfaceStreamShading</c> with the
    /// stream's name, the channels to show (<c>rgb</c> for a colour, <c>rrr</c> for a number) and
    /// whether to remap a signed value into 0..1; the world normal has a shader of its own.
    /// </summary>
    public static ShaderClassSource FilterFor(MaterialStream stream) => stream switch
    {
        MaterialStream.Diffuse => Filter("matDiffuse", "rgb"),
        MaterialStream.ColorBase => Filter("matColorBase", "rgb"),
        MaterialStream.Specular => Filter("matSpecular", "rgb"),
        MaterialStream.Glossiness => Filter("matGlossiness", "rrr"),
        MaterialStream.NormalTangent => Filter("matNormal", "rgb", remapSigned: true),
        MaterialStream.NormalWorld => new ShaderClassSource("MaterialSurfaceNormalStreamShading"),
        MaterialStream.Occlusion => Filter("matAmbientOcclusion", "rrr"),
        MaterialStream.Cavity => Filter("matCavity", "rrr"),
        MaterialStream.Emissive => Filter("matEmissive", "rgb"),
        _ => throw new ArgumentOutOfRangeException(nameof(stream), stream, "Not a material stream"),
    };

    private static ShaderClassSource Filter(string streamName, string channels, bool remapSigned = false)
        => new("MaterialSurfaceStreamShading", streamName, channels, remapSigned);
}