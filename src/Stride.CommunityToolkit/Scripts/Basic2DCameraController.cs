using Stride.CommunityToolkit.Games;
using Stride.CommunityToolkit.Scripts.Utilities;
using Stride.Engine;
using Stride.Input;

namespace Stride.CommunityToolkit.Scripts;

/// <summary>
/// Provides an interactive 2D camera controller for navigating 2D scenes in Stride.
/// This controller supports movement in the XY-plane using keyboard inputs (W, A, S, D, arrow keys),
/// zooming in and out with the mouse wheel, and moving the camera based on mouse position near screen edges.
/// Additional features include a speed boost when holding shift and the ability to reset the camera
/// to a default position and zoom level using the 'H' key.
/// </summary>
/// <remarks>
/// - The camera moves at a default speed which can be increased with shift keys.
/// - Zooming is performed by changing the OrthographicSize of the camera.
/// - The camera can be moved towards the edges of the screen when the mouse cursor is close to them.
/// - The 'H' key resets the camera to its default position and orthographic size.
/// - Default settings: FarClipPlane=1000, NearClipPlane=0.1f, OrthographicSize=10.
/// </remarks>
public class Basic2DCameraController : SyncScript
{
    private const float CameraMoveSpeed = 5.0f;
    private const float OrthographicSizeDefault = 10.0f;

    // Additional speed multiplier when holding shift
    private const float SpeedFactor = 5.0f;

    // Speed of zooming in and out
    private const float ZoomSpeed = 50.0f;

    // Width of the screen edge border for triggering movement
    private const float ScreenEdgeBorderWidth = 10.0f;

    private static readonly Vector3 _defaultCameraPosition = new(0, 0, 50);
    private CameraComponent? _camera;

    private DebugTextPrinter? _instructions;
    private bool _showInstructions = true;

    public override void Start()
    {
        _instructions = new DebugTextPrinter()
        {
            DebugTextSystem = DebugText,
            TextSize = new(205, 18 * 7),
            ScreenSize = GetScreenSize(),
            Instructions =
            [
                new("CONTROL INSTRUCTIONS"),
                new("F2: Toggle Help", Color.Red),
                new("F3: Reposition Help", Color.Red),
                new("Arrow Keys: Move"),
                new("Hold Shift: Increase speed"),
                new("Mouse Wheel: Zoom"),
                new("H: Reset Camera"),
            ]
        };

        _instructions.Initialize();
    }

    private Int2 GetScreenSize() => new Int2(Game.GraphicsDevice.Presenter.BackBuffer.Width, Game.GraphicsDevice.Presenter.BackBuffer.Height);

    public override void Update()
    {
        if (_camera == null)
        {
            _camera = Entity.Get<CameraComponent>();

            if (_camera == null) return; // Ensure we have a camera component
        }

        ToggleInstructionKeys();

        ProcessCameraMovement();

        ProcessScreenEdgeMovement();

        ProcessCameraZoom();

        ResetCameraToDefault();

        if (_showInstructions)
        {
            _instructions?.UpdateScreenSize(GetScreenSize());
            _instructions?.Print();
        }
    }

    private void ToggleInstructionKeys()
    {
        if (!Input.HasKeyboard) return;

        if (Input.IsKeyPressed(Keys.F2))
        {
            _showInstructions = !_showInstructions;
        }

        if (Input.IsKeyPressed(Keys.F3))
        {
            _instructions?.ChangeStartPosition();
        }
    }

    /// <summary>
    /// Process camera movement based on mouse movement screen edge proximity
    /// </summary>
    private void ProcessCameraMovement()
    {
        var moveDirection = Vector3.Zero;

        // Update moveDirection based on key input
        if (Input.IsKeyDown(Keys.Up))
            moveDirection.Y++;
        if (Input.IsKeyDown(Keys.Down))
            moveDirection.Y--;
        if (Input.IsKeyDown(Keys.Left))
            moveDirection.X--;
        if (Input.IsKeyDown(Keys.Right))
            moveDirection.X++;

        // Normalize the moveDirection to ensure consistent movement speed, for example when moving diagonally
        if (moveDirection.LengthSquared() > 1)
            moveDirection.Normalize();

        // Apply speed factor when shift is held
        if (Input.IsKeyDown(Keys.LeftShift) || Input.IsKeyDown(Keys.RightShift))
            moveDirection *= SpeedFactor;

        // Apply movement to the camera position
        Entity.Transform.Position += moveDirection * CameraMoveSpeed * Game.DeltaTime();
    }

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

        // We could lock mouse inside the window
        //Input.Mouse.LockPosition();

        // Check if the mouse is within the screen bounds. We are detecting -1 because mouse keeps detected outside of the screen
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

        // Apply the movement
        Entity.Transform.Position += moveDirection * CameraMoveSpeed * Game.DeltaTime();
    }

    private void ProcessCameraZoom()
    {
        // In an orthographic camera setup, moving the camera along the Z-axis doesn't create a zoom effect as it would with a perspective camera. This is because an orthographic camera does not have a perspective foreshortening effect – objects appear the same size regardless of their distance from the camera. To create a zoom effect with an orthographic camera, you need to adjust the OrthographicSize property of the camera, which defines the visible height of the camera's view at a given distance.a
        var zoomDelta = Input.MouseWheelDelta;

        _camera!.OrthographicSize = Math.Max(0.1f, _camera.OrthographicSize - zoomDelta * ZoomSpeed * Game.DeltaTime());
    }

    /// <summary>
    /// Reset camera to default position and orthographic size when 'H' key is pressed
    /// </summary>
    private void ResetCameraToDefault()
    {
        if (!Input.IsKeyPressed(Keys.H)) return;

        Entity.Transform.Position = _defaultCameraPosition;

        _camera!.OrthographicSize = OrthographicSizeDefault;
    }
}