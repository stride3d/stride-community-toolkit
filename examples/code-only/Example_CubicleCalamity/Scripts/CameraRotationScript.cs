using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Scripts.Utilities;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;

namespace Example_CubicleCalamity.Scripts;

public class CameraRotationScript : SyncScript
{
    private const string GroundEntityName = "Ground";
    private float _rotationSpeed = 45f; // degrees per second
    private Vector3 _rotationCentre;
    DebugTextPrinter? _instructionsPrinter;

    public override void Start()
    {
        //var ground = Entity.Scene.Entities.FirstOrDefault(e => e.Name == GroundEntityName);
        var ground = SceneSystem.SceneInstance.RootScene
                         .Entities.FirstOrDefault(e => e.Name == GroundEntityName);

        if (ground == null) return;

        _rotationCentre = ground.Transform.Position;

        InitializeDebugTextPrinter();
    }

    public override void Update()
    {
        DisplayInstructions();

        // Compute how many degrees we should turn this frame
        var deltaTime = this.DeltaTime();

        float deltaRotation = 0f;

        if (Input.IsKeyDown(Keys.Z))
        {
            deltaRotation = -_rotationSpeed * deltaTime;
        }
        else if (Input.IsKeyDown(Keys.C))
        {
            deltaRotation = +_rotationSpeed * deltaTime;
        }

        if (Math.Abs(deltaRotation) > 0.001f)
        {
            RotateAroundCentre(deltaRotation);
        }
    }

    private void RotateAroundCentre(float angleDegrees)
    {
        // Compute offset from centre
        var offset = Entity.Transform.Position - _rotationCentre;

        // Rotate offset around world‑Y
        var yawQuat = Quaternion.RotationY(MathUtil.DegreesToRadians(angleDegrees));
        var rotatedOffset = Vector3.Transform(offset, yawQuat);

        // Reposition the camera
        Entity.Transform.Position = _rotationCentre + rotatedOffset;

        // Re‑aim the camera at the centre (preserves original pitch/tilt)
        Entity.Transform.LookAt(_rotationCentre, Vector3.UnitY);
    }

    void DisplayInstructions()
    {
        _instructionsPrinter?.Print(GenerateInstructions(Entity.Transform.Position));
    }

    void InitializeDebugTextPrinter()
    {
        var screenSize = new Int2(Game.GraphicsDevice.Presenter.BackBuffer.Width, Game.GraphicsDevice.Presenter.BackBuffer.Height);
        var instructions = GenerateInstructions(Entity.Transform.Position);

        _instructionsPrinter = new DebugTextPrinter()
        {
            DebugTextSystem = DebugText,
            TextSize = new(205, 17 * instructions.Count),
            ScreenSize = screenSize,
            Instructions = instructions,
        };

        _instructionsPrinter.Initialize(DisplayPosition.BottomLeft);
    }

    static List<TextElement> GenerateInstructions(Vector3 cameraPosition)
     => [
            new("GAME INSTRUCTIONS"),
            //new("Click the golden sphere and drag to move it (Y-axis locked)"),
            new("Click a cube", Color.Yellow),
            new("Hold Shift: Left mouse button down", Color.Yellow),
            new("Z/C orbit around the ground", Color.Yellow),
            new($"Camera Position: {cameraPosition}", Color.Yellow),
        ];
}