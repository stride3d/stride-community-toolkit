using Stride.CommunityToolkit.Engine;
using Stride.Engine;
using Stride.Input;

namespace Stride.CommunityToolkit.Scripts;

public class Basic2DCameraController : SyncScript
{
    // Speed at which the camera moves
    private const float MoveSpeed = 5.0f;

    // Speed of zooming in and out
    private const float ZoomSpeed = 50.0f;

    // Default orthographic size
    private const float OrthographicSizeDefault = 10.0f;

    // Default camera position
    private static readonly Vector3 DefaultPosition = new Vector3(0, 0, 50);

    private CameraComponent? _camera;

    public override void Update()
    {
        if (_camera == null)
        {
            _camera = Entity.Get<CameraComponent>();

            if (_camera == null) return; // Ensure we have a camera component
        }

        ProcessCameraMovement();

        ProcessCameraZoom();

        // Reset camera to default position and size when 'H' key is pressed
        if (Input.IsKeyPressed(Keys.H))
        {
            ResetCameraToDefault();
        }
    }

    private void ProcessCameraMovement()
    {
        var moveDirection = Vector3.Zero;

        // Update moveDirection based on key input
        if (Input.IsKeyDown(Keys.W) || Input.IsKeyDown(Keys.Up))
            moveDirection.Y += 1;
        if (Input.IsKeyDown(Keys.S) || Input.IsKeyDown(Keys.Down))
            moveDirection.Y -= 1;
        if (Input.IsKeyDown(Keys.A) || Input.IsKeyDown(Keys.Left))
            moveDirection.X -= 1;
        if (Input.IsKeyDown(Keys.D) || Input.IsKeyDown(Keys.Right))
            moveDirection.X += 1;

        /// Normalize the moveDirection to ensure consistent movement speed, for example when moving diagonally
        if (moveDirection.LengthSquared() > 1)
            moveDirection.Normalize();

        // Apply movement to the camera position
        Entity.Transform.Position += moveDirection * MoveSpeed * Game.DeltaTime();
    }

    private void ProcessCameraZoom()
    {
        // In an orthographic camera setup, moving the camera along the Z-axis doesn't create a zoom effect as it would with a perspective camera. This is because an orthographic camera does not have a perspective foreshortening effect – objects appear the same size regardless of their distance from the camera. To create a zoom effect with an orthographic camera, you need to adjust the OrthographicSize property of the camera, which defines the visible height of the camera's view at a given distance.a
        var zoomDelta = Input.MouseWheelDelta;

        _camera!.OrthographicSize = Math.Max(0.1f, _camera.OrthographicSize - zoomDelta * ZoomSpeed * Game.DeltaTime());
    }

    private void ResetCameraToDefault()
    {
        // Reset the camera's position and orthographic size to defaults
        Entity.Transform.Position = DefaultPosition;

        _camera!.OrthographicSize = OrthographicSizeDefault;
    }
}