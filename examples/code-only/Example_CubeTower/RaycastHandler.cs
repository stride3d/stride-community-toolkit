using Stride.CommunityToolkit.Engine;
using Stride.Engine;
using Stride.Input;
using Stride.Physics;

namespace Example_CubeTower;

public class RaycastHandler : AsyncScript
{
    public override async Task Execute()
    {
        var cameraComponent = Entity.Scene.Entities.FirstOrDefault(x => x.Get<CameraComponent>() != null)?.Get<CameraComponent>();

        // not working
        var cameraComponent2 = Entity.GetComponent<CameraComponent>();

        // not working
        var cameraComponent3 = this.GetFirstCamera();

        var simulation = this.GetSimulation();

        if (cameraComponent == null || simulation == null) return;

        while (Game.IsRunning)
        {
            if (Input.HasMouse && Input.IsMouseButtonPressed(MouseButton.Left))
            {
                var hitResult = cameraComponent.RayCast(this, Input.MousePosition);

                if (hitResult.Succeeded)
                {
                    if (hitResult.Collider.Entity.Name == "Cube")
                    {
                        hitResult.Collider.Entity.Remove();
                    }

                    //Console.WriteLine($"Hit {hitResult.Collider.Entity.Name}");
                }
            }

            await Script.NextFrame();
        }
    }

    //private Ray GetCurrentRay()
    //{
    //    // Implement logic to construct a ray from the camera through the screen.
    //}

    //private RaycastResult Raycast(Ray ray)
    //{
    //    // Implement logic to perform a raycast and return the result.
    //}

    //private void OnEntityHit(Entity entity)
    //{
    //    // Implement logic to handle an entity being hit by a raycast.
    //}
}
