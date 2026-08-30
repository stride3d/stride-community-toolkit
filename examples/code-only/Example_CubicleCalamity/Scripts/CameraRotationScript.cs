using Example_CubicleCalamity.Shared;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.Text;
using Stride.CommunityToolkit.Scripts.Utilities;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;

namespace Example_CubicleCalamity.Scripts;

public class CameraRotationScript : SyncScript
{
    /// <summary>
    /// Closest the camera may be to the orbit centre and still be swung around it, in world units.
    /// </summary>
    private const float MinimumOrbitRadius = 0.01f;

    /// <summary>
    /// How much faster the camera orbits while shift is held.
    /// </summary>
    /// <remarks>
    /// Matches <c>Basic3DCameraController.SpeedFactor</c>, so shift means the same thing whichever
    /// way the camera is being moved.
    /// </remarks>
    private const float SprintMultiplier = 5f;

    private readonly float _rotationSpeed = 45f; // degrees per second
    private Vector3 _rotationCentre;
    private DebugOverlaySection? _instructions;

    /// <summary>
    /// Gets or sets the world point the camera orbits and aims at. Leave <see langword="null"/> to
    /// fall back to the ground entity's position.
    /// </summary>
    /// <remarks>
    /// The ground sits at Y = 0, so orbiting around it alone aims the camera at the base of the
    /// platform. Pointing this at the platform's mid-height keeps the stack framed as it turns.
    /// </remarks>
    public Vector3? RotationCentre { get; set; }

    public override void Start()
    {
        // Falls back to the ground, then to the origin - previously a missing ground returned early
        // and silently took the instructions overlay down with it
        _rotationCentre = RotationCentre
            ?? SceneSystem.SceneInstance.RootScene
                   .Entities.FirstOrDefault(e => e.Name == EntityNames.Ground)?.Transform.Position
            ?? Vector3.Zero;

        InitializeDebugOverlay();
    }

    /// <inheritdoc />
    public override void Update()
    {
        // The property wins whenever it is set, so the centre can follow the platform as levels
        // change its height - Start's fallback only covers the case where it never is
        if (RotationCentre is { } centre)
        {
            _rotationCentre = centre;
        }

        var deltaTime = this.DeltaTime();

        // Shift speeds the orbit up, matching what it already does for the free-look controller's
        // movement and for rapid clearing. One modifier, the same meaning everywhere.
        var speed = _rotationSpeed * (IsSprinting() ? SprintMultiplier : 1f);

        var deltaRotation = 0f;

        if (Input.IsKeyDown(Keys.Z))
        {
            deltaRotation = -speed * deltaTime;
        }
        else if (Input.IsKeyDown(Keys.C))
        {
            deltaRotation = +speed * deltaTime;
        }

        if (Math.Abs(deltaRotation) > 0.001f)
        {
            RotateAroundCentre(deltaRotation);
        }
    }

    private bool IsSprinting()
        => Input.IsKeyDown(Keys.LeftShift) || Input.IsKeyDown(Keys.RightShift);

    /// <summary>
    /// Swings the camera around <see cref="RotationCentre"/> by the given angle and re-aims it.
    /// </summary>
    /// <param name="angleDegrees">How far to swing this frame, in degrees.</param>
    /// <remarks>
    /// The orbit is computed from the camera's own position each frame rather than from a stored
    /// angle, so it composes with the free-look controller instead of fighting it: fly somewhere with
    /// WASD and the orbit continues from wherever that left the camera.
    /// </remarks>
    private void RotateAroundCentre(float angleDegrees)
    {
        var offset = Entity.Transform.Position - _rotationCentre;

        // Sitting on the centre leaves no radius to swing through and no direction to look in. Both
        // are degenerate, so there is nothing meaningful to do until the camera is moved off it.
        if (offset.Length() < MinimumOrbitRadius) return;

        var yaw = Quaternion.RotationY(MathUtil.DegreesToRadians(angleDegrees));
        var rotatedOffset = Vector3.Transform(offset, yaw);

        Entity.Transform.Position = _rotationCentre + rotatedOffset;

        // Re-aim at the centre, which preserves the pitch the camera already had
        Entity.Transform.LookAt(_rotationCentre, Vector3.UnitY);
    }

    void InitializeDebugOverlay()
    {
        var overlay = DebugOverlay.GetOrCreate(Game);

        overlay.Position = DisplayPosition.BottomLeft;

        // Runs every frame the overlay is drawn, so the camera position readout stays live
        _instructions = overlay.AddSection(
            "Game", () => GenerateInstructions(Entity.Transform.Position));
    }

    static List<TextElement> GenerateInstructions(Vector3 cameraPosition)
     => [
            new("GAME INSTRUCTIONS"),
            //new("Click the golden sphere and drag to move it (Y-axis locked)"),
            new("Click a cube", Color.Yellow),
            new("Hold Shift: Left mouse button down", Color.Yellow),
            new("Z/C orbit around the platform (Shift: faster)", Color.Yellow),
            new($"Camera Position: {cameraPosition}", Color.Yellow),
        ];
}