using Stride.CommunityToolkit.Rendering;
using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Games;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Materials;
using System.Runtime.InteropServices;
using Buffer = Stride.Graphics.Buffer;

namespace Stride.CommunityToolkit.Shapes;

/// <summary>
/// Renders every <see cref="ShapeBatch"/> with <c>ShapeShader</c>: one instanced draw of a shared
/// quad per batch, the shape records, their points and the points of space runs delivered through
/// three structured buffers and evaluated per fragment as a signed distance function.
/// </summary>
/// <remarks>
/// The frame's shapes are uploaded once, in <see cref="Prepare"/>, for every batch together, and
/// the batches are emptied in <see cref="Flush"/>, after the last view has drawn - so a second
/// camera, a render-to-texture pass or the editor's view sees the same shapes as the first, and a
/// batch whose stage never runs does not keep growing.
/// </remarks>
public class ShapeBatchFeature : RootRenderFeature
{
    /// <summary>The GPU timing scope around each batch's draw, for Stride's profiler and for a frame capture.</summary>
    public static readonly ProfilingKey ProfilingKey = new("ShapeBatch");

    private static readonly Color4 ProfileColor = new(1f, 0.6f, 0.1f, 1f);

    private DynamicEffectInstance? _effect;
    private MutablePipelineState? _pipelineState;
    private Buffer? _quadBuffer;
    private Buffer? _instanceBuffer;
    private Buffer? _pointBuffer;
    private Buffer? _spacePointBuffer;
    private VertexDeclaration? _vertexDeclaration;
    private DisplayScale? _displayScale;

    // The depth the forward renderer resolved for each view before its transparent stage, handed
    // over through BindPerViewShaderResource; cleared each frame so a view that stops rendering
    // does not keep a stale texture
    private readonly Dictionary<RenderView, Texture> _depthByView = [];

    // The effect of every batch with a fill source, keyed by the batch: its own parameters carry
    // the textures, samplers and values the source generates keys for, and its own effect instance
    // reloads when the composed source changes. Pruned in Flush when a batch drops its source.
    private readonly Dictionary<ShapeBatch, TexturedEffect> _textured = [];

    // Every batch's records and points for the frame, one after another
    private readonly List<ShapeInstance> _instances = [];
    private readonly List<Vector2> _points = [];
    private readonly List<Vector4> _spacePoints = [];

    // The effect, its parameters and the pipeline state are shared by every draw of this
    // feature. Stride draws a stage on worker threads when it knows the stage's depth access;
    // for a feature without render effects it does not, so today the draws are sequential - but
    // a compositor may say so explicitly, and then two batches would draw at once.
    private readonly object _drawLock = new();

    /// <summary>
    /// Creates the feature. Shapes sort after everything else in their stage, so an overlay batch
    /// draws over the scene's other transparent objects and a depth-tested one still hides behind
    /// the opaque geometry it reads.
    /// </summary>
    public ShapeBatchFeature()
    {
        SortKey = 255;
    }

    /// <inheritdoc/>
    public override Type SupportedRenderObjectType => typeof(ShapeBatch);

