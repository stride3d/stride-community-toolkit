using Stride.CommunityToolkit.Scripts.Utilities;
using Stride.Engine;
using Stride.Input;

namespace Stride.CommunityToolkit.Scripts;

/// <summary>
/// Provides an interactive 2D camera controller for navigating 2D scenes in Stride.
/// This controller supports movement in the XY-plane using the arrow keys (optionally WASD),
/// zooming in and out with the mouse wheel, middle-mouse drag panning, optional screen edge panning,
/// camera following, and smooth movement. Additional features include a speed boost when holding shift
/// and the ability to reset the camera to a default position and zoom level using the 'H' key.
/// </summary>
/// <remarks>
/// - The camera moves at a configurable speed which can be increased with shift keys.
/// - Zooming scales the camera's OrthographicSize by a fixed fraction per mouse-wheel notch; shift zooms faster too.
/// - Optional features: screen edge movement, camera bounds, follow target, smooth movement, mouse drag panning.
/// - The 'H' key resets the camera to its default position and orthographic size.
/// - Default settings: FarClipPlane=1000, NearClipPlane=0.1f, OrthographicSize=10.
/// </remarks>
public class Basic2DCameraController : SyncScript
{
    // Movement Properties
    /// <summary>
    /// Gets or sets the base speed of camera movement in units per second.
    /// </summary>
    /// <remarks>
    /// This value is multiplied by <see cref="SpeedFactor"/> when shift keys are held.
    /// </remarks>
    public float CameraMoveSpeed { get; set; } = 5.0f;

    /// <summary>
    /// Gets or sets whether W, A, S and D move the camera in addition to the arrow keys. Defaults to <see langword="false"/>.
    /// </summary>
    /// <remarks>
    /// Off by default so that a game built on top of the toolkit keeps WASD for itself; the arrow keys
    /// are always active. Turn it on for tools and playgrounds where nothing else wants those keys.
    /// </remarks>
    public bool EnableWasdMovement { get; set; } = false;

    /// <summary>
    /// Gets or sets the speed multiplier applied when holding shift keys.
    /// </summary>
    /// <remarks>
    /// The effective movement speed becomes <see cref="CameraMoveSpeed"/> * <see cref="SpeedFactor"/> when either shift key is pressed,
    /// and each mouse-wheel notch counts as <see cref="SpeedFactor"/> notches of <see cref="ZoomStep"/>.
    /// </remarks>
    public float SpeedFactor { get; set; } = 5.0f;

    // Zoom Properties
    /// <summary>
    /// Gets or sets the default orthographic size used when resetting the camera with the 'H' key.
    /// </summary>
    public float OrthographicSizeDefault { get; set; } = 10.0f;

    /// <summary>
    /// Gets or sets the fraction by which the visible area scales per mouse-wheel notch. Defaults to 0.1 (10 %).
    /// </summary>
    /// <remarks>
    /// Zoom is multiplicative, so every notch changes the view by the same proportion whether the camera is
    /// zoomed far in or far out. Wheel input is an impulse rather than a held key, so it is not scaled by
    /// delta time. Holding shift multiplies the notch count by <see cref="SpeedFactor"/>.
    /// </remarks>
    public float ZoomStep { get; set; } = 0.1f;

    /// <summary>
    /// Gets or sets the minimum orthographic size, representing maximum zoom in.
    /// </summary>
    public float MinOrthographicSize { get; set; } = 0.1f;

    /// <summary>
    /// Gets or sets the maximum orthographic size, representing maximum zoom out.
    /// </summary>
    public float MaxOrthographicSize { get; set; } = 100.0f;

    // Screen Edge Movement Properties
    /// <summary>
    /// Gets or sets whether RTS-style screen edge panning is enabled.
    /// </summary>
    /// <remarks>
    /// When enabled, moving the mouse cursor near screen edges will pan the camera in that direction.
    /// </remarks>
    public bool EnableScreenEdgeMovement { get; set; } = false;

    /// <summary>
    /// Gets or sets the width in pixels of the screen edge border that triggers camera movement.
    /// </summary>
    /// <remarks>
    /// Only applies when <see cref="EnableScreenEdgeMovement"/> is true.
    /// </remarks>
    public float ScreenEdgeBorderWidth { get; set; } = 10.0f;

