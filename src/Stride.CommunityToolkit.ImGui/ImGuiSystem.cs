using Hexa.NET.ImGui;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Games;
using Stride.Graphics;
using Stride.Input;
using Stride.Rendering;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Hexa.NET.ImGui.ImGui;

namespace Stride.CommunityToolkit.ImGui;

/// <summary>
/// The Dear ImGui backend for Stride: feeds input to ImGui, begins a frame every update and renders the draw lists
/// at the end of every draw. Create one after the game's graphics device exists and every <see cref="BaseWindow"/>
/// created afterwards is drawn through it.
/// </summary>
/// <remarks>
/// The constructor registers the instance as a service and as a game system, so keeping the returned reference is
/// optional; <see cref="BaseWindow"/> finds it through <see cref="IServiceRegistry"/>.
/// </remarks>
public class ImGuiSystem : GameSystemBase
{
    private const int InitialVertexBufferSize = 128;
    private const int InitialIndexBufferSize = 128;
    private const bool Is32BitIndices = false;

    // The Stride keys with an ImGui counterpart; everything else only reaches ImGui as text
    private static readonly Dictionary<Keys, ImGuiKey> _keys = new()
    {
        [Keys.Tab] = ImGuiKey.Tab,
        [Keys.Left] = ImGuiKey.LeftArrow,
        [Keys.Right] = ImGuiKey.RightArrow,
        [Keys.Up] = ImGuiKey.UpArrow,
        [Keys.Down] = ImGuiKey.DownArrow,
        [Keys.PageUp] = ImGuiKey.PageUp,
        [Keys.PageDown] = ImGuiKey.PageDown,
        [Keys.Home] = ImGuiKey.Home,
        [Keys.End] = ImGuiKey.End,
        [Keys.Delete] = ImGuiKey.Delete,
        [Keys.Back] = ImGuiKey.Backspace,
        [Keys.Enter] = ImGuiKey.Enter,
        [Keys.Escape] = ImGuiKey.Escape,
        [Keys.Space] = ImGuiKey.Space,
        [Keys.A] = ImGuiKey.A,
        [Keys.C] = ImGuiKey.C,
        [Keys.V] = ImGuiKey.V,
        [Keys.X] = ImGuiKey.X,
        [Keys.Y] = ImGuiKey.Y,
        [Keys.Z] = ImGuiKey.Z,
    };

    // Kept in static fields because ImGui holds raw pointers to these delegates
    [FixedAddressValueType] private static SetClipboardDelegate? _setClipboardFn;
    [FixedAddressValueType] private static GetClipboardDelegate? _getClipboardFn;

    /// <summary>
    /// The ImGui context this system created and renders. Pass it to add-on libraries (ImNodes, ImPlot) that need to
    /// share the context.
    /// </summary>
    public readonly ImGuiContextPtr ImGuiContext;

    /// <summary>
    /// A UI scale factor windows can read through <see cref="BaseWindow.Scale"/> to size themselves for the display's DPI.
    /// Defaults to <c>1</c>; it is not applied automatically.
    /// </summary>
    public float Scale { get; set; } = 1;

    private ImGuiIOPtr _io;
    private ImGuiPlatformIOPtr _platform;

    // dependencies
    private readonly GameBase _game;
    private readonly InputManager _input;
    private readonly GraphicsDevice _device;
    private readonly GraphicsContext _context;
    private readonly EffectSystem _effectSystem;
    private readonly CommandList _commandList;
    private readonly ImGuiTextureManager _textures;

    // device objects
    private PipelineState _pipeline;
    private VertexDeclaration _vertexLayout;
    private VertexBufferBinding _vertexBinding;
    private IndexBufferBinding _indexBinding;
    private EffectInstance _shader;

    private bool _isFirstFrame = true;

