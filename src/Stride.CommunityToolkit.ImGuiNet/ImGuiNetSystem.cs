using ImGuiNET;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering;
using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;
using Stride.Input;
using Stride.Rendering;
using System.Runtime.CompilerServices;
using Rectangle = Stride.Core.Mathematics.Rectangle;

namespace Stride.CommunityToolkit.ImGuiNet;

/// <summary>
/// Provides ImGui.NET integration for Stride with Box2D.NET-style text rendering capabilities.
/// This is an alternative to the existing Hexa.NET.ImGui implementation in the toolkit.
/// </summary>
public class ImGuiNetSystem : GameSystemBase
{
    private static readonly Logger _logger = GlobalLogger.GetLogger("ImGuiNet");

    // GameSystemBase.Game is nullable; resolve it once so every later access is a plain dereference.
    private readonly GameBase _game;
    private readonly ImGuiNetTextOverlay _textOverlay = new();

    /// <summary>
    /// Whether <c>NewFrame</c> has been called without a matching <c>Render</c> yet.
    /// </summary>
    /// <remarks>
    /// Dear ImGui requires exactly one <c>NewFrame</c> per <c>Render</c>, and this system splits the pair
    /// across <see cref="Update"/> and <see cref="EndDraw"/>. That holds only while the host runs one
    /// update per draw - true of Stride's default variable timestep, and not true once
    /// <c>IsFixedTimeStep</c> is set, because the game then runs extra updates to catch up whenever a
    /// frame overruns. The startup shader compile causes exactly that, and two <c>NewFrame</c> calls in a
    /// row abort the process from native code with "Forgot to call Render() or EndFrame() at the end of
    /// the previous frame?" - an abort, so neither the <see cref="IsInitialized"/> guard nor the
    /// try/catch in <see cref="EndDraw"/> can intercept it.
    /// </remarks>
    private bool _frameBegun;

    private InputManager? _inputManager;
    private GraphicsDevice? _graphicsDevice;
    private CommandList? _commandList;
    private Texture? _fontTexture;
    private GraphicsContext? _graphicsContext;
    private CameraComponent? _camera;

    // Rendering infrastructure
    private VertexBufferBinding _vertexBinding;
    private IndexBufferBinding? _indexBinding;
    private EffectInstance? _imguiShader;
    private PipelineState? _pipelineState;
    private VertexDeclaration? _vertexLayout;

    // ImGui.NET context
    private IntPtr _context;

    // DPI / scaling state
    private float _dpiScale = 1f;
    private bool _pendingFontRebuild;

    /// <summary>
    /// Optional path to a custom TTF font. If the file exists, it will be used instead of the default font.
    /// Defaults to 'data/droid_sans.ttf'.
    /// </summary>
    public string? FontPath { get; set; } = Path.Combine("data", "droid_sans.ttf");

    /// <summary>
    /// Font size in pixels for the custom TTF font. Ignored if <see cref="FontPath"/> doesn't exist.
    /// </summary>
    public float FontSize { get; set; } = 15f;

    /// <summary>
    /// When true (default), the font atlas is rebuilt when the display scale changes, for example
    /// when the window moves to a monitor with a different DPI, so text stays crisp.
    /// </summary>
    public bool AutoScaleFonts { get; set; } = true;

    /// <summary>
    /// Gets or sets whether UI elements should be displayed.
    /// </summary>
    public bool ShowUI { get; set; } = true;