    // Camera Bounds Properties
    /// <summary>
    /// Gets or sets whether camera position bounds limiting is enabled.
    /// </summary>
    /// <remarks>
    /// When enabled, the camera position is constrained between <see cref="MinBounds"/> and <see cref="MaxBounds"/>.
    /// </remarks>
    public bool EnableBounds { get; set; } = false;

    /// <summary>
    /// Gets or sets the minimum camera position bounds in the XY-plane.
    /// </summary>
    /// <remarks>
    /// Only applies when <see cref="EnableBounds"/> is true.
    /// </remarks>
    public Vector2 MinBounds { get; set; } = new(-100, -100);

    /// <summary>
    /// Gets or sets the maximum camera position bounds in the XY-plane.
    /// </summary>
    /// <remarks>
    /// Only applies when <see cref="EnableBounds"/> is true.
    /// </remarks>
    public Vector2 MaxBounds { get; set; } = new(100, 100);

    // Camera Follow Properties
    /// <summary>
    /// Gets or sets the entity for the camera to follow.
    /// </summary>
    /// <remarks>
    /// When set, the camera will automatically track this entity's position, applying <see cref="FollowOffset"/> and <see cref="FollowSmoothing"/>.
    /// Manual camera controls are disabled while following a target.
    /// </remarks>
    public Entity? FollowTarget { get; set; } = null;

    /// <summary>
    /// Gets or sets the offset from the follow target's position.
    /// </summary>
    /// <remarks>
    /// This offset is added to the <see cref="FollowTarget"/> position when calculating the camera's target position.
    /// </remarks>
    public Vector3 FollowOffset { get; set; } = Vector3.Zero;

    /// <summary>
    /// Gets or sets the smoothing factor for the camera follow movement.
    /// </summary>
    /// <remarks>
    /// A value of 0 results in instant following, while higher values produce smoother, more gradual movement.
    /// </remarks>
    public float FollowSmoothing { get; set; } = 5.0f;

    // Smooth Movement Properties
    /// <summary>
    /// Gets or sets whether smooth camera movement with linear interpolation is enabled.
    /// </summary>
    /// <remarks>
    /// When enabled, camera movement is smoothed using lerp based on <see cref="SmoothingSpeed"/>.
    /// </remarks>
    public bool EnableSmoothing { get; set; } = false;

    /// <summary>
    /// Gets or sets the speed of smooth movement interpolation.
    /// </summary>
    /// <remarks>
    /// Higher values result in faster interpolation towards the target position. Only applies when <see cref="EnableSmoothing"/> is true.
    /// </remarks>
    public float SmoothingSpeed { get; set; } = 10.0f;

    // Mouse Drag Panning Properties
    /// <summary>
    /// Gets or sets whether mouse drag panning is enabled. Defaults to <see langword="true"/>.
    /// </summary>
    /// <remarks>
    /// When enabled, holding <see cref="MouseDragButton"/> and moving the mouse drags the world with the
    /// cursor: the point under the cursor stays under the cursor.
    /// </remarks>
    public bool EnableMouseDragPan { get; set; } = true;

    /// <summary>
    /// Gets or sets the mouse button used for drag panning.
    /// </summary>
    /// <remarks>
    /// Only applies when <see cref="EnableMouseDragPan"/> is true.
    /// </remarks>
    public MouseButton MouseDragButton { get; set; } = MouseButton.Middle;

    /// <summary>
    /// Gets or sets whether on-screen camera instructions are displayed.
    /// </summary>
    /// <remarks>
    /// This hides only the camera's own lines. The overlay itself, and anything else contributing to
    /// it, is toggled with <see cref="DebugOverlay.ToggleKey"/>.
    /// </remarks>
    public bool ShowInstructions
    {
        get => _instructions?.Enabled ?? false;
        set { if (_instructions is not null) _instructions.Enabled = value; }
    }

    private CameraComponent? _camera;
    private Vector3 _defaultCameraPosition;
    private Vector3 _targetPosition;
    private float _targetOrthographicSize;
    private Vector2? _lastMousePosition;
    private float _defaultZ = 0;