    /// <summary>
    /// Whether <see cref="Hexa.NET.ImGui.ImGui.NewFrame"/> has been called without a matching
    /// <see cref="Hexa.NET.ImGui.ImGui.Render"/> yet.
    /// </summary>
    /// <remarks>
    /// Dear ImGui requires exactly one <c>NewFrame</c> per <c>Render</c>, and this system splits the pair
    /// across <see cref="Update"/> and <see cref="EndDraw"/>. That holds only while the host runs one
    /// update per draw, which is true of Stride's default variable timestep and <em>not</em> true when
    /// <see cref="GameBase.IsFixedTimeStep"/> is set: the game then runs extra updates to catch up
    /// whenever a frame overruns, which the startup shader compile reliably causes. Two <c>NewFrame</c>
    /// calls in a row abort the process with "Forgot to call Render() or EndFrame() at the end of the
    /// previous frame?".
    ///
    /// The pair cannot simply be moved into the draw phase: windows build their UI from
    /// <see cref="BaseWindow.Update"/>, which runs after this system's update, so <c>NewFrame</c> has to
    /// have happened by then.
    /// </remarks>
    private bool _frameBegun;

    /// <summary>
    /// Creates the ImGui context, compiles the ImGui shader, allocates the vertex and index buffers, and registers the
    /// system with the game's services and systems.
    /// </summary>
    /// <param name="registry">The game's service registry; must provide <see cref="IGame"/>, <see cref="GraphicsContext"/> and <see cref="EffectSystem"/>.</param>
    /// <param name="graphicsDeviceManager">The device manager whose <see cref="GraphicsDeviceManager.GraphicsDevice"/> is rendered to.</param>
    /// <param name="inputManager">The input manager to read from, or <see langword="null"/> to resolve it from <paramref name="registry"/>.</param>
    /// <exception cref="ArgumentNullException">If <paramref name="registry"/> or <paramref name="graphicsDeviceManager"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">If a required service or the graphics device is not available yet.</exception>
    public ImGuiSystem(IServiceRegistry registry, GraphicsDeviceManager graphicsDeviceManager, InputManager? inputManager = null) : base(registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(graphicsDeviceManager);

        _game = Game ?? throw new InvalidOperationException("ImGuiSystem: IGame must be available!");
        _input = inputManager ?? Services.GetService<InputManager>() ?? throw new InvalidOperationException("ImGuiSystem: InputManager must be available!");
        _device = graphicsDeviceManager.GraphicsDevice ?? throw new InvalidOperationException("ImGuiSystem: GraphicsDevice must be available!");
        _context = Services.GetService<GraphicsContext>() ?? throw new InvalidOperationException("ImGuiSystem: GraphicsContext must be available!");
        _effectSystem = Services.GetService<EffectSystem>() ?? throw new InvalidOperationException("ImGuiSystem: EffectSystem must be available!");
        _commandList = _context.CommandList;
        _textures = new ImGuiTextureManager(_device, _commandList);

        ImGuiContext = CreateContext();
        SetCurrentContext(ImGuiContext);

        _io = GetIO();
        _platform = GetPlatformIO();

        SetupInput();
        CreateDeviceObjects();

        // Opt into the Dear ImGui 1.92+ texture management protocol so NewFrame() doesn't assert on IsBuilt()
        _io.BackendFlags |= ImGuiBackendFlags.RendererHasTextures;
        _platform.RendererTextureMaxWidth = 4096;
        _platform.RendererTextureMaxHeight = 4096;

        Enabled = true; // Force Update functions to be run
        Visible = true; // Force Draw related functions to be run
        UpdateOrder = 1; // Update should occur after Stride's InputManager

        // Include this new instance into our services and systems so that stride fires our functions automatically
        Services.AddService(this);
        _game.GameSystems.Add(this);
    }

    /// <inheritdoc />
    protected override void Destroy()
    {
        _textures.Dispose();
        _vertexBinding.Buffer?.Dispose();
        _indexBinding?.Buffer?.Dispose();
        _pipeline?.Dispose();
        _shader?.Dispose();
        DestroyContext(ImGuiContext);
        base.Destroy();
    }