    /// <summary>
    /// Gets whether the ImGui system is initialized and ready for use.
    /// </summary>
    public bool IsInitialized { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ImGuiNetSystem"/> class.
    /// </summary>
    /// <param name="registry">The service registry.</param>
    public ImGuiNetSystem(IServiceRegistry registry) : base(registry)
    {
        _game = Game ?? throw new InvalidOperationException("ImGuiNetSystem: IGame must be available in the service registry.");

        Enabled = true;
        Visible = true;
        UpdateOrder = 1;

        Services.AddService(this);
        _game.GameSystems.Add(this);
    }

    /// <summary>
    /// Draws a string at screen coordinates, similar to Box2D.NET's DrawString method.
    /// </summary>
    /// <param name="x">The x coordinate in screen space.</param>
    /// <param name="y">The y coordinate in screen space.</param>
    /// <param name="message">The message to display.</param>
    /// <param name="color">The text color (optional, defaults to light gray).</param>
    public void DrawString(int x, int y, string message, Vector4? color = null)
    {
        if (ShowUI && IsInitialized) _textOverlay.AddScreenText(x, y, message, color);
    }

    /// <summary>
    /// Draws a string at world coordinates, similar to Box2D.NET's DrawString method.
    /// </summary>
    /// <param name="worldPosition">The position in world space.</param>
    /// <param name="message">The message to display.</param>
    /// <param name="color">The text color (optional, defaults to light gray).</param>
    public void DrawString(Vector3 worldPosition, string message, Vector4? color = null)
    {
        if (ShowUI && IsInitialized) _textOverlay.AddWorldText(worldPosition, message, color);
    }

    /// <summary>
    /// Allows external DPI providers (e.g. Windows DPI awareness helpers) to drive the font scaling.
    /// Pass the scale relative to 96 DPI (1.0 == 96 DPI, 2.0 == 192 DPI, etc.).
    /// When this value changes significantly, the font atlas will be rebuilt.
    /// </summary>
    public void SetDpiScale(float dpiScale)
    {
        if (dpiScale <= 0 || MathF.Abs(dpiScale - _dpiScale) < 0.01f) return;

        var old = _dpiScale;

        _dpiScale = dpiScale;
        _pendingFontRebuild = true;

        // Scale ImGui style sizes proportionally to maintain UI sizing
        try
        {
            if (old > 0) ImGui.GetStyle().ScaleAllSizes(_dpiScale / old);
        }
        catch { /* ignore if context not ready */ }

        _logger.Info($"ImGuiNetSystem: DPI scale updated to {_dpiScale:F2} (pending font rebuild)");
    }

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        _inputManager = Services.GetService<InputManager>();
        _graphicsDevice = _game.GraphicsDevice;
        _graphicsContext = _game.GraphicsContext;
        _commandList = _graphicsContext?.CommandList;

        if (_graphicsDevice is null)
        {
            _logger.Warning("ImGuiNetSystem: GraphicsDevice not available");
            return;
        }

        try
        {
            _context = ImGui.CreateContext();
            ImGui.SetCurrentContext(_context);

            var io = ImGui.GetIO();

            io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;

            // The backbuffer-to-window ratio alone is 1 on Windows at any scaling; the display
            // scale service also knows the monitor's DPI, and says when the window moves
            var displayScale = DisplayScale.GetOrCreate(_game);

            displayScale.Refresh();
            _dpiScale = displayScale.Value;

            displayScale.Changed += (_, _) =>
            {
                if (AutoScaleFonts) SetDpiScale(displayScale.Value);
            };

            _logger.Info($"ImGuiNetSystem: display scale {_dpiScale:F2}");

            // Build the font atlas at the current DPI - this keeps fonts crisp
            SetupFontAtlas(_dpiScale);
            CreateRenderingResources();

            _camera = _game.Services.GetService<SceneSystem>()?.SceneInstance.RootScene.GetCamera();
            IsInitialized = true;

            _logger.Info("ImGuiNetSystem initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to initialize ImGuiNetSystem: {ex.Message}");
        }
    }