    private DebugOverlaySection? _instructions;

    /// <summary>
    /// Seconds elapsed since the previous update, for frame-rate independent movement.
    /// </summary>
    private float DeltaTime => (float)Game.UpdateTime.Elapsed.TotalSeconds;

    /// <summary>
    /// Gets or sets the key that collapses and expands the camera's help. Defaults to
    /// <see cref="Keys.F2"/>. Must be set before the script starts.
    /// </summary>
    public Keys HelpToggleKey { get; set; } = Keys.F2;

    /// <summary>
    /// Gets or sets whether the camera's help starts collapsed to its title line, leaving a one-line
    /// reminder of the key rather than the full list. Defaults to <see langword="true"/>, and must be
    /// set before the script starts.
    /// </summary>
    /// <remarks>
    /// Collapsed by default because these keys are the same in every scene and stop being worth
    /// several lines of screen space almost immediately, while whatever the scene itself has to say
    /// does not. The remaining line names the key, so nothing is hidden without a way back.
    /// </remarks>
    public bool HelpCollapsed { get; set; } = true;

    /// <summary>
    /// Initializes the camera controller by setting up the instruction overlay and caching the initial state.
    /// </summary>
    /// <remarks>
    /// This method sets the target position to the current camera position and configures the debug text printer
    /// for displaying on-screen instructions. Called once when the script starts.
    /// The position captured here is the one the 'H' key restores, so a camera created at a custom
    /// position resets to that position rather than to a fixed default.
    /// </remarks>
    public override void Start()
    {
        _defaultCameraPosition = Entity.Transform.Position;
        _targetPosition = Entity.Transform.Position;
        _defaultZ = Entity.Transform.Position.Z;

        _instructions = DebugOverlay.GetOrCreate(Game).AddCollapsibleSection(
            "Camera", "Camera controls", HelpToggleKey, () =>
            {
                var lines = new List<TextElement>
                {
                    new("F3: Reposition Help", Color.LightGoldenrodYellow),
                    new(EnableWasdMovement ? "WASD / Arrow Keys: Move" : "Arrow Keys: Move"),
                    new("Hold Shift: Increase speed"),
                    new("Mouse Wheel: Zoom"),
                };

                if (EnableMouseDragPan)
                    lines.Add(new($"{MouseDragButton} Mouse Drag: Pan"));

                lines.Add(new("H: Reset Camera"));

                return lines;
            }, HelpCollapsed, order: -100);
    }

    /// <summary>
    /// Updates the camera controller state every frame, handling movement, zoom, following, bounds, and instruction display.
    /// </summary>
    /// <remarks>
    /// <para>The update order is as follows:</para>
    /// <list type="number">
    /// <item><description>Cache the camera component reference if not already cached.</description></item>
    /// <item><description>Process instruction toggle keys (F2, F3).</description></item>
    /// <item><description>Process camera follow if <see cref="FollowTarget"/> is set, otherwise process manual controls (movement, screen edge, mouse drag).</description></item>
    /// <item><description>Process camera zoom via mouse wheel.</description></item>
    /// <item><description>Check for camera reset (H key).</description></item>
    /// <item><description>Apply smooth movement if <see cref="EnableSmoothing"/> is enabled.</description></item>
    /// <item><description>Apply camera bounds if <see cref="EnableBounds"/> is enabled.</description></item>
    /// <item><description>Display instructions if visible.</description></item>
    /// </list>
    /// </remarks>
    public override void Update()
    {
        if (_camera is null)
        {
            _camera = Entity.Get<CameraComponent>();

            if (_camera is null) return; // Ensure we have a camera component

            _targetOrthographicSize = _camera.OrthographicSize;
        }


        // Process follow target first (the highest priority)
        if (FollowTarget is null)
        {
            // Only process manual controls if not following a target
            ProcessCameraMovement();

            if (EnableScreenEdgeMovement)
                ProcessScreenEdgeMovement();

            if (EnableMouseDragPan)
                ProcessMouseDragPan();
        }
        else
        {
            ProcessCameraFollow();
        }

        ProcessCameraZoom();

        ResetCameraToDefault();

        // Apply smooth movement or direct movement
        if (EnableSmoothing)
            ApplySmoothMovement();

        // Apply camera bounds if enabled
        if (EnableBounds)
            ApplyCameraBounds();

    }