    /// <inheritdoc/>
    protected override void InitializeCore()
    {
        _effect = new DynamicEffectInstance("ShapeEffect");
        _effect.Initialize(Context.Services);
        _effect.UpdateEffect(Context.GraphicsDevice);

        // The display's scale, for batches whose pixel widths follow it. Absent outside a game -
        // the editor, a test harness - which leaves the widths at exactly the pixels asked for.
        if (Context.Services.GetService<IGame>() is { } game)
        {
            _displayScale = DisplayScale.GetOrCreate(game);
        }

        _vertexDeclaration = new VertexDeclaration(VertexElement.Position<Vector2>());

        // A unit quad; the vertex shader grows it per shape to leave room for a thick border
        _quadBuffer = Buffer.Vertex.New(Context.GraphicsDevice, new[]
        {
            new Vector2(-1f, -1f),
            new Vector2(1f, -1f),
            new Vector2(-1f, 1f),
            new Vector2(1f, -1f),
            new Vector2(1f, 1f),
            new Vector2(-1f, 1f),
        });

        _instanceBuffer = Buffer.Structured.New<ShapeInstance>(Context.GraphicsDevice, 1);
        _pointBuffer = Buffer.Structured.New<Vector2>(Context.GraphicsDevice, 1);
        _spacePointBuffer = Buffer.Structured.New<Vector4>(Context.GraphicsDevice, 1);

        _pipelineState = new MutablePipelineState(Context.GraphicsDevice);
        _pipelineState.State.SetDefaults();
        _pipelineState.State.InputElements = _vertexDeclaration.CreateInputElements();
        // The shader composites its layers premultiplied and emits premultiplied colour, so the
        // blend is Stride's own AlphaBlend: source One, destination InverseSourceAlpha, which also
        // leaves a correct alpha in the target when a batch renders into a texture. Fed straight
        // alpha this state would add colour at full strength whatever the alpha - which is what
        // the shader used to produce, and why it once needed NonPremultiplied instead.
        _pipelineState.State.BlendState = BlendStates.AlphaBlend;
        _pipelineState.State.RasterizerState = RasterizerStates.CullNone;
        _pipelineState.State.PrimitiveType = PrimitiveType.TriangleList;
    }

    /// <summary>
    /// Gathers every batch's shapes into the two buffers, once per frame, before any view draws.
    /// </summary>
    public override void Prepare(RenderDrawContext context)
    {
        base.Prepare(context);

        _instances.Clear();
        _points.Clear();
        _spacePoints.Clear();
        _depthByView.Clear();

        foreach (var renderObject in RenderObjects)
        {
            var batch = (ShapeBatch)renderObject;

            // Where this batch's records and points start, for the shader to add to its indices
            batch.InstanceBase = _instances.Count;
            batch.PointBase = _points.Count;
            batch.SpacePointBase = _spacePoints.Count;

            _instances.AddRange(batch.Instances);
            _points.AddRange(batch.Points);
            _spacePoints.AddRange(batch.SpacePoints);
        }

        Upload(context, ref _instanceBuffer!, CollectionsMarshal.AsSpan(_instances));
        Upload(context, ref _pointBuffer!, CollectionsMarshal.AsSpan(_points));
        Upload(context, ref _spacePointBuffer!, CollectionsMarshal.AsSpan(_spacePoints));
    }

