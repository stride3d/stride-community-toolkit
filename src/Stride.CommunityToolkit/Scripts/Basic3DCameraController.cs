using Stride.CommunityToolkit.Scripts.Utilities;
using Stride.Engine;
using Stride.Input;

namespace Stride.CommunityToolkit.Scripts;

/// <summary>
/// A script that allows to move and rotate an entity through keyboard, mouse and touch input to provide basic camera navigation.
/// </summary>
/// <remarks>
/// The entity can be moved using W, A, S, D, Q and E, arrow keys, a gamepad's left stick or dragging/scaling using multitouch.
/// Rotation is achieved using the Numpad, the mouse while holding the right mouse button, a gamepad's right stick, or dragging using single-touch.
///
/// This functionality is inspired by Stride.Assets.Presentation, Assets->Scripts->Camera
/// </remarks>
public class Basic3DCameraController : SyncScript
{
    private const float MaximumPitch = MathUtil.PiOverTwo * 0.99f;
    private readonly DisplayPosition _displayPosition = DisplayPosition.TopRight;
    private Vector3 _upVector;
    private Vector3 _translation;
    private Vector3 _defaultCameraPosition;
    private Quaternion _defaultCameraRotation;
    private float _yaw;
    private float _pitch;

    private DebugOverlaySection? _instructions;

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
    /// Collapsed by default because these keys are the same in every scene and stop being worth ten
    /// lines of screen space almost immediately, while whatever the scene itself has to say does not.
    /// The remaining line names the key, so nothing is hidden without a way back.
    /// </remarks>
    public bool HelpCollapsed { get; set; } = true;

