using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using Buffer = Stride.Graphics.Buffer;

namespace Example18_Box2DPhysics;

/// <summary>
/// Render feature that applies an outline effect to meshes using the MeshOutlineShader.
/// </summary>
/// <remarks>
/// Only entities with <see cref="MeshOutlineComponent"/> and matching <see cref="RenderGroupMask"/> will be outlined.
/// Ensure this feature is enabled in the Graphics Compositor.
/// </remarks>
public class SDFPerimeterOutline2DShaderRenderFeature : RootRenderFeature
{
    private DynamicEffectInstance? _shader;
    private MutablePipelineState? _pipelineState;

    // Cache for vertex buffers to prevent memory leaks
    private readonly Dictionary<int, Buffer> _vertexBufferCache = [];

    /// <summary>
    /// Default sort key for outline rendering.
    /// </summary>
    public const int DefaultSortKey = 255;

    /// <summary>
    /// Adjusts the scale of the model for outline thickness.
    /// </summary>
    [DataMember(10)]
    [DataMemberRange(0.0f, 0.1f, 0.001f, 0.002f, 4)]
    public float ScaleAdjust = 0.001f;

    /// <summary>
    /// Specifies which render groups will have outlines applied.
    /// </summary>
    [DataMember(5)]
    public RenderGroupMask RenderGroupMask;

    /// <inheritdoc/>
    public override Type SupportedRenderObjectType => typeof(RenderMesh);

    /// <summary>
    /// Initializes a new instance of the <see cref="SDFPerimeterOutline2DShaderRenderFeature"/> class.
    /// </summary>
    public SDFPerimeterOutline2DShaderRenderFeature() => SortKey = DefaultSortKey;

    /// <summary>
    /// Initializes the shader and pipeline state for outline rendering.
    /// </summary>
    protected override void InitializeCore()
    {
        base.InitializeCore();

        try
        {
            _shader = new DynamicEffectInstance("SDFPerimeterOutline2DShader");
            _shader.Initialize(Context.Services);

            _pipelineState = new MutablePipelineState(Context.GraphicsDevice);
            _pipelineState.State.SetDefaults();
            _pipelineState.State.InputElements = VertexPositionNormalTexture.Layout.CreateInputElements();
            //_pipelineState.State.BlendState = BlendStates.AlphaBlend;
            _pipelineState.State.BlendState = BlendStates.Opaque; // solid crisp border
            _pipelineState.State.RasterizerState.CullMode = CullMode.Back;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to initialize MeshOutlineRenderFeature.", ex);
        }
    }

    /// <summary>
    /// Gets or creates a cached vertex buffer for the given vertices.
    /// </summary>
    private Buffer GetOrCreateVertexBuffer(Vector2[] vertices)
    {
        // Create a hash key based on the vertices content
        var hashCode = GetVerticesHashCode(vertices);

        if (!_vertexBufferCache.TryGetValue(hashCode, out var buffer))
        {
            // Create new buffer and cache it
            buffer = Buffer.Structured.New<Vector2>(Context.GraphicsDevice, vertices);
            _vertexBufferCache[hashCode] = buffer;
        }

        return buffer;
    }

    /// <summary>
    /// Computes a hash code for the vertices array.
    /// </summary>
    private static int GetVerticesHashCode(Vector2[] vertices)
    {
        var hash = vertices.Length.GetHashCode();
        foreach (var vertex in vertices)
        {
            hash = HashCode.Combine(hash, vertex.GetHashCode());
        }
        return hash;
    }

    /// <inheritdoc/>
    public override void Prepare(RenderDrawContext context)
        => base.Prepare(context);

