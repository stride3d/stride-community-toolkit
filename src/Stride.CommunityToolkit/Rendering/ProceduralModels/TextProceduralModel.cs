using Stride.CommunityToolkit.Rendering.Text.Meshes;
using Stride.Graphics;
using Stride.Rendering.ProceduralModels;

namespace Stride.CommunityToolkit.Rendering.ProceduralModels;

/// <summary>
/// A procedural model of solid lettering: <see cref="Text"/> in a <see cref="Font"/>, built by
/// <see cref="TextMesh"/>. What <c>Primitive3D.Text</c> turns into.
/// </summary>
/// <remarks>
/// The font and the options are runtime objects, not asset data, so this model is for code; Game
/// Studio cannot serialise it.
/// </remarks>
public class TextProceduralModel : PrimitiveProceduralModelBase
{
    /// <summary>The text; <c>\n</c> starts a new line.</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>The font; <see cref="MeshFont.Default"/> when <see langword="null"/>.</summary>
    [DataMemberIgnore]
    public MeshFont? Font { get; set; }

    /// <summary>Size, depth and layout.</summary>
    [DataMemberIgnore]
    public TextMeshOptions Options { get; set; } = new();

    /// <inheritdoc />
    protected override GeometricMeshData<VertexPositionNormalTexture> CreatePrimitiveMeshData()
        => TextMesh.Create(Font ?? MeshFont.Default, Text, Options);
}