    private unsafe void SetupInput()
    {
        _io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;

        _setClipboardFn = SetClipboard;
        _getClipboardFn = GetClipboard;

        _platform.PlatformSetClipboardTextFn = (void*)Marshal.GetFunctionPointerForDelegate(_setClipboardFn);
        _platform.PlatformGetClipboardTextFn = (void*)Marshal.GetFunctionPointerForDelegate(_getClipboardFn);
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private unsafe delegate void SetClipboardDelegate(ImGuiContextPtr ctx, byte* text);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private unsafe delegate byte* GetClipboardDelegate(ImGuiContextPtr ctx);

    // The clipboard is not bridged to the OS: text stays within ImGui's own buffer
    private static unsafe void SetClipboard(ImGuiContextPtr ctx, byte* text) { }

    private unsafe byte* GetClipboard(ImGuiContextPtr ctx) => (byte*)_platform.PlatformClipboardUserData;

    [MemberNotNull(nameof(_shader), nameof(_vertexLayout), nameof(_pipeline), nameof(_indexBinding))]
    private void CreateDeviceObjects()
    {
        _shader = new EffectInstance(_effectSystem.LoadEffect("ImGuiShader").WaitForResult());
        _shader.UpdateEffect(_device);

        _vertexLayout = new VertexDeclaration(VertexElement.Position<Vector2>(), VertexElement.TextureCoordinate<Vector2>(), VertexElement.Color(PixelFormat.R8G8B8A8_UNorm));

        var pipeline = new PipelineStateDescription
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
            DepthStencilState = DepthStencilStates.Default,
            EffectBytecode = _shader.Effect.Bytecode,
            RootSignature = _shader.RootSignature,
            Output = new RenderOutputDescription(PixelFormat.R8G8B8A8_UNorm)
        };

        _pipeline = PipelineState.New(_device, pipeline);

        _indexBinding = new IndexBufferBinding(Stride.Graphics.Buffer.Index.New(_device, InitialIndexBufferSize * sizeof(ushort), GraphicsResourceUsage.Dynamic), Is32BitIndices, 0);

        // BufferFlags is passed explicitly: without it, C# picks the generic New<T>(device, ref readonly T value, usage)
        // overload with T = int (it needs no default argument) and creates a 4-byte buffer holding the size value.
        var vertexBuffer = Stride.Graphics.Buffer.Vertex.New(_device, InitialVertexBufferSize * _vertexLayout.CalculateSize(), GraphicsResourceUsage.Dynamic, BufferFlags.None);

        _vertexBinding = new VertexBufferBinding(vertexBuffer, _vertexLayout, 0);
    }

    /// <summary>
    /// Forwards this frame's input and display size to ImGui and begins a new ImGui frame. Windows build their UI in
    /// their own <c>Update</c>, which runs after this one.
    /// </summary>
    /// <inheritdoc />
    public override void Update(GameTime gameTime)
    {
        // The first frame has no elapsed time worth reporting
        var deltaTime = _isFirstFrame ? 1 / 60f : (float)gameTime.Elapsed.TotalSeconds;

        _isFirstFrame = false;

        var surfaceSize = _game.Window.ClientBounds;

        _io.DisplaySize = new System.Numerics.Vector2(surfaceSize.Width, surfaceSize.Height);
        _io.DeltaTime = deltaTime;

        FeedInput();

        // An update that never reached a draw left a frame open. Close it before starting the next one,
        // discarding its draw data - the frame it belonged to is not being presented anyway.
        if (_frameBegun) Hexa.NET.ImGui.ImGui.EndFrame();

        Hexa.NET.ImGui.ImGui.NewFrame();
        _frameBegun = true;
    }