    /// <inheritdoc/>
    public override void Draw(RenderDrawContext context, RenderView renderView, RenderViewStage renderViewStage)
    {
        if (_shader == null || _pipelineState == null) return;

        _shader.UpdateEffect(context.GraphicsDevice);

        // Cache frequently used values outside the loop
        var viewProjection = renderView.ViewProjection;
        var viewport = new Vector4(context.RenderContext.RenderView.ViewSize, 0, 0);
        var worldScale = new Vector3(ScaleAdjust + 1.0f);

        foreach (var renderNode in renderViewStage.SortedRenderNodes)
        {
            if (renderNode.RenderObject is not RenderMesh renderMesh)
            {
                continue;
            }

            MeshOutlineComponent? outlineScript = null;

            if (renderMesh.Source is ModelComponent component)
            {
                outlineScript = component.Entity.Get<MeshOutlineComponent>();
            }

            if (outlineScript is null || !outlineScript.Enabled)
            {
                continue;
            }

            MeshDraw drawData = renderMesh.ActiveMeshDraw;

            for (int slot = 0; slot < drawData.VertexBuffers.Length; slot++)
            {
                var vertexBuffer = drawData.VertexBuffers[slot];
                context.CommandList.SetVertexBuffer(slot, vertexBuffer.Buffer, vertexBuffer.Offset, vertexBuffer.Stride);
            }

            // Use cached values
            _shader.Parameters.Set(TransformationKeys.WorldViewProjection, renderMesh.World * viewProjection);
            _shader.Parameters.Set(TransformationKeys.WorldScale, worldScale);
            _shader.Parameters.Set(SDFPerimeterOutline2DShaderKeys.Viewport, viewport);
            _shader.Parameters.Set(SDFPerimeterOutline2DShaderKeys.Color, outlineScript.Color);
            _shader.Parameters.Set(SDFPerimeterOutline2DShaderKeys.Intensity, outlineScript.Intensity);
            _shader.Parameters.Set(SDFPerimeterOutline2DShaderKeys.OutlineThickness, outlineScript.OutlineThickness);
            _shader.Parameters.Set(SDFPerimeterOutline2DShaderKeys.ShapeType, (int)outlineScript.ShapeType);
            _shader.Parameters.Set(SDFPerimeterOutline2DShaderKeys.Radius, outlineScript.Radius);
            _shader.Parameters.Set(SDFPerimeterOutline2DShaderKeys.PixelScale, outlineScript.PixelScale);
            _shader.Parameters.Set(SDFPerimeterOutline2DShaderKeys.FillColor, outlineScript.Color);
            _shader.Parameters.Set(SDFPerimeterOutline2DShaderKeys.AntiAlias, 2);
            _shader.Parameters.Set(SDFPerimeterOutline2DShaderKeys.CapsuleHalfHeight, outlineScript.CapsuleHalfHeight - outlineScript.Radius);

            // Use cached vertex buffer instead of creating new ones
            if (outlineScript.PolygonVertices.Length > 0)
            {
                var vertexBuffer = GetOrCreateVertexBuffer(outlineScript.PolygonVertices);
                _shader.Parameters.Set(SDFPerimeterOutline2DShaderKeys.PolygonVertices, vertexBuffer);
                _shader.Parameters.Set(SDFPerimeterOutline2DShaderKeys.PolygonVertexCount, outlineScript.VertexCount);
            }
            else
            {
                _shader.Parameters.Set(SDFPerimeterOutline2DShaderKeys.PolygonVertexCount, 0);
            }

            //Console.WriteLine($"{_vertexBufferCache.Count} vertices");

            _pipelineState.State.RootSignature = _shader.RootSignature;
            _pipelineState.State.EffectBytecode = _shader.Effect.Bytecode;
            _pipelineState.State.PrimitiveType = drawData.PrimitiveType;

            _pipelineState.State.Output.CaptureState(context.CommandList);
            _pipelineState.Update();

            context.CommandList.SetIndexBuffer(drawData.IndexBuffer.Buffer, drawData.IndexBuffer.Offset, drawData.IndexBuffer.Is32Bit);
            context.CommandList.SetPipelineState(_pipelineState.CurrentState);

            _shader.Apply(context.GraphicsContext);

            if (drawData.IndexBuffer != null)
            {
                context.CommandList.DrawIndexed(drawData.DrawCount, drawData.StartLocation);
            }
            else
            {
                context.CommandList.Draw(drawData.DrawCount, drawData.StartLocation);
            }
        }
    }

    /// <summary>
    /// Cleans up cached vertex buffers.
    /// </summary>
    protected override void Destroy()
    {
        foreach (var buffer in _vertexBufferCache.Values)
        {
            buffer?.Dispose();
        }

        _vertexBufferCache.Clear();

        base.Destroy();
    }
}