    /// <summary>
    /// Initializes a new instance of the <see cref="Basic3DCameraController"/> class with the specified display
    /// position.
    /// </summary>
    /// <param name="displayPosition">The position on the screen where the camera controls will be displayed. Defaults to <see cref="DisplayPosition.TopRight"/>.
    /// </param>
    public Basic3DCameraController(DisplayPosition displayPosition = DisplayPosition.TopRight)
    {
        _displayPosition = displayPosition;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Basic3DCameraController"/> class.
    /// </summary>
    public Basic3DCameraController() { }

    /// <summary>
    /// Enables gamepad input when <c>true</c>.
    /// </summary>
    public bool Gamepad { get; set; }

    /// <summary>
    /// Base translation speed (units/second) for keyboard navigation.
    /// </summary>
    public Vector3 KeyboardMovementSpeed { get; set; } = new Vector3(5.0f);

    /// <summary>
    /// Translation speed scaling for touch gestures (XY, Z).
    /// </summary>
    public Vector3 TouchMovementSpeed { get; set; } = new Vector3(0.7f, 0.7f, 0.3f);

    /// <summary>
    /// Multiplier applied when sprint modifier (shift / pad buttons) is active.
    /// </summary>
    public float SpeedFactor { get; set; } = 5.0f;

    /// <summary>
    /// Rotation speed for keyboard / gamepad discrete rotation.
    /// </summary>
    public Vector2 KeyboardRotationSpeed { get; set; } = new Vector2(3.0f);

    /// <summary>
    /// Sensitivity for mouse-driven yaw/pitch adjustments.
    /// </summary>
    public Vector2 MouseRotationSpeed { get; set; } = new Vector2(1.0f, 1.0f);

    /// <summary>
    /// Sensitivity for touch drag rotation (yaw, pitch).
    /// </summary>
    public Vector2 TouchRotationSpeed { get; set; } = new Vector2(1.0f, 0.7f);

    /// <summary>
    /// Initializes help overlay, caches default transform, and configures gesture recognizers.
    /// </summary>
    public override void Start()
    {
        base.Start();

        // DisplayPosition.None means the caller does not want camera help on screen, so no section is
        // registered - and the shared overlay's position is left for whoever else is using it
        if (_displayPosition != DisplayPosition.None)
        {
            var overlay = DebugOverlay.GetOrCreate(Game);

            overlay.Position = _displayPosition;

            _instructions = overlay.AddCollapsibleSection("Camera", "Camera controls", HelpToggleKey, static () =>
            [
                new("F3: Reposition Help", Color.Red),
                new("WASD: Move", Color.LightGreen),
                new("Arrow Keys: Move", Color.LightGreen),
                new("Q/E: Ascend/Descend", Color.LightGreen),
                new("Hold Shift: Increase speed", Color.LightGreen),
                new("Numpad 2/4/6/8: Rotation", Color.LightGreen),
                new("Right Mouse Button: Rotate", Color.LightGreen),
                new("H: Reset Camera", Color.LightGreen),
            ], HelpCollapsed, order: -100);
        }

        // Default up-direction
        _upVector = Vector3.UnitY;

        _defaultCameraPosition = Entity.Transform.Position;
        _defaultCameraRotation = Entity.Transform.Rotation;

        // Configure touch input
        if (!Platform.IsWindowsDesktop)
        {
            Input.Gestures.Add(new GestureConfigDrag());
            Input.Gestures.Add(new GestureConfigComposite());
        }
    }

    /// <summary>
    /// Per-frame update: processes input and applies translation/rotation.
    /// </summary>
    public override void Update()
    {
        ProcessInput();
        UpdateTransform();
    }

    private void ProcessInput()
    {
        var deltaTime = (float)Game.UpdateTime.Elapsed.TotalSeconds;
        _translation = Vector3.Zero;
        _yaw = 0f;
        _pitch = 0f;


        KeyboardAndGamePadBasedMovement(deltaTime);

        KeyboardAndGamePadBasedRotation(deltaTime);

        MouseMovementAndGestures();

        ResetCameraToDefault();
    }

    /// <summary>
    /// Gets or sets whether the camera's help section is shown.
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

    private void KeyboardAndGamePadBasedMovement(float deltaTime)
    {
        // Our base speed is: one unit per second:
        // deltaTime contains the duration of the previous frame, let's say that in this update
        // or frame it is equal to 1/60, that means that the previous update ran 1/60 of a second ago
        // and the next will, in most cases, run in around 1/60 of a second from now. Knowing that,
        // we can move 1/60 of a unit on this frame so that in around 60 frames(1 second)
        // we will have traveled one whole unit in a second.
        // If you don't use deltaTime, your speed will be dependent on the number of frames rendered
        // on screen, which often are inconsistent, meaning that if the player has performance issues,
        // this entity will move around slower.

        var speed = 1f * deltaTime;

        var movementDirection = Vector3.Zero;

        if (Gamepad && Input.HasGamePad)
        {
            GamePadState padState = Input.DefaultGamePad.State;
            // LeftThumb can be positive or negative on both axes (pushed to the right or to the left)
            movementDirection.Z += padState.LeftThumb.Y;
            movementDirection.X += padState.LeftThumb.X;

            // Triggers are always positive, in this case using one to increase and the other to decrease
            movementDirection.Y -= padState.LeftTrigger;
            movementDirection.Y += padState.RightTrigger;

            // Increase speed when pressing A, LeftShoulder or RightShoulder
            // Here:does the enum flag 'Buttons' have one of the flag ('A','LeftShoulder' or 'RightShoulder') set
            if ((padState.Buttons & (GamePadButton.A | GamePadButton.LeftShoulder | GamePadButton.RightShoulder)) != 0)
                speed *= SpeedFactor;
        }

        if (Input.HasKeyboard)
        {
            // Move with keyboard
            // Forward/Backward
            if (Input.IsKeyDown(Keys.W) || Input.IsKeyDown(Keys.Up))
                movementDirection.Z += 1;
            if (Input.IsKeyDown(Keys.S) || Input.IsKeyDown(Keys.Down))
                movementDirection.Z -= 1;

            // Left/Right
            if (Input.IsKeyDown(Keys.A) || Input.IsKeyDown(Keys.Left))
                movementDirection.X -= 1;
            if (Input.IsKeyDown(Keys.D) || Input.IsKeyDown(Keys.Right))
                movementDirection.X += 1;

            // Down/Up
            if (Input.IsKeyDown(Keys.Q))
                movementDirection.Y -= 1;
            if (Input.IsKeyDown(Keys.E))
                movementDirection.Y += 1;

            // Increase speed when pressing shift
            if (Input.IsKeyDown(Keys.LeftShift) || Input.IsKeyDown(Keys.RightShift))
                speed *= SpeedFactor;

            // If the player pushes down two or more buttons, the direction and ultimately the base speed
            // will be greater than one (vector(1, 1) is farther away from zero than vector(0, 1)),
            // normalizing the vector ensures that whichever direction the player chooses, that direction
            // will always be at most one unit in length.
            // We're keeping dir as is if it isn't longer than one to retain subunit movement:
            // a stick not entirely pushed forward should make the entity move slower.
            if (movementDirection.Length() > 1f)
                movementDirection = Vector3.Normalize(movementDirection);
        }

        // Finally, push all of that to the translation variable which will be used within UpdateTransform()
        _translation += movementDirection * KeyboardMovementSpeed * speed;
    }

    private void KeyboardAndGamePadBasedRotation(float deltaTime)
    {
        // See Keyboard & Gamepad translation's deltaTime usage

        float speed = 1f * deltaTime;

        Vector2 rotation = Vector2.Zero;

        if (Gamepad && Input.HasGamePad)
        {
            GamePadState padState = Input.DefaultGamePad.State;
            rotation.X += padState.RightThumb.Y;
            rotation.Y += -padState.RightThumb.X;
        }

        if (Input.HasKeyboard)
        {
            if (Input.IsKeyDown(Keys.NumPad2))
                rotation.X += 1;
            if (Input.IsKeyDown(Keys.NumPad8))
                rotation.X -= 1;

            if (Input.IsKeyDown(Keys.NumPad4))
                rotation.Y += 1;
            if (Input.IsKeyDown(Keys.NumPad6))
                rotation.Y -= 1;

            // See Keyboard & Gamepad translation's Normalize() usage
            if (rotation.Length() > 1f)
                rotation = Vector2.Normalize(rotation);
        }

        // Modulate by speed
        rotation *= KeyboardRotationSpeed * speed;

        // Finally, push all of that to pitch & yaw which are going to be used within UpdateTransform()
        _pitch += rotation.X;
        _yaw += rotation.Y;
    }

    private void MouseMovementAndGestures()
    {
        // This type of input should not use delta time at all, they already are frame-rate independent.
        // Let's say that you are going to move your finger/mouse for one second over 40 units, it doesn't matter
        // the number of frames occurring within that time frame, each frame will receive the right amount of delta:
        // a quarter of a second -> 10 units, half a second -> 20 units, one second -> your 40 units.

        if (Input.HasMouse)
        {
            // Rotate with a mouse
            if (Input.IsMouseButtonDown(MouseButton.Right))
            {
                Input.LockMousePosition();
                Game.IsMouseVisible = false;

                _yaw -= Input.MouseDelta.X * MouseRotationSpeed.X;
                _pitch -= Input.MouseDelta.Y * MouseRotationSpeed.Y;
            }
            else
            {
                Input.UnlockMousePosition();
                Game.IsMouseVisible = true;
            }
        }

        // Handle gestures
        foreach (var gestureEvent in Input.GestureEvents)
        {
            switch (gestureEvent.Type)
            {
                // Rotate by dragging
                case GestureType.Drag:
                    var drag = (GestureEventDrag)gestureEvent;
                    var dragDistance = drag.DeltaTranslation;
                    _yaw = -dragDistance.X * TouchRotationSpeed.X;
                    _pitch = -dragDistance.Y * TouchRotationSpeed.Y;
                    break;

                // Move along z-axis by scaling and in xy-plane by multitouch dragging
                case GestureType.Composite:
                    var composite = (GestureEventComposite)gestureEvent;
                    _translation.X = -composite.DeltaTranslation.X * TouchMovementSpeed.X;
                    _translation.Y = -composite.DeltaTranslation.Y * TouchMovementSpeed.Y;
                    _translation.Z = MathF.Log(composite.DeltaScale + 1) * TouchMovementSpeed.Z;
                    break;
            }
        }
    }

    private void UpdateTransform()
    {
        // Get the local coordinate system
        var rotation = Matrix.RotationQuaternion(Entity.Transform.Rotation);

        // Enforce the global up-vector by adjusting the local x-axis
        var right = Vector3.Cross(rotation.Forward, _upVector);
        var up = Vector3.Cross(right, rotation.Forward);

        // Stabilize
        right.Normalize();
        up.Normalize();

        // Adjust pitch. Prevent it from exceeding up and down facing. Stabilize edge cases.
        var currentPitch = MathUtil.PiOverTwo - MathF.Acos(Vector3.Dot(rotation.Forward, _upVector));
        _pitch = MathUtil.Clamp(currentPitch + _pitch, -MaximumPitch, MaximumPitch) - currentPitch;

        Vector3 finalTranslation = _translation;
        finalTranslation.Z = -finalTranslation.Z;
        finalTranslation = Vector3.TransformCoordinate(finalTranslation, rotation);

        // Move in local coordinates
        Entity.Transform.Position += finalTranslation;

        // Yaw around global up-vector, pitch and roll in local space
        Entity.Transform.Rotation *= Quaternion.RotationAxis(right, _pitch) * Quaternion.RotationAxis(_upVector, _yaw);
    }

    /// <summary>
    /// Reset camera to default position and orthographic size when the 'H' key is pressed
    /// </summary>
    private void ResetCameraToDefault()
    {
        if (!Input.IsKeyPressed(Keys.H)) return;

        Entity.Transform.Position = _defaultCameraPosition;
        Entity.Transform.Rotation = _defaultCameraRotation;
    }
}