    /// <summary>
    /// Feeds one frame of Stride input into ImGui: mouse position and buttons, text, keys, wheel and
    /// modifiers. Skipped while the mouse is position-locked.
    /// </summary>
    private void FeedInput()
    {
        if (_input.HasMouse && _input.IsMousePositionLocked) return;

        var mousePos = _input.AbsoluteMousePosition;

        _io.AddMousePosEvent(mousePos.X, mousePos.Y);

        if (_io.WantTextInput) _input.TextInput.EnabledTextInput();
        else _input.TextInput.DisableTextInput();

        ForwardInputEvents();
        ForwardMouseButtonsAndModifiers();
    }

    /// <summary>Forwards this frame's text, key and wheel events to ImGui.</summary>
    private void ForwardInputEvents()
    {
        foreach (var ev in _input.Events)
        {
            switch (ev)
            {
                case TextInputEvent tev when tev.Text != "\t":
                    _io.AddInputCharactersUTF8(tev.Text);
                    break;
                case KeyEvent kev when _keys.TryGetValue(kev.Key, out var imGuiKey):
                    _io.AddKeyEvent(imGuiKey, _input.IsKeyDown(kev.Key));
                    break;
                case MouseWheelEvent mw:
                    _io.AddMouseWheelEvent(0, mw.WheelDelta);
                    break;
            }
        }
    }

    /// <summary>Forwards the mouse button and modifier key states to ImGui.</summary>
    private void ForwardMouseButtonsAndModifiers()
    {
        _io.AddMouseButtonEvent(0, _input.IsMouseButtonDown(MouseButton.Left));
        _io.AddMouseButtonEvent(1, _input.IsMouseButtonDown(MouseButton.Right));
        _io.AddMouseButtonEvent(2, _input.IsMouseButtonDown(MouseButton.Middle));

        _io.AddKeyEvent(ImGuiKey.ModAlt, _input.IsKeyDown(Keys.LeftAlt) || _input.IsKeyDown(Keys.RightAlt));
        _io.AddKeyEvent(ImGuiKey.ModShift, _input.IsKeyDown(Keys.LeftShift) || _input.IsKeyDown(Keys.RightShift));
        _io.AddKeyEvent(ImGuiKey.ModCtrl, _input.IsKeyDown(Keys.LeftCtrl) || _input.IsKeyDown(Keys.RightCtrl));
        _io.AddKeyEvent(ImGuiKey.ModSuper, _input.IsKeyDown(Keys.LeftWin) || _input.IsKeyDown(Keys.RightWin));
    }

    /// <summary>
    /// Ends the ImGui frame begun in <see cref="Update"/>, uploads any textures ImGui requested and renders the draw
    /// lists on top of everything else drawn this frame.
    /// </summary>
    public override void EndDraw()
    {
        // Nothing to present when the draw is not paired with an update - Render() without a preceding
        // NewFrame() asserts just as loudly as the reverse.
        if (!_frameBegun) return;

        Hexa.NET.ImGui.ImGui.Render();
        _frameBegun = false;

        var drawData = Hexa.NET.ImGui.ImGui.GetDrawData();

        _textures.ProcessTextureUpdates(drawData);
        RenderDrawLists(drawData);
        ImGuiExtension.ClearTextures();
    }

    // Grows the buffers, with headroom, when this frame's draw data does not fit
    private void CheckBuffers(ImDrawDataPtr drawData)
    {
        var vertexBytes = (uint)(drawData.TotalVtxCount * Unsafe.SizeOf<ImDrawVert>());

        if (vertexBytes > _vertexBinding.Buffer.SizeInBytes)
        {
            _vertexBinding.Buffer.Dispose();
            _vertexBinding = new VertexBufferBinding(Stride.Graphics.Buffer.Vertex.New(_device, (int)(vertexBytes * 1.5f)), _vertexLayout, 0);
        }

        var indexBytes = (uint)(drawData.TotalIdxCount * sizeof(ushort));

        if (indexBytes > _indexBinding.Buffer.SizeInBytes)
        {
            _indexBinding.Buffer.Dispose();
            _indexBinding = new IndexBufferBinding(Stride.Graphics.Buffer.Index.New(_device, (int)(indexBytes * 1.5f)), Is32BitIndices, 0);
        }
    }

