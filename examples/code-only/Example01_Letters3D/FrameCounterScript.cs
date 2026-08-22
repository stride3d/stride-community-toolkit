using Stride.CommunityToolkit.Rendering.Utilities;
using Stride.Engine;
using Stride.Rendering;

namespace Example01_Letters3D;

/// <summary>
/// Shows a number as solid 3D digits, rebuilding the mesh whenever the number changes.
/// </summary>
/// <remarks>
/// This is the worst case for mesh text - a value that changes every single frame - and it is here
/// on purpose, because it makes the one rule of rebuilding impossible to miss: release the old
/// buffers first. <c>CreateTextMeshDraw</c> hands the caller GPU buffers that no content manager
/// tracks, so swapping in a new model without disposing the old one leaks a buffer pair per rebuild
/// - at hundreds of frames a second, megabytes per second. For a HUD counter in a real game, prefer
/// <c>EntityTextComponent</c>, which redraws a font instead of rebuilding geometry.
/// </remarks>
public class FrameCounterScript : SyncScript
{
    private readonly ModelComponent _modelComponent = new();
    private int _frames;
    private string _shown = string.Empty;

    /// <summary>Gets the material the digits are drawn with.</summary>
    public required Material Material { get; init; }

    /// <inheritdoc />
    public override void Start() => Entity.Add(_modelComponent);

    /// <inheritdoc />
    public override void Update()
    {
        _frames++;

        var text = _frames.ToString();

        // A frame counter changes every update, but this guard is the pattern to keep: a score or a
        // seconds counter holds the same text for many frames, and those frames should cost nothing
        if (text == _shown) return;

        _shown = text;

        ReleaseMeshBuffers();

        _modelComponent.Model = new Model
        {
            new MaterialInstance { Material = Material },
            new Mesh
            {
                Draw = LetterMeshFactory.CreateTextMeshDraw(GraphicsDevice, text, centerOrigin: true),
                MaterialIndex = 0
            }
        };
    }

    // Disposes the GPU buffers behind the current model. Only for meshes this script built itself -
    // content-manager-loaded models manage their own buffers.
    private void ReleaseMeshBuffers()
    {
        if (_modelComponent.Model is not { } model) return;

        foreach (var mesh in model.Meshes)
        {
            foreach (var vertexBuffer in mesh.Draw.VertexBuffers)
            {
                vertexBuffer.Buffer.Dispose();
            }

            mesh.Draw.IndexBuffer?.Buffer.Dispose();
        }
    }
}