    /// <summary>
    /// Handles keyboard input for toggling instruction visibility and repositioning the instruction overlay.
    /// </summary>
    /// <remarks>
    /// <para>Supported keys:</para>
    /// <list type="bullet">
    /// <item><description>F2: Toggles the visibility of on-screen instructions.</description></item>
    /// <item><description>F3: Changes the position of the instruction overlay on screen.</description></item>
    /// </list>
    /// </remarks>

    /// <summary>
    /// Processes keyboard-driven camera translation.
    /// </summary>
    private void ProcessCameraMovement()
    {
        var moveDirection = Vector3.Zero;

        // Update moveDirection based on key input
        if (Input.IsKeyDown(Keys.Up) || (EnableWasdMovement && Input.IsKeyDown(Keys.W)))
            moveDirection.Y++;
        if (Input.IsKeyDown(Keys.Down) || (EnableWasdMovement && Input.IsKeyDown(Keys.S)))
            moveDirection.Y--;
        if (Input.IsKeyDown(Keys.Left) || (EnableWasdMovement && Input.IsKeyDown(Keys.A)))
            moveDirection.X--;
        if (Input.IsKeyDown(Keys.Right) || (EnableWasdMovement && Input.IsKeyDown(Keys.D)))
            moveDirection.X++;

        // Normalize the moveDirection to ensure consistent movement speed, for example, when moving diagonally
        if (moveDirection.LengthSquared() > 1)
            moveDirection.Normalize();

        // Apply a speed factor when shift is held
        if (Input.IsKeyDown(Keys.LeftShift) || Input.IsKeyDown(Keys.RightShift))
            moveDirection *= SpeedFactor;

        // Apply movement to the target position or directly to the camera
        var movement = moveDirection * CameraMoveSpeed * DeltaTime;
        if (EnableSmoothing)
        {
            _targetPosition += movement;
        }
        else
        {
            Entity.Transform.Position += movement;
        }
    }

    /// <summary>
    /// Moves camera when the mouse is near screen edges (RTS-style panning).
    /// </summary>
    private void ProcessScreenEdgeMovement()
    {
        var moveDirection = Vector3.Zero;
        var mousePosition = Input.MousePosition;

        // Calculate the screen dimensions
        var screenWidth = Game.GraphicsDevice.Presenter.BackBuffer.Width;
        var screenHeight = Game.GraphicsDevice.Presenter.BackBuffer.Height;

        // Convert normalized mouse coordinates to screen coordinates
        var screenMouseX = mousePosition.X * screenWidth;
        var screenMouseY = mousePosition.Y * screenHeight;

        // Check if the mouse is within the screen bounds. We are detecting -1 because the mouse keeps detected outside the screen
        if (screenMouseX > 0 && screenMouseX < screenWidth - 1 && screenMouseY > 0 && screenMouseY < screenHeight - 1)
        {
            // Check if the mouse is near the edges of the screen and update moveDirection accordingly
            if (screenMouseX < ScreenEdgeBorderWidth)
                moveDirection.X--;
            if (screenMouseX > screenWidth - ScreenEdgeBorderWidth)
                moveDirection.X++;
            if (screenMouseY < ScreenEdgeBorderWidth)
                moveDirection.Y++;
            if (screenMouseY > screenHeight - ScreenEdgeBorderWidth)
                moveDirection.Y--;
        }

        if (moveDirection.LengthSquared() > 1)
            moveDirection.Normalize();

        // Apply movement to the target position or directly to the camera
        var movement = moveDirection * CameraMoveSpeed * DeltaTime;
        if (EnableSmoothing)
        {
            _targetPosition += movement;
        }
        else
        {
            Entity.Transform.Position += movement;
        }
    }