    private unsafe void UpdateBuffers(ImDrawDataPtr drawData)
    {
        var vtxOffsetBytes = 0;
        var idxOffsetBytes = 0;

        for (var n = 0; n < drawData.CmdListsCount; n++)
        {
            ImDrawListPtr cmdList = drawData.CmdLists[n];

            _vertexBinding.Buffer.SetData(_commandList, new ReadOnlySpan<ImDrawVert>(cmdList.VtxBuffer.Data, cmdList.VtxBuffer.Size), vtxOffsetBytes);
            _indexBinding.Buffer.SetData(_commandList, new ReadOnlySpan<ushort>(cmdList.IdxBuffer.Data, cmdList.IdxBuffer.Size), idxOffsetBytes);
            vtxOffsetBytes += cmdList.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>();
            idxOffsetBytes += cmdList.IdxBuffer.Size * sizeof(ushort);
        }
    }

    private void RenderDrawLists(ImDrawDataPtr drawData)
    {
        var surfaceSize = _game.Window.ClientBounds;
        var projMatrix = Matrix.OrthoRH(surfaceSize.Width, -surfaceSize.Height, -1, 1);

        CheckBuffers(drawData);
        UpdateBuffers(drawData);

        _commandList.SetPipelineState(_pipeline);
        _commandList.SetVertexBuffer(0, _vertexBinding.Buffer, 0, Unsafe.SizeOf<ImDrawVert>());
        _commandList.SetIndexBuffer(_indexBinding.Buffer, 0, Is32BitIndices);

        // The projection and colour space hold for the whole frame. ImGui's colours are sRGB bytes; the
        // shader decodes them only when the device says the backbuffer is sRGB (the default).
        _shader.Parameters.Set(ImGuiShaderKeys.proj, ref projMatrix);
        _shader.Parameters.Set(ImGuiShaderKeys.LinearOutput, _device.ColorSpace == ColorSpace.Linear ? 1u : 0u);

        // Seed with the first available managed texture (font atlas) as the initial shader binding
        if (_textures.FirstTexture() is { } fontAtlas) _shader.Parameters.Set(ImGuiShaderKeys.tex, fontAtlas);

        var vtxOffset = 0;
        var idxOffset = 0;

        for (var n = 0; n < drawData.CmdListsCount; n++)
        {
            ImDrawListPtr cmdList = drawData.CmdLists[n];

            for (var i = 0; i < cmdList.CmdBuffer.Size; i++)
            {
                ImDrawCmd cmd = cmdList.CmdBuffer[i];

                // Managed textures (the font atlas, ImGui's own) have high-bit ids; user textures registered
                // through ImGuiExtension use small sequential ids
                var texId = cmd.TexRef.GetTexID();

                if (_textures.TryGet(texId, out var managedTexture)) _shader.Parameters.Set(ImGuiShaderKeys.tex, managedTexture);
                else if (ImGuiExtension.TryGetTexture((ulong)(nint)texId, out var userTexture)) _shader.Parameters.Set(ImGuiShaderKeys.tex, userTexture);

                _commandList.SetScissorRectangle(new Rectangle((int)cmd.ClipRect.X, (int)cmd.ClipRect.Y, (int)(cmd.ClipRect.Z - cmd.ClipRect.X), (int)(cmd.ClipRect.W - cmd.ClipRect.Y)));

                _shader.Apply(_context);
                _commandList.DrawIndexed((int)cmd.ElemCount, idxOffset, vtxOffset);

                idxOffset += (int)cmd.ElemCount;
            }

            vtxOffset += cmdList.VtxBuffer.Size;
        }
    }
}