using Stride.Graphics;

namespace Stride.CommunityToolkit.Rendering.Text.Meshes;

/// <summary>
/// One glyph of a string as its own mesh, centred on itself, with where it belongs in the text: what
/// <see cref="TextMesh.CreateGlyphs"/> returns, for one body or one animation per letter.
/// </summary>
/// <param name="Codepoint">The character the glyph draws.</param>
/// <param name="Position">The centre of the glyph's bounds within the text, in world units; put the entity there.</param>
/// <param name="Mesh">The glyph as a solid, centred on the origin.</param>
public sealed record GlyphMesh(int Codepoint, Vector3 Position, GeometricMeshData<VertexPositionNormalTexture> Mesh);