    /// <inheritdoc/>
    public override void Draw(RenderDrawContext context, RenderView renderView, RenderViewStage renderViewStage, int startIndex, int endIndex)
    {
        if (_effect is null || _pipelineState is null) return;

        var commandList = context.CommandList;

        // Pixels per world unit at clip w = 1: projection M22 is 2 / world height for an
        // orthographic view, and 1 / tan(fov / 2) for a perspective one, where the shader's
        // per-fragment w supplies the distance falloff
        var pixelScale = renderView.ViewSize.Y * renderView.Projection.M22 * 0.5f;

        var viewInverse = Matrix.Invert(renderView.View);
        var cameraRight = new Vector3(viewInverse.M11, viewInverse.M12, viewInverse.M13);
        var cameraUp = new Vector3(viewInverse.M21, viewInverse.M22, viewInverse.M23);
        var eyePosition = viewInverse.TranslationVector;

        // Stride decides once per device whether the backbuffer is sRGB; the shader decodes its
        // palette only then, the way SpriteBatch picks its sRGB effect
        var linearOutput = context.GraphicsDevice.ColorSpace == ColorSpace.Linear ? 1u : 0u;

        lock (_drawLock)
        {
            for (var index = startIndex; index < endIndex; index++)
            {
                var renderNodeReference = renderViewStage.SortedRenderNodes[index].RenderNode;
                var batch = (ShapeBatch)GetRenderNode(renderNodeReference).RenderObject;

                var effect = EffectFor(batch, context.GraphicsDevice);

                // A display at 150% has 1.5 physical pixels where a 100% one has one, so the same
                // width in "pixels" needs 1.5 of them: fewer pixels per world unit, as the shader
                // sees it, is what makes every pixel-measured width come out that much wider
                var displayScale = batch.AutoScale && _displayScale is not null ? _displayScale.Value : 1f;

                // What a pick needs to place the mouse the way this view placed the shapes. The mouse is
                // in the window, so the view drawn at the window's size is the one picks answer for; a
                // render-texture camera draws the same batch at its own size and must not take over.
                // Any view will do until the window's has drawn once.
                var backBuffer = context.GraphicsDevice.Presenter?.BackBuffer;
                var windowView = backBuffer is null || (renderView.ViewSize.X == backBuffer.Width && renderView.ViewSize.Y == backBuffer.Height);

                if (windowView || batch.LastView is null)
                {
                    batch.LastView = new ShapeView(renderView.ViewProjection, Matrix.Invert(renderView.ViewProjection), renderView.ViewSize, pixelScale / displayScale, displayScale, cameraRight, cameraUp, eyePosition);
                }

                if (batch.Instances.Count == 0) continue;

                using var _ = context.QueryManager.BeginProfile(ProfileColor, ProfilingKey);


                effect.UpdateEffect(context.GraphicsDevice);
                effect.Parameters.Set(ShapeShaderKeys.ViewProjection, renderView.ViewProjection);
                effect.Parameters.Set(ShapeShaderKeys.PixelScale, pixelScale / displayScale);
                effect.Parameters.Set(ShapeShaderKeys.CameraRight, cameraRight);
                effect.Parameters.Set(ShapeShaderKeys.CameraUp, cameraUp);
                effect.Parameters.Set(ShapeShaderKeys.EyePosition, eyePosition);
                effect.Parameters.Set(ShapeShaderKeys.LinearOutput, linearOutput);
                effect.Parameters.Set(ShapeShaderKeys.ViewSize, renderView.ViewSize);
                effect.Parameters.Set(ShapeShaderKeys.ScreenScale, displayScale);

                // The soft depth fade reads the scene's depth where the renderer bound it for this view
                if (_depthByView.TryGetValue(renderView, out var depth))
                {
                    effect.Parameters.Set(DepthBaseKeys.DepthStencil, depth);
                    effect.Parameters.Set(CameraKeys.ZProjection, CameraKeys.ZProjectionACalculate(renderView.NearClipPlane, renderView.FarClipPlane));
                    effect.Parameters.Set(ShapeShaderKeys.DepthAvailable, 1u);
                }
                else
                {
                    effect.Parameters.Set(ShapeShaderKeys.DepthAvailable, 0u);
                }
                effect.Parameters.Set(ShapeShaderKeys.InstanceBase, (uint)batch.InstanceBase);
                effect.Parameters.Set(ShapeShaderKeys.PointBase, (uint)batch.PointBase);
                effect.Parameters.Set(ShapeShaderKeys.SpacePointBase, (uint)batch.SpacePointBase);
                effect.Parameters.Set(ShapeShaderKeys.Shapes, _instanceBuffer);
                effect.Parameters.Set(ShapeDistanceKeys.Points, _pointBuffer);
                effect.Parameters.Set(ShapeDistanceKeys.SpacePoints, _spacePointBuffer);

                // Tested but never written: shapes are transparent, so writing depth would let one
                // shape reject another that should blend over it
                _pipelineState.State.DepthStencilState = batch.DepthTest ? DepthStencilStates.DepthRead : DepthStencilStates.None;
                _pipelineState.State.RootSignature = effect.RootSignature;
                _pipelineState.State.EffectBytecode = effect.Effect.Bytecode;
                _pipelineState.State.Output.CaptureState(commandList);
                _pipelineState.Update();

                commandList.SetPipelineState(_pipelineState.CurrentState);
                commandList.SetVertexBuffer(0, _quadBuffer, 0, _vertexDeclaration!.VertexStride);

                effect.Apply(context.GraphicsContext);

                commandList.DrawInstanced(6, batch.Instances.Count);
            }
        }
    }