    /// <summary>
    /// Adjusts the camera's orthographic size based on mouse wheel input.
    /// </summary>
    /// <remarks>
    /// <para>Scrolling the mouse wheel up decreases the orthographic size (zooms in), while scrolling down increases it (zooms out).
    /// Each notch scales the size by <see cref="ZoomStep"/>; holding shift multiplies the notch count by <see cref="SpeedFactor"/>.</para>
    /// <para>The orthographic size is clamped between <see cref="MinOrthographicSize"/> and <see cref="MaxOrthographicSize"/>
    /// to prevent excessive zoom levels. With <see cref="EnableSmoothing"/> the size eases towards the target, otherwise it is applied at once.</para>
    /// </remarks>
    private void ProcessCameraZoom()
    {
        var zoomDelta = Input.MouseWheelDelta;

        if (zoomDelta == 0) return;

        // A wheel notch is an impulse - it arrives on one frame and is gone the next - so it is scaled
        // per notch, not per second. Multiplying by delta time would only make each notch depend on
        // the frame rate, which is what made zooming feel jumpy.
        if (Input.IsKeyDown(Keys.LeftShift) || Input.IsKeyDown(Keys.RightShift))
            zoomDelta *= SpeedFactor;

        // Multiplicative, so every notch changes the visible area by the same fraction whether the
        // camera is zoomed far in or far out. Subtracting a constant is a nudge at size 100 and a wall
        // at size 1.
        var newSize = _targetOrthographicSize * MathF.Pow(1f + ZoomStep, -zoomDelta);

        _targetOrthographicSize = Math.Clamp(newSize, MinOrthographicSize, MaxOrthographicSize);

        if (!EnableSmoothing)
            _camera!.OrthographicSize = _targetOrthographicSize;
    }

    /// <summary>
    /// Resets the camera to its default position and orthographic size when the 'H' key is pressed.
    /// </summary>
    /// <remarks>
    /// <para>The camera is reset to a position of (0, 0, 50), and the orthographic size is set to <see cref="OrthographicSizeDefault"/>.
    /// Both the target position (for smoothing) and the actual transform position are updated immediately.</para>
    /// </remarks>
    private void ResetCameraToDefault()
    {
        if (!Input.IsKeyPressed(Keys.H)) return;

        _targetPosition = _defaultCameraPosition;
        Entity.Transform.Position = _defaultCameraPosition;

        _targetOrthographicSize = OrthographicSizeDefault;
        _camera!.OrthographicSize = OrthographicSizeDefault;
    }

    /// <summary>
    /// Processes camera following behavior to track the <see cref="FollowTarget"/> entity.
    /// </summary>
    /// <remarks>
    /// <para>The camera moves towards the target's position plus <see cref="FollowOffset"/>.
    /// If <see cref="FollowSmoothing"/> is greater than 0, the camera smoothly interpolates towards the target using lerp.
    /// If <see cref="FollowSmoothing"/> is 0, the camera instantly snaps to the target position.</para>
    /// <para>Both the target position (for internal tracking) and the actual transform position are updated.</para>
    /// </remarks>
    private void ProcessCameraFollow()
    {
        if (FollowTarget is null) return;

        var targetPos = FollowTarget.Transform.Position + FollowOffset;

        if (FollowSmoothing > 0)
        {
            // Smooth follow using lerp
            var smoothFactor = Math.Clamp(FollowSmoothing * DeltaTime, 0, 1);
            _targetPosition = Vector3.Lerp(Entity.Transform.Position, targetPos, smoothFactor);
            _targetPosition.Z = _defaultZ;
            Entity.Transform.Position = _targetPosition;
        }
        else
        {
            // Instant follow
            _targetPosition = targetPos;
            _targetPosition.Z = _defaultZ;
            Entity.Transform.Position = targetPos;
        }
    }

    /// <summary>
    /// Applies smooth linear interpolation to gradually move the camera towards the target position.
    /// </summary>
    /// <remarks>
    /// <para>Eases both the position and the orthographic size. Uses <see cref="SmoothingSpeed"/> to control interpolation rate. The interpolation factor is clamped
    /// between 0 and 1 to ensure stable movement. Higher <see cref="SmoothingSpeed"/> values result in faster
    /// convergence to the target position.</para>
    /// <para>This method should only be called when <see cref="EnableSmoothing"/> is true.</para>
    /// </remarks>
    private void ApplySmoothMovement()
    {
        var smoothFactor = Math.Clamp(SmoothingSpeed * DeltaTime, 0, 1);
        Entity.Transform.Position = Vector3.Lerp(Entity.Transform.Position, _targetPosition, smoothFactor);
        _camera!.OrthographicSize = MathUtil.Lerp(_camera.OrthographicSize, _targetOrthographicSize, smoothFactor);
    }

