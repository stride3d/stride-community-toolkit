using Stride.CommunityToolkit.Scripts.Utilities;
using Stride.Core;
using Stride.Engine;
using Stride.Input;

namespace Stride.CommunityToolkit.Scripts;

/// <summary>
/// Provides an interactive 2D camera controller for navigating 2D scenes in Stride.
/// This controller supports movement in the XY-plane using the arrow keys (optionally WASD),
/// zooming in and out with the mouse wheel, mouse drag panning, optional screen edge panning,
/// camera following, and smooth movement. Additional features include a speed boost when holding shift
/// and the ability to reset the camera to a default position and zoom level using the 'H' key.
/// </summary>
/// <remarks>
/// - The camera moves at a configurable speed which can be increased with shift keys.
/// - Zooming scales the camera's OrthographicSize by a fixed fraction per mouse-wheel notch; shift zooms faster too.
/// - Optional features: screen edge movement, camera bounds, follow target, smooth movement, mouse drag panning.
/// - The 'H' key resets the camera to its default position and orthographic size.
/// </remarks>
[Display("Basic 2D Camera Controller")]
[ComponentCategory("Camera")]
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
    /// Gets or sets the orthographic size the 'H' key resets the camera to. <see langword="null"/>, the
    /// default, restores the size the camera had when the controller started - so a scene that set
    /// its own framing gets that framing back, not a fixed number.
    /// </summary>
    public float? OrthographicSizeDefault { get; set; }

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

    /// <summary>
    /// Gets or sets whether zooming keeps the world point under the cursor fixed. Defaults to
    /// <see langword="true"/>; <see langword="false"/> zooms about the centre of the screen.
    /// </summary>
    /// <remarks>
    /// With smoothing enabled the position shift is applied to the smoothing target, so the anchor is
    /// approximate while the zoom eases and exact once it settles.
    /// </remarks>
    public bool ZoomToCursor { get; set; } = true;


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
    public MouseButton MouseDragButton { get; set; } = MouseButton.Right;

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
    private float _defaultOrthographicSize = 10f;
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
    /// Registers the camera's help section with the shared <see cref="DebugOverlay"/> and records the
    /// current position as the one the 'H' key restores.
    /// </remarks>
    public override void Start()
    {
        _defaultCameraPosition = Entity.Transform.Position;
        _targetPosition = Entity.Transform.Position;
        _defaultZ = Entity.Transform.Position.Z;

        _instructions = DebugOverlay.GetOrCreate(Game).AddCollapsibleSection(
            "Camera", "Camera controls", HelpToggleKey, () =>
            {
                // Keys first, one per line, each in the brackets the title uses, then the live values
                var lines = new List<TextElement>
                {
                    new("F3", "Reposition help", Color.LightGoldenrodYellow),
                    new("F4", "Hide help", Color.LightGoldenrodYellow),
                    new("H", "Reset camera"),
                };

                if (EnableWasdMovement)
                    lines.Add(new("W A S D", "Move"));

                lines.Add(new("Arrow keys", "Move"));
                lines.Add(new("Shift", "Hold to move faster"));
                lines.Add(new("Mouse wheel", "Zoom"));

                if (EnableMouseDragPan)
                    lines.Add(new($"{MouseDragButton} drag", "Pan"));

                // Live state, matching the 3D controller's help: where the camera is and how much of
                // the world is visible (OrthographicSize is the view height in world units). Two fixed
                // decimals, so the numbers stop changing width as the camera moves.
                var position = Entity.Transform.Position;
                lines.Add(new($"Position: {position.X:0.00}, {position.Y:0.00}", Color.Yellow));

                var camera = _camera ?? Entity.Get<CameraComponent>();
                if (camera is not null)
                    lines.Add(new($"Zoom: {camera.OrthographicSize:0.00} world units high", Color.Yellow));

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
    /// <item><description>Process camera follow if <see cref="FollowTarget"/> is set, otherwise process manual controls (movement, screen edge, mouse drag).</description></item>
    /// <item><description>Process camera zoom via mouse wheel.</description></item>
    /// <item><description>Check for camera reset (H key).</description></item>
    /// <item><description>Apply smooth movement if <see cref="EnableSmoothing"/> is enabled.</description></item>
    /// <item><description>Apply camera bounds if <see cref="EnableBounds"/> is enabled.</description></item>
    /// </list>
    /// </remarks>
    public override void Update()
    {
        if (_camera is null)
        {
            _camera = Entity.Get<CameraComponent>();

            if (_camera is null) return; // Ensure we have a camera component

            // Captured here rather than in Start: a scene sets its framing in its own Start, which
            // runs before this script's first update, and this is what H restores
            _targetOrthographicSize = _camera.OrthographicSize;
            _defaultOrthographicSize = _camera.OrthographicSize;
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
    /// Processes keyboard-driven camera translation.
    /// </summary>
    private void ProcessCameraMovement()
    {
        var moveDirection = ReadMovementKeys();

        // Normalize the moveDirection to ensure consistent movement speed, for example, when moving diagonally
        if (moveDirection.LengthSquared() > 1)
            moveDirection.Normalize();

        // Apply a speed factor when shift is held
        if (Input.IsKeyDown(Keys.LeftShift) || Input.IsKeyDown(Keys.RightShift))
            moveDirection *= SpeedFactor;

        Pan(moveDirection * CameraMoveSpeed * DeltaTime);
    }

    /// <summary>
    /// Reads the movement keys as a direction, before any speed or frame-time scaling.
    /// </summary>
    /// <returns>A direction whose components are -1, 0 or 1, so opposite keys cancel out.</returns>
    /// <remarks>
    /// The arrow keys always move the camera; WASD does so only when <see cref="EnableWasdMovement"/>
    /// is set, because a 2D game that uses WASD for the player cannot also spend it on the camera.
    /// </remarks>
    private Vector3 ReadMovementKeys()
    {
        var direction = Vector3.Zero;

        if (Input.IsKeyDown(Keys.Up) || (EnableWasdMovement && Input.IsKeyDown(Keys.W)))
            direction.Y++;
        if (Input.IsKeyDown(Keys.Down) || (EnableWasdMovement && Input.IsKeyDown(Keys.S)))
            direction.Y--;
        if (Input.IsKeyDown(Keys.Left) || (EnableWasdMovement && Input.IsKeyDown(Keys.A)))
            direction.X--;
        if (Input.IsKeyDown(Keys.Right) || (EnableWasdMovement && Input.IsKeyDown(Keys.D)))
            direction.X++;

        return direction;
    }

    /// <summary>
    /// Applies a world-space movement to the camera.
    /// </summary>
    /// <param name="movement">The movement in world units.</param>
    /// <remarks>
    /// With <see cref="EnableSmoothing"/> the movement goes to the target position and
    /// <see cref="ApplySmoothMovement"/> eases the camera towards it; without it the transform moves
    /// at once. Every source of panning - keys, screen edges, zoom-to-cursor and drag - goes through
    /// here, so that choice is made once rather than repeated at each of them.
    /// </remarks>
    private void Pan(Vector3 movement)
    {
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
        var backBuffer = Game.GraphicsDevice.Presenter.BackBuffer;

        var moveDirection = Camera2DMath.ScreenEdgeDirection(
            Input.MousePosition, backBuffer.Width, backBuffer.Height, ScreenEdgeBorderWidth);

        if (moveDirection.LengthSquared() > 1)
            moveDirection.Normalize();

        Pan(moveDirection * CameraMoveSpeed * DeltaTime);
    }

    /// <summary>
    /// Adjusts the camera's orthographic size based on mouse wheel input.
    /// </summary>
    /// <remarks>
    /// <para>Scrolling the mouse wheel up decreases the orthographic size (zooms in), while scrolling down increases it (zooms out).
    /// Each notch scales the size by <see cref="ZoomStep"/>; holding shift multiplies the notch count by <see cref="SpeedFactor"/>.
    /// With <see cref="ZoomToCursor"/> the point under the cursor stays fixed while zooming.</para>
    /// <para>While the middle mouse button is both the drag-pan button and held down, wheel input is ignored:
    /// pressing the wheel to pan almost always rolls it a notch too, which would zoom mid-drag.</para>
    /// <para>The orthographic size is clamped between <see cref="MinOrthographicSize"/> and <see cref="MaxOrthographicSize"/>
    /// to prevent excessive zoom levels. With <see cref="EnableSmoothing"/> the size eases towards the target, otherwise it is applied at once.</para>
    /// </remarks>
    private void ProcessCameraZoom()
    {
        // Pressing the wheel to drag-pan almost always rolls it a notch too; while the middle button is
        // both the drag button and held down, the wheel is a pan grip, not a zoom request
        if (FollowTarget is null && EnableMouseDragPan && MouseDragButton == MouseButton.Middle && Input.IsMouseButtonDown(MouseButton.Middle))
            return;

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
        var oldSize = _targetOrthographicSize;
        var newSize = Math.Clamp(oldSize * MathF.Pow(1f + ZoomStep, -zoomDelta), MinOrthographicSize, MaxOrthographicSize);

        _targetOrthographicSize = newSize;

        if (ZoomToCursor && newSize != oldSize)
        {
            var mouse = Input.MousePosition;

            // A cursor outside the window (alt-tabbed, multi-monitor) falls back to centre zoom
            if (mouse.X >= 0f && mouse.X <= 1f && mouse.Y >= 0f && mouse.Y <= 1f)
            {
                var backBuffer = Game.GraphicsDevice.Presenter.BackBuffer;
                var aspect = (float)backBuffer.Width / backBuffer.Height;

                Pan(Camera2DMath.ZoomToCursorShift(mouse, aspect, oldSize, newSize));
            }
        }

        if (!EnableSmoothing)
            _camera!.OrthographicSize = _targetOrthographicSize;
    }

    /// <summary>
    /// Resets the camera to its default position and orthographic size when the 'H' key is pressed.
    /// </summary>
    /// <remarks>
    /// <para>The camera returns to the position it started at and to <see cref="OrthographicSizeDefault"/>, or
    /// to the orthographic size it started with when that is not set. Both the target values (for
    /// smoothing) and the actual camera are updated immediately.</para>
    /// </remarks>
    private void ResetCameraToDefault()
    {
        if (!Input.IsKeyPressed(Keys.H)) return;

        _targetPosition = _defaultCameraPosition;
        Entity.Transform.Position = _defaultCameraPosition;

        var size = OrthographicSizeDefault ?? _defaultOrthographicSize;

        _targetOrthographicSize = size;
        _camera!.OrthographicSize = size;
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

                Pan(worldDelta);
            }

            _lastMousePosition = currentMousePos;
        }
        else
        {
            _lastMousePosition = null;
        }
    }
}