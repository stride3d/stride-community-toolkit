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
    /// <summary>
    /// The ImGui context this system created and renders. Pass it to add-on libraries (ImNodes, ImPlot) that need to
    /// share the context.
    /// </summary>
    public readonly ImGuiContextPtr ImGuiContext;

    /// <summary>
    /// A UI scale factor windows can read through <see cref="BaseWindow.Scale"/> to size themselves for the display's DPI.
    /// Defaults to <c>1</c>; it is not applied automatically.
    /// </summary>
    public float Scale
    {
        get => _scale;
        set => _scale = value;
    }
    private float _scale = 1;

    const int INITIAL_VERTEX_BUFFER_SIZE = 128;
    const int INITIAL_INDEX_BUFFER_SIZE = 128;

    private ImGuiIOPtr _io;
    private ImGuiPlatformIOPtr _platform;

    // dependencies
    private readonly IGame _game;
    private readonly InputManager input;
    private readonly GraphicsDevice device;
    private readonly GraphicsDeviceManager deviceManager;
    private readonly GraphicsContext context;
    private readonly EffectSystem effectSystem;
    private readonly CommandList commandList;

    // device objects
    private PipelineState imPipeline;
    private VertexDeclaration imVertLayout;
    private VertexBufferBinding vertexBinding;
    private IndexBufferBinding indexBinding;
    private EffectInstance imShader;
    private readonly Dictionary<ImTextureID, Texture> _managedTextures = new();

    private Dictionary<Keys, ImGuiKey> _keys = [];
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
        input = inputManager ?? Services.GetService<InputManager>() ?? throw new InvalidOperationException("ImGuiSystem: InputManager must be available!");
        deviceManager = graphicsDeviceManager;
        device = deviceManager.GraphicsDevice ?? throw new InvalidOperationException("ImGuiSystem: GraphicsDevice must be available!");
        context = Services.GetService<GraphicsContext>() ?? throw new InvalidOperationException("ImGuiSystem: GraphicsContext must be available!");
        effectSystem = Services.GetService<EffectSystem>() ?? throw new InvalidOperationException("ImGuiSystem: EffectSystem must be available!");
        commandList = context.CommandList;

        ImGuiContext = CreateContext();
        SetCurrentContext(ImGuiContext);

        _io = GetIO();
        _platform = GetPlatformIO();

        // SETTO
        SetupInput();

        // vbos etc
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
        foreach (var texture in _managedTextures.Values)
            texture.Dispose();
        _managedTextures.Clear();
        vertexBinding.Buffer?.Dispose();
        indexBinding?.Buffer?.Dispose();
        imPipeline?.Dispose();
        imShader?.Dispose();
        DestroyContext(ImGuiContext);
        base.Destroy();
    }

    unsafe void SetupInput()
    {
        // keyboard nav yes
        _io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;

        _keys.Add(Keys.Tab, ImGuiKey.Tab);
        _keys.Add(Keys.Left, ImGuiKey.LeftArrow);
        _keys.Add(Keys.Right, ImGuiKey.RightArrow);
        _keys.Add(Keys.Up, ImGuiKey.UpArrow);
        _keys.Add(Keys.Down, ImGuiKey.DownArrow);
        _keys.Add(Keys.PageUp, ImGuiKey.PageUp);
        _keys.Add(Keys.PageDown, ImGuiKey.PageDown);
        _keys.Add(Keys.Home, ImGuiKey.Home);
        _keys.Add(Keys.End, ImGuiKey.End);
        _keys.Add(Keys.Delete, ImGuiKey.Delete);
        _keys.Add(Keys.Back, ImGuiKey.Backspace);
        _keys.Add(Keys.Enter, ImGuiKey.Enter);
        _keys.Add(Keys.Escape, ImGuiKey.Escape);
        _keys.Add(Keys.Space, ImGuiKey.Space);
        _keys.Add(Keys.A, ImGuiKey.A);
        _keys.Add(Keys.C, ImGuiKey.C);
        _keys.Add(Keys.V, ImGuiKey.V);
        _keys.Add(Keys.X, ImGuiKey.X);
        _keys.Add(Keys.Y, ImGuiKey.Y);
        _keys.Add(Keys.Z, ImGuiKey.Z);

        setClipboardFn = SetClipboard;
        getClipboardFn = GetClipboard;

        _platform.PlatformSetClipboardTextFn = (void*)Marshal.GetFunctionPointerForDelegate(setClipboardFn);
        _platform.PlatformGetClipboardTextFn = (void*)Marshal.GetFunctionPointerForDelegate(getClipboardFn);
    }

    [FixedAddressValueType]
    static SetClipboardDelegate? setClipboardFn;

    [FixedAddressValueType]
    static GetClipboardDelegate? getClipboardFn;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    unsafe delegate void SetClipboardDelegate(ImGuiContextPtr ctx, byte* text);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    unsafe delegate byte* GetClipboardDelegate(ImGuiContextPtr ctx);

    unsafe void SetClipboard(ImGuiContextPtr ctx, byte* text)
    {
    }

    unsafe byte* GetClipboard(ImGuiContextPtr ctx)
    {
        return (byte*)_platform.PlatformClipboardUserData;
    }

    [MemberNotNull(nameof(imShader), nameof(imVertLayout), nameof(imPipeline), nameof(indexBinding))]
    void CreateDeviceObjects()
    {
        // compile de shader
        imShader = new EffectInstance(effectSystem.LoadEffect("ImGuiShader").WaitForResult());
        imShader.UpdateEffect(device);

        var layout = new VertexDeclaration(
            VertexElement.Position<Vector2>(),
            VertexElement.TextureCoordinate<Vector2>(),
            VertexElement.Color(PixelFormat.R8G8B8A8_UNorm)
        );

        imVertLayout = layout;

        // de pipeline desc
        var pipeline = new PipelineStateDescription()
        {
            BlendState = BlendStates.NonPremultiplied,

            RasterizerState = new RasterizerStateDescription()
            {
                CullMode = CullMode.None,
                DepthBias = 0,
                FillMode = FillMode.Solid,
                MultisampleAntiAliasLine = false,
                ScissorTestEnable = true,
                SlopeScaleDepthBias = 0,
            },

            PrimitiveType = PrimitiveType.TriangleList,
            InputElements = imVertLayout.CreateInputElements(),
            DepthStencilState = DepthStencilStates.Default,

            EffectBytecode = imShader.Effect.Bytecode,
            RootSignature = imShader.RootSignature,

            Output = new RenderOutputDescription(PixelFormat.R8G8B8A8_UNorm)
        };

        // finally set up the pipeline
        var pipelineState = PipelineState.New(device, pipeline);
        imPipeline = pipelineState;

        var is32Bits = false;
        var indexBuffer = Stride.Graphics.Buffer.Index.New(device, INITIAL_INDEX_BUFFER_SIZE * sizeof(ushort), GraphicsResourceUsage.Dynamic);
        var indexBufferBinding = new IndexBufferBinding(indexBuffer, is32Bits, 0);
        indexBinding = indexBufferBinding;

        // BufferFlags is passed explicitly: without it, C# picks the generic New<T>(device, ref readonly T value, usage)
        // overload with T = int (it needs no default argument) and creates a 4-byte buffer holding the size value.
        var vertexBuffer = Stride.Graphics.Buffer.Vertex.New(device, INITIAL_VERTEX_BUFFER_SIZE * imVertLayout.CalculateSize(), GraphicsResourceUsage.Dynamic, BufferFlags.None);
        var vertexBufferBinding = new VertexBufferBinding(vertexBuffer, layout, 0);
        vertexBinding = vertexBufferBinding;
    }

    // Dear ImGui 1.92+ texture event handlers (RendererHasTextures protocol)

    unsafe void ProcessTextureUpdates(ImDrawDataPtr drawData)
    {
        if (drawData.Handle->Textures == null) return;
        var textures = drawData.Textures;
        for (int i = 0; i < textures.Size; i++)
        {
            ImTextureDataPtr textureData = textures.Data[i];
            switch (textureData.Status)
            {
                case ImTextureStatus.WantCreate:
                    CreateManagedTexture(textureData);
                    break;
                case ImTextureStatus.WantUpdates:
                    UpdateManagedTexture(textureData);
                    break;
                case ImTextureStatus.WantDestroy:
                    DestroyManagedTexture(textureData);
                    break;
            }
        }
    }

    unsafe void CreateManagedTexture(ImTextureDataPtr textureData)
    {
        var pixelFormat = textureData.Format == ImTextureFormat.Rgba32
            ? PixelFormat.R8G8B8A8_UNorm
            : PixelFormat.R8_UNorm;
        var newTexture = Texture.New2D(device, textureData.Width, textureData.Height, pixelFormat, TextureFlags.ShaderResource);
        newTexture.SetData(commandList, new ReadOnlySpan<byte>(textureData.Pixels, textureData.GetSizeInBytes()));

        // Use high-bit sentinel to distinguish ImGui-managed IDs from ImGuiExtension user-texture IDs (which start from 1)
        var managedId = (ImTextureID)(nint)(0x80000000u | (uint)textureData.UniqueID);
        textureData.SetTexID(managedId);
        _managedTextures[managedId] = newTexture;
        textureData.SetStatus(ImTextureStatus.Ok);
    }

    unsafe void UpdateManagedTexture(ImTextureDataPtr textureData)
    {
        var texId = textureData.GetTexID();
        if (_managedTextures.TryGetValue(texId, out var existing))
        {
            var pixelFormat = textureData.Format == ImTextureFormat.Rgba32
                ? PixelFormat.R8G8B8A8_UNorm
                : PixelFormat.R8_UNorm;
            if (existing.Width != textureData.Width || existing.Height != textureData.Height)
            {
                existing.Dispose();
                var newTexture = Texture.New2D(device, textureData.Width, textureData.Height, pixelFormat, TextureFlags.ShaderResource);
                newTexture.SetData(commandList, new ReadOnlySpan<byte>(textureData.Pixels, textureData.GetSizeInBytes()));
                _managedTextures[texId] = newTexture;
            }
            else
            {
                existing.SetData(commandList, new ReadOnlySpan<byte>(textureData.Pixels, textureData.GetSizeInBytes()));
            }
        }
        textureData.SetStatus(ImTextureStatus.Ok);
    }

    void DestroyManagedTexture(ImTextureDataPtr textureData)
    {
        var texId = textureData.GetTexID();
        if (_managedTextures.TryGetValue(texId, out var texture))
        {
            texture.Dispose();
            _managedTextures.Remove(texId);
        }
        textureData.SetStatus(ImTextureStatus.Ok);
    }

    /// <summary>
    /// Forwards this frame's input and display size to ImGui and begins a new ImGui frame. Windows build their UI in
    /// their own <c>Update</c>, which runs after this one.
    /// </summary>
    /// <inheritdoc />
    public override void Update(GameTime gameTime)
    {
        var deltaTime = (float)gameTime.Elapsed.TotalSeconds;
        if (_isFirstFrame)
        {
            _isFirstFrame = false;
            deltaTime = 1 / 60f;
        }
        var surfaceSize = _game.Window.ClientBounds;
        _io.DisplaySize = new System.Numerics.Vector2(surfaceSize.Width, surfaceSize.Height);
        _io.DeltaTime = deltaTime;

        if (input.HasMouse == false || input.IsMousePositionLocked == false)
        {
            var mousePos = input.AbsoluteMousePosition;
            _io.AddMousePosEvent(mousePos.X, mousePos.Y);

            if (_io.WantTextInput)
            {
                input.TextInput.EnabledTextInput();
            }
            else
            {
                input.TextInput.DisableTextInput();
            }

            // handle input events
            foreach (InputEvent ev in input.Events)
            {
                switch (ev)
                {
                    case TextInputEvent tev:
                        if (tev.Text == "\t") continue;
                        _io.AddInputCharactersUTF8(tev.Text);
                        break;
                    case KeyEvent kev:
                        if (_keys.TryGetValue(kev.Key, out var imGuiKey))
                            _io.AddKeyEvent(imGuiKey, input.IsKeyDown(kev.Key));
                        break;
                    case MouseWheelEvent mw:
                        _io.AddMouseWheelEvent(0, mw.WheelDelta);
                        break;
                }
            }

            _io.AddMouseButtonEvent(0, input.IsMouseButtonDown(MouseButton.Left));
            _io.AddMouseButtonEvent(1, input.IsMouseButtonDown(MouseButton.Right));
            _io.AddMouseButtonEvent(2, input.IsMouseButtonDown(MouseButton.Middle));

            _io.AddKeyEvent(ImGuiKey.ModAlt, input.IsKeyDown(Keys.LeftAlt) || input.IsKeyDown(Keys.RightAlt));
            _io.AddKeyEvent(ImGuiKey.ModShift, input.IsKeyDown(Keys.LeftShift) || input.IsKeyDown(Keys.RightShift));
            _io.AddKeyEvent(ImGuiKey.ModCtrl, input.IsKeyDown(Keys.LeftCtrl) || input.IsKeyDown(Keys.RightCtrl));
            _io.AddKeyEvent(ImGuiKey.ModSuper, input.IsKeyDown(Keys.LeftWin) || input.IsKeyDown(Keys.RightWin));
        }
        // An update that never reached a draw left a frame open. Close it before starting the next one,
        // discarding its draw data - the frame it belonged to is not being presented anyway.
        if (_frameBegun)
        {
            Hexa.NET.ImGui.ImGui.EndFrame();
        }

        Hexa.NET.ImGui.ImGui.NewFrame();
        _frameBegun = true;
    }

    /// <summary>
    /// Ends the ImGui frame begun in <see cref="Update"/>, uploads any textures ImGui requested and renders the draw
    /// lists on top of everything else drawn this frame.
    /// </summary>
    public override void EndDraw()
    {
        // Nothing to present when the draw is not paired with an update - Render() without a preceding
        // NewFrame() asserts just as loudly as the reverse.
        if (!_frameBegun)
        {
            return;
        }

        Hexa.NET.ImGui.ImGui.Render();
        _frameBegun = false;

        var drawData = Hexa.NET.ImGui.ImGui.GetDrawData();
        ProcessTextureUpdates(drawData);
        RenderDrawLists(drawData);
        ImGuiExtension.ClearTextures();
    }

    void CheckBuffers(ImDrawDataPtr drawData)
    {
        uint totalVBOSize = (uint)(drawData.TotalVtxCount * Unsafe.SizeOf<ImDrawVert>());
        if (totalVBOSize > vertexBinding.Buffer.SizeInBytes)
        {
            vertexBinding.Buffer.Dispose();
            var vertexBuffer = Stride.Graphics.Buffer.Vertex.New(device, (int)(totalVBOSize * 1.5f));
            vertexBinding = new VertexBufferBinding(vertexBuffer, imVertLayout, 0);
        }

        uint totalIBOSize = (uint)(drawData.TotalIdxCount * sizeof(ushort));
        if (totalIBOSize > indexBinding.Buffer.SizeInBytes)
        {
            indexBinding.Buffer.Dispose();
            var is32Bits = false;
            var indexBuffer = Stride.Graphics.Buffer.Index.New(device, (int)(totalIBOSize * 1.5f));
            indexBinding = new IndexBufferBinding(indexBuffer, is32Bits, 0);
        }
    }

    unsafe void UpdateBuffers(ImDrawDataPtr drawData)
    {
        // copy de dators
        int vtxOffsetBytes = 0;
        int idxOffsetBytes = 0;

        for (int n = 0; n < drawData.CmdListsCount; n++)
        {
            ImDrawListPtr cmdList = drawData.CmdLists[n];
            vertexBinding.Buffer.SetData(commandList, new ReadOnlySpan<ImDrawVert>(cmdList.VtxBuffer.Data, cmdList.VtxBuffer.Size), vtxOffsetBytes);
            indexBinding.Buffer.SetData(commandList, new ReadOnlySpan<ushort>(cmdList.IdxBuffer.Data, cmdList.IdxBuffer.Size), idxOffsetBytes);
            vtxOffsetBytes += cmdList.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>();
            idxOffsetBytes += cmdList.IdxBuffer.Size * sizeof(ushort);
        }
    }

    void RenderDrawLists(ImDrawDataPtr drawData)
    {
        // view proj
        var surfaceSize = _game.Window.ClientBounds;
        var projMatrix = Matrix.OrthoRH(surfaceSize.Width, -surfaceSize.Height, -1, 1);

        CheckBuffers(drawData); // potentially resize buffers first if needed
        UpdateBuffers(drawData); // updeet em now

        // set pipeline stuff
        var is32Bits = false;
        commandList.SetPipelineState(imPipeline);
        commandList.SetVertexBuffer(0, vertexBinding.Buffer, 0, Unsafe.SizeOf<ImDrawVert>());
        commandList.SetIndexBuffer(indexBinding.Buffer, 0, is32Bits);

        // Seed with the first available managed texture (font atlas) as the initial shader binding
        Texture? currentTexture = null;
        foreach (var t in _managedTextures.Values) { currentTexture = t; break; }
        if (currentTexture != null)
            imShader.Parameters.Set(ImGuiShaderKeys.tex, currentTexture);

        int vtxOffset = 0;
        int idxOffset = 0;
        for (int n = 0; n < drawData.CmdListsCount; n++)
        {
            ImDrawListPtr cmdList = drawData.CmdLists[n];

            for (int i = 0; i < cmdList.CmdBuffer.Size; i++)
            {
                ImDrawCmd cmd = cmdList.CmdBuffer[i];

                // Resolve the texture for this draw command:
                // managed (font atlas, ImGui-internal) textures have high-bit IDs;
                // user textures registered via ImGuiExtension use small sequential IDs.
                var texId = cmd.TexRef.GetTexID();
                if (_managedTextures.TryGetValue(texId, out var managedTexture))
                {
                    imShader.Parameters.Set(ImGuiShaderKeys.tex, managedTexture);
                }
                else if (ImGuiExtension.TryGetTexture((ulong)(nint)texId, out var userTexture))
                {
                    imShader.Parameters.Set(ImGuiShaderKeys.tex, userTexture);
                }

                // Set the scissor rectangle for clipping
                commandList.SetScissorRectangle(
                    new Rectangle(
                        (int)cmd.ClipRect.X,
                        (int)cmd.ClipRect.Y,
                        (int)(cmd.ClipRect.Z - cmd.ClipRect.X),
                        (int)(cmd.ClipRect.W - cmd.ClipRect.Y)
                    )
                );

                // Set the projection matrix and apply shader
                imShader.Parameters.Set(ImGuiShaderKeys.proj, ref projMatrix);
                imShader.Apply(context);

                // Draw the indexed vertices
                commandList.DrawIndexed((int)cmd.ElemCount, idxOffset, vtxOffset);

                idxOffset += (int)cmd.ElemCount;

            }

            vtxOffset += cmdList.VtxBuffer.Size;
        }
    }
}