    /// <summary>
    /// Takes the depth buffer the forward renderer resolves for a view before its transparent stage,
    /// for the soft depth fade. The renderer offers it to every root render feature under the
    /// "Depth" logical group; anything else offered is ignored.
    /// </summary>
    public override void BindPerViewShaderResource(string logicalGroupName, RenderView renderView, GraphicsResource resource)
    {
        if (logicalGroupName == "Depth" && resource is Texture texture)
        {
            _depthByView[renderView] = texture;
        }
    }

    /// <summary>
    /// Empties every batch once the frame's last view has drawn: immediate mode, one frame's
    /// submissions drawn by every view that wants them and then gone.
    /// </summary>
    public override void Flush(RenderDrawContext context)
    {
        base.Flush(context);

        foreach (var renderObject in RenderObjects)
        {
            ((ShapeBatch)renderObject).Reset();
        }

        // A batch that dropped its fill source, or left rendering, gives its effect back
        foreach (var (batch, textured) in _textured)
        {
            if (batch.FillSource is null || !RenderObjects.Contains(batch))
            {
                textured.Dispose();
                _textured.Remove(batch);
            }
        }
    }

    /// <summary>
    /// The effect a batch draws with this frame: the shared plain one, or the batch's own with its
    /// fill source composed in. The source is regenerated every frame - a handful of parameter
    /// sets and one equality check - so a node's texture, scale or offset changed from code is
    /// picked up next frame, and only a different composition reloads the effect.
    /// </summary>
    private DynamicEffectInstance EffectFor(ShapeBatch batch, GraphicsDevice graphicsDevice)
    {
        if (batch.FillSource is not { } fill) return _effect!;

        if (!_textured.TryGetValue(batch, out var textured))
        {
            textured = new TexturedEffect();
            textured.Effect.Initialize(Context.Services);
            _textured[batch] = textured;
        }

        // A fresh context each time hands out the same indexed keys, so the parameters of the
        // previous frame are overwritten rather than accumulated
        using var generator = new ShaderGeneratorContext(graphicsDevice)
        {
            Parameters = textured.Parameters,
            ColorSpace = graphicsDevice.ColorSpace,
        };

        var source = fill.GenerateShaderSource(generator, new MaterialComputeColorKeys(ShapeEffectKeys.FillMap, ShapeEffectKeys.FillValue, Color.White));

        if (!source.Equals(textured.Parameters.Get(ShapeEffectKeys.FillSource)))
        {
            textured.Parameters.Set(ShapeEffectKeys.FillSource, source);
        }

        return textured.Effect;
    }

    /// <summary>A textured batch's effect and the parameter collection it and its fill source share.</summary>
    private sealed class TexturedEffect : IDisposable
    {
        internal ParameterCollection Parameters { get; } = new();

        internal DynamicEffectInstance Effect { get; }

        public TexturedEffect()
        {
            Effect = new DynamicEffectInstance("ShapeEffect", Parameters);
        }

        public void Dispose() => Effect.Dispose();
    }

    // Default usage and a whole-buffer update: the one contiguous upload per frame that every
    // backend takes on its fast path. Grown by powers of two, never shrunk.
    private static void Upload<T>(RenderDrawContext context, ref Buffer buffer, ReadOnlySpan<T> data) where T : unmanaged
    {
        if (data.IsEmpty) return;

        if (data.Length > buffer.ElementCount)
        {
            buffer.Dispose();

            var capacity = (int)System.Numerics.BitOperations.RoundUpToPowerOf2((uint)Math.Max(data.Length, 64));
            buffer = Buffer.Structured.New<T>(context.GraphicsDevice, capacity);
        }

        buffer.SetData(context.CommandList, data);
    }

    /// <inheritdoc/>
    public override void Unload()
    {
        _effect?.Dispose();

        foreach (var textured in _textured.Values)
        {
            textured.Dispose();
        }

        _textured.Clear();
        _quadBuffer?.Dispose();
        _instanceBuffer?.Dispose();
        _pointBuffer?.Dispose();
        _spacePointBuffer?.Dispose();

        base.Unload();
    }
}