    private void CreateRenderingResources()
    {
        if (_graphicsDevice is null || _graphicsContext is null) return;

        if (Services.GetService<EffectSystem>() is { } effectSystem)
        {
            try
            {
                _imguiShader = new EffectInstance(effectSystem.LoadEffect("ImGuiNetShader").WaitForResult());
                _imguiShader.UpdateEffect(_graphicsDevice);
                _logger.Info("Using ImGuiNetShader for rendering");
            }
            catch
            {
                _logger.Warning("Could not load any ImGui shader, text will not be visible");
                return;
            }
        }

        _vertexLayout = new VertexDeclaration(VertexElement.Position<Vector2>(), VertexElement.TextureCoordinate<Vector2>(), VertexElement.Color(PixelFormat.R8G8B8A8_UNorm));

        var pipelineDesc = new PipelineStateDescription
        {
            BlendState = BlendStates.NonPremultiplied,
            RasterizerState = new RasterizerStateDescription
            {
                CullMode = CullMode.None,
                DepthBias = 0,
                FillMode = FillMode.Solid,
                MultisampleAntiAliasLine = false,
                ScissorTestEnable = true,
                SlopeScaleDepthBias = 0,
            },
            PrimitiveType = PrimitiveType.TriangleList,
            InputElements = _vertexLayout.CreateInputElements(),
            DepthStencilState = DepthStencilStates.None,
            EffectBytecode = _imguiShader?.Effect.Bytecode,
            RootSignature = _imguiShader?.RootSignature,
            Output = new RenderOutputDescription(PixelFormat.R8G8B8A8_UNorm)
        };

        _pipelineState = PipelineState.New(_graphicsDevice, pipelineDesc);

        // The 'bufferSize'/'usage' arguments must be named. Buffer.Vertex.New also exposes a generic
        // New<T>(device, ref readonly T value, usage) overload, and positional arguments bind to it
        // (T = int) instead of the size-based overload, producing a 4-byte buffer.
        var vertexBuffer = Stride.Graphics.Buffer.Vertex.New(_graphicsDevice, bufferSize: 1024 * _vertexLayout.CalculateSize(), usage: GraphicsResourceUsage.Dynamic);
        var indexBuffer = Stride.Graphics.Buffer.Index.New(_graphicsDevice, bufferSize: 2048 * sizeof(ushort), usage: GraphicsResourceUsage.Dynamic);

        _vertexBinding = new VertexBufferBinding(vertexBuffer, _vertexLayout, 0);
        _indexBinding = new IndexBufferBinding(indexBuffer, false, 0);
    }

    private unsafe void SetupFontAtlas(float dpiScale)
    {
        var io = ImGui.GetIO();

        io.Fonts.Clear();

        // Bake default font (wrapper does not expose size for default font), will be scaled by style/font scaling
        if (!TryLoadCustomFont(io, dpiScale)) io.Fonts.AddFontDefault();

        io.Fonts.GetTexDataAsRGBA32(out byte* pixels, out var width, out var height, out var bytesPerPixel);

        _fontTexture?.Dispose();
        _fontTexture = null;

        if (_graphicsDevice is not null && pixels != null)
        {
            // Create Stride texture from ImGui font data
            _fontTexture = Texture.New2D(_graphicsDevice, width, height, PixelFormat.R8G8B8A8_UNorm, TextureFlags.ShaderResource);

            if (_commandList is not null) _fontTexture.SetData(_commandList, new ReadOnlySpan<byte>(pixels, width * height * bytesPerPixel));

            // Set a simple texture ID for ImGui (using texture hashcode as a simple identifier)
            io.Fonts.SetTexID((IntPtr)_fontTexture.GetHashCode());

            // Optionally clear CPU-side temp data
            io.Fonts.ClearTexData();
        }
        else
        {
            // Fallback: just mark as built without texture
            io.Fonts.SetTexID(IntPtr.Zero);
        }

        _logger.Info($"Font atlas built successfully: {width}x{height}, {bytesPerPixel} bpp (dpiScale={dpiScale:F2})");
    }