    /// <summary>
    /// Constrains the camera position to stay within defined rectangular bounds.
    /// </summary>
    /// <remarks>
    /// <para>Clamps both the current camera position and the target position (when smoothing is enabled)
    /// to the rectangle defined by <see cref="MinBounds"/> and <see cref="MaxBounds"/> in the XY-plane.</para>
    /// <para>Bounds checking does not affect the Z-coordinate.</para>
    /// <para>This method should only be called when <see cref="EnableBounds"/> is true.</para>
    /// </remarks>
    private void ApplyCameraBounds()
    {
        var pos = Entity.Transform.Position;
        pos.X = Math.Clamp(pos.X, MinBounds.X, MaxBounds.X);
        pos.Y = Math.Clamp(pos.Y, MinBounds.Y, MaxBounds.Y);
        Entity.Transform.Position = pos;

        // Also clamp the target position if smoothing is enabled
        if (EnableSmoothing)
        {
            _targetPosition.X = Math.Clamp(_targetPosition.X, MinBounds.X, MaxBounds.X);
            _targetPosition.Y = Math.Clamp(_targetPosition.Y, MinBounds.Y, MaxBounds.Y);
        }
    }

    /// <summary>
    /// Processes mouse drag panning when the specified mouse button is held and the mouse is moved.
    /// </summary>
    /// <remarks>
    /// <para>When <see cref="MouseDragButton"/> is held down and the mouse moves, the camera pans in the opposite
    /// direction to create a natural drag feel (the world moves with the mouse). The Y-axis is inverted for natural panning.</para>
    /// <para>The movement is exactly the cursor's movement in world units: <see cref="CameraComponent.OrthographicSize"/> is
    /// the visible height, so the point under the cursor stays under the cursor at any zoom level.</para>
    /// <para>If <see cref="EnableSmoothing"/> is enabled, movement is applied to the target position for interpolation.
    /// Otherwise, movement is applied directly to the camera's transform position.</para>
    /// <para>This method should only be called when <see cref="EnableMouseDragPan"/> is true.</para>
    /// </remarks>
    private void ProcessMouseDragPan()
    {
        var currentMousePos = Input.MousePosition;

        // Check if the drag button is down
        bool isDragging = MouseDragButton switch
        {
            MouseButton.Left => Input.IsMouseButtonDown(MouseButton.Left),
            MouseButton.Middle => Input.IsMouseButtonDown(MouseButton.Middle),
            MouseButton.Right => Input.IsMouseButtonDown(MouseButton.Right),
            _ => false
        };

        if (isDragging)
        {
            if (_lastMousePosition.HasValue)
            {
                // Calculate mouse delta in screen space
                var mouseDelta = currentMousePos - _lastMousePosition.Value;

                // Mouse position is normalised (0..1 across the window) and OrthographicSize is the
                // visible height, so a cursor move of mouseDelta covers mouseDelta * size world units
                // vertically and mouseDelta * size * aspect horizontally. Invert X so the world follows
                // the cursor; Y is already flipped between screen space (down) and world space (up).
                var backBuffer = Game.GraphicsDevice.Presenter.BackBuffer;
                var aspect = (float)backBuffer.Width / backBuffer.Height;
                var height = _camera!.OrthographicSize;

                var worldDelta = new Vector3(-mouseDelta.X * height * aspect, mouseDelta.Y * height, 0);

                // Apply movement to the target position or directly to the camera
                if (EnableSmoothing)
                {
                    _targetPosition += worldDelta;
                }
                else
                {
                    Entity.Transform.Position += worldDelta;
                }
            }

            _lastMousePosition = currentMousePos;
        }
        else
        {
            _lastMousePosition = null;
        }
    }
}