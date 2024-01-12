using Stride.CommunityToolkit.Engine;
using Stride.Engine;
using Stride.Input;

namespace Stride.CommunityToolkit.Scripts;

// Use mouse to scroll in and out

public class Basic2DCameraController : SyncScript
{
    // Speed at which the camera moves
    public float MoveSpeed { get; set; } = 5.0f;

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

        // Normalize the moveDirection to ensure consistent movement speed
        if (moveDirection.Length() > 1)
        {
            moveDirection.Normalize();
        }

        // Update the camera's position
        Entity.Transform.Position += moveDirection * MoveSpeed * Game.DeltaTime();
    }
}