    private bool TryLoadCustomFont(ImGuiIOPtr io, float dpiScale)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(FontPath) || !File.Exists(FontPath)) return false;

            io.Fonts.AddFontFromFileTTF(FontPath, MathF.Max(1f, FontSize * dpiScale));
            _logger.Info($"Loaded custom ImGui font: '{FontPath}' at {FontSize * dpiScale:F1}px (scale {dpiScale:F2})");

            return true;
        }
        catch (Exception ex)
        {
            _logger.Warning($"Failed to load custom font '{FontPath}': {ex.Message}. Falling back to default font.");

            return false;
        }
    }

    /// <inheritdoc/>
    public override void Update(GameTime gameTime)
    {
        if (!IsInitialized) return;

        var io = ImGui.GetIO();

        UpdateDisplayMetrics(io);

        if (_pendingFontRebuild)
        {
            SetupFontAtlas(_dpiScale);
            _pendingFontRebuild = false;
        }

        var deltaTime = (float)gameTime.Elapsed.TotalSeconds;

        io.DeltaTime = deltaTime > 0 ? deltaTime : 1f / 60f;

        if (_inputManager is not null) ImGuiNetInputMapper.Update(_inputManager);

        // An update that never reached a draw left a frame open. Close it before starting the next one,
        // discarding its draw data - the frame it belonged to is not being presented anyway.
        if (_frameBegun) ImGui.EndFrame();

        ImGui.NewFrame();
        _frameBegun = true;

        _textOverlay.Draw(ShowUI, _camera, GraphicsDevice);
    }

    /// <summary>
    /// Tells ImGui the size of the surface it is drawing on. The font atlas is rebuilt from
    /// <see cref="DisplayScale.Changed"/>, not from here: the backbuffer-to-window ratio this reads
    /// is 1 on Windows however the display is scaled.
    /// </summary>
    private void UpdateDisplayMetrics(ImGuiIOPtr io)
    {
        var clientBounds = _game.Window.ClientBounds;

        io.DisplaySize = new Vector2(clientBounds.Width, clientBounds.Height);

        // HiDPI/backbuffer scaling (matches Box2D.NET pattern)
        if (_graphicsDevice?.Presenter?.BackBuffer is not { } back || clientBounds.Width <= 0 || clientBounds.Height <= 0) return;

        io.DisplayFramebufferScale = new Vector2(back.Width / (float)clientBounds.Width, back.Height / (float)clientBounds.Height);
    }

    /// <inheritdoc/>
    public override void EndDraw()
    {
        // Nothing to present when the draw is not paired with an update - Render() without a preceding
        // NewFrame() asserts just as loudly as the reverse.
        if (!IsInitialized || !_frameBegun) return;

        try
        {
            ImGui.Render();
            _frameBegun = false;

            RenderImGuiDrawData();
        }
        catch (Exception ex)
        {
            _logger.Warning($"Error in ImGui EndDraw: {ex.Message}");
        }
    }

    private unsafe void RenderImGuiDrawData()
    {
        var drawData = ImGui.GetDrawData();

        if (drawData.CmdListsCount == 0 || _commandList is null || _imguiShader is null || _pipelineState is null) return;

        var clientBounds = _game.Window.ClientBounds;
        var projMatrix = Matrix.OrthoRH(clientBounds.Width, -clientBounds.Height, -1, 1);

        _commandList.SetPipelineState(_pipelineState);

        if (!TrySetShaderParameters(ref projMatrix)) return;

        _imguiShader.Apply(_graphicsContext);

        for (var n = 0; n < drawData.CmdListsCount; n++)
        {
            var cmdList = drawData.CmdLists[n];

            EnsureBufferCapacity(cmdList);
            UploadCommandListData(cmdList);
            DrawCommandList(cmdList);
        }
    }

    /// <summary>
    /// Puts the projection matrix and font texture on the shader.
    /// </summary>
    /// <returns>
    /// <see langword="false"/> if the parameters could not be set, in which case the frame is skipped.
    /// </returns>
    private bool TrySetShaderParameters(ref Matrix projMatrix)
    {
        try
        {
            _imguiShader!.Parameters.Set(ImGuiNetShaderKeys.proj, ref projMatrix);
            _imguiShader.Parameters.Set(ImGuiNetShaderKeys.tex, _fontTexture);
            // ImGui's colours are sRGB bytes; the shader decodes them only when the device says the
            // backbuffer is sRGB (the default), the way Stride's own SpriteBatch does
            _imguiShader.Parameters.Set(ImGuiNetShaderKeys.LinearOutput, _graphicsDevice!.ColorSpace == ColorSpace.Linear ? 1u : 0u);

            return true;
        }
        catch
        {
            _logger.Warning("Using fallback shader parameter setting");

            return false;
        }
    }

    /// <summary>
    /// Grows the vertex and index buffers when this command list does not fit in them.
    /// </summary>
    private void EnsureBufferCapacity(ImDrawListPtr cmdList)
    {
        // See CreateRenderingResources for why the arguments are named
        var vertexBytes = cmdList.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>();

        if (vertexBytes > _vertexBinding.Buffer.SizeInBytes)
        {
            _vertexBinding.Buffer?.Dispose();
            _vertexBinding = new VertexBufferBinding(Stride.Graphics.Buffer.Vertex.New(_graphicsDevice, bufferSize: vertexBytes * 2, usage: GraphicsResourceUsage.Dynamic), _vertexLayout, 0);
        }

        var indexBytes = cmdList.IdxBuffer.Size * sizeof(ushort);

        if (indexBytes > _indexBinding!.Buffer.SizeInBytes)
        {
            _indexBinding.Buffer?.Dispose();
            _indexBinding = new IndexBufferBinding(Stride.Graphics.Buffer.Index.New(_graphicsDevice, bufferSize: indexBytes * 2, usage: GraphicsResourceUsage.Dynamic), false, 0);
        }
    }

    /// <summary>
    /// Uploads this command list's vertices and indices, then binds them.
    /// </summary>
    private unsafe void UploadCommandListData(ImDrawListPtr cmdList)
    {
        _vertexBinding.Buffer.SetData(_commandList, new ReadOnlySpan<ImDrawVert>((void*)cmdList.VtxBuffer.Data, cmdList.VtxBuffer.Size));
        _indexBinding!.Buffer.SetData(_commandList, new ReadOnlySpan<ushort>((void*)cmdList.IdxBuffer.Data, cmdList.IdxBuffer.Size));

        _commandList!.SetVertexBuffer(0, _vertexBinding.Buffer, 0, Unsafe.SizeOf<ImDrawVert>());
        _commandList.SetIndexBuffer(_indexBinding.Buffer, 0, false);
    }

    /// <summary>
    /// Issues one scissored indexed draw per ImGui command in this list.
    /// </summary>
    private void DrawCommandList(ImDrawListPtr cmdList)
    {
        var idxOffset = 0;

        for (var i = 0; i < cmdList.CmdBuffer.Size; i++)
        {
            var cmd = cmdList.CmdBuffer[i];

            _commandList!.SetScissorRectangle(new Rectangle((int)cmd.ClipRect.X, (int)cmd.ClipRect.Y, (int)(cmd.ClipRect.Z - cmd.ClipRect.X), (int)(cmd.ClipRect.W - cmd.ClipRect.Y)));
            _commandList.DrawIndexed((int)cmd.ElemCount, idxOffset, 0);

            idxOffset += (int)cmd.ElemCount;
        }
    }

    /// <inheritdoc/>
    protected override void Destroy()
    {
        _fontTexture?.Dispose();
        _fontTexture = null;
        _vertexBinding.Buffer?.Dispose();
        _indexBinding?.Buffer?.Dispose();
        _imguiShader?.Dispose();

        if (IsInitialized && _context != IntPtr.Zero) ImGui.DestroyContext(_context);

        base.Destroy();
    }
}