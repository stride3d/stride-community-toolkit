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

    private CameraComponent? _camera;

    public override void Update()
    {
        // Initialize a vector to hold the movement direction
        var moveDirection = Vector3.Zero;

        // Check for input and adjust the moveDirection accordingly
        if (Input.IsKeyDown(Keys.W) || Input.IsKeyDown(Keys.Up))
        {
            moveDirection.Y += 1;
        }
        if (Input.IsKeyDown(Keys.S) || Input.IsKeyDown(Keys.Down))
        {
            moveDirection.Y -= 1;
        }
        if (Input.IsKeyDown(Keys.A) || Input.IsKeyDown(Keys.Left))
        {
            moveDirection.X -= 1;
        }
        if (Input.IsKeyDown(Keys.D) || Input.IsKeyDown(Keys.Right))
        {
            moveDirection.X += 1;
        }

        // Normalize the moveDirection to ensure consistent movement speed, for example when moving diagonally
        if (moveDirection.Length() > 1)
        {
            moveDirection.Normalize();
        }

        // Update the camera's position
        Entity.Transform.Position += moveDirection * MoveSpeed * Game.DeltaTime();

        // In an orthographic camera setup, moving the camera along the Z-axis doesn't create a zoom effect as it would with a perspective camera. This is because an orthographic camera does not have a perspective foreshortening effect – objects appear the same size regardless of their distance from the camera. To create a zoom effect with an orthographic camera, you need to adjust the OrthographicSize property of the camera, which defines the visible height of the camera's view at a given distance.
        if (_camera == null)
        {
            _camera = Entity.Get<CameraComponent>();
        }
        else
        {
            // Zooming in and out
            var zoomDelta = Input.MouseWheelDelta;

            _camera.OrthographicSize = Math.Max(0.1f, _camera.OrthographicSize - zoomDelta * ZoomSpeed * Game.DeltaTime());
        }
    }
}