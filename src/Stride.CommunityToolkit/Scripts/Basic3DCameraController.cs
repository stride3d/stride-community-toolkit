using Stride.CommunityToolkit.Rendering;
using Stride.CommunityToolkit.Rendering.Overlay;
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
[Display("Basic 3D Camera Controller")]
[ComponentCategory("Camera")]
public class Basic3DCameraController : SyncScript
{
    private const float MaximumPitch = MathUtil.PiOverTwo * 0.99f;
    private readonly Anchor? _helpPosition;
    private Vector3 _translation;
    private Vector3 _defaultCameraPosition;
    private Quaternion _defaultCameraRotation;
    private float _yaw;
    private float _pitch;

    private TextOverlaySection? _instructions;

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
    /// Gets or sets whether the camera's help is put on the shared <see cref="TextOverlay"/> at all. Defaults to
    /// <see langword="true"/>, and must be set before the script starts.
    /// </summary>
    public bool ShowHelp { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the camera's help section is shown.
    /// </summary>
    /// <remarks>
    /// This hides only the camera's own lines. The overlay itself, and anything else contributing to
    /// it, is toggled with <see cref="TextOverlay.ToggleKey"/>.
    /// </remarks>
    public bool ShowInstructions
    {
        get => _instructions?.Enabled ?? false;
        set { if (_instructions is not null) _instructions.Enabled = value; }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Basic3DCameraController"/> class, optionally moving the
    /// shared overlay to an anchor.
    /// </summary>
    /// <param name="helpPosition">Where the shared <see cref="TextOverlay"/> - and so the camera help - is
    /// drawn. <see langword="null"/>, the default, leaves the overlay where it is, so a position set elsewhere
    /// (for example before the controller starts) is kept.
    /// </param>
    public Basic3DCameraController(Anchor? helpPosition = null)
    {
        _helpPosition = helpPosition;
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

        // The overlay is shared, and this Start runs a frame after the game's own start callback, so
        // its position is only touched when an anchor was asked for explicitly - otherwise a position
        // set by the example before the controller started would be silently overwritten
        if (ShowHelp)
        {
            var overlay = TextOverlay.GetOrCreate(Game);

            if (_helpPosition is { } position)
            {
                overlay.Position = position;
            }

            // Not static: the last two lines read the live transform. The factory runs every frame the
            // section is drawn expanded, so the numbers track the camera as it is flown around.
            _instructions = overlay.AddCollapsibleSection("Camera", "Camera controls", HelpToggleKey, () =>
            {
                var position = Entity.Transform.Position;

                // Printed as yaw/pitch/roll in degrees, which is what Add3DCamera's initialRotation and
                // SetCameraRotation both take - X is Yaw, Y is Pitch, Z is Roll. Printing plain XYZ Euler
                // here would give numbers that look right, paste cleanly, and aim the camera somewhere
                // else entirely.
                var rotation = Entity.Transform.Rotation.YawPitchRoll;

                return
                [
                    // Keys first, one per line, each in the brackets the title uses, then the live
                    // values with two fixed decimals so they stop changing width as the camera flies
                    new("F3", "Reposition help", Color.LightGoldenrodYellow),
                    new("F4", "Hide help", Color.LightGoldenrodYellow),
                    new("H", "Reset camera", Color.LightGreen),
                    new("W A S D", "Move", Color.LightGreen),
                    new("Arrow keys", "Move", Color.LightGreen),
                    new(["Q", "E"], "Ascend / descend", Color.LightGreen),
                    new("Numpad 2 4 6 8", "Rotate", Color.LightGreen),
                    new("Shift", "Hold to move faster", Color.LightGreen),
                    new("Right drag", "Rotate", Color.LightGreen),
                    new($"Position: {position.X:0.00}, {position.Y:0.00}, {position.Z:0.00}", Color.Yellow),
                    new($"Rotation (YPR): {MathUtil.RadiansToDegrees(rotation.X):0.00}, {MathUtil.RadiansToDegrees(rotation.Y):0.00}, {MathUtil.RadiansToDegrees(rotation.Z):0.00}", Color.Yellow),
                ];
            }, HelpCollapsed, order: -100);
        }

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

    private void KeyboardAndGamePadBasedMovement(float deltaTime)
    {
        // Scaled by the frame time, so the speed is in units per second whatever the frame rate
        var speed = deltaTime;

        var movementDirection = Vector3.Zero;

        if (Gamepad && Input.HasGamePad)
        {
            var padState = Input.DefaultGamePad.State;
            // LeftThumb can be positive or negative on both axes (pushed to the right or to the left)
            movementDirection.Z += padState.LeftThumb.Y;
            movementDirection.X += padState.LeftThumb.X;

            // Triggers are always positive, in this case using one to increase and the other to decrease
            movementDirection.Y -= padState.LeftTrigger;
            movementDirection.Y += padState.RightTrigger;

            // Sprint while A or either shoulder is held
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
            if (Input.IsShiftDown())
                speed *= SpeedFactor;

            // Normalise only when longer than one, so a half-pushed stick still moves slowly
            if (movementDirection.Length() > 1f)
                movementDirection = Vector3.Normalize(movementDirection);
        }

        // Finally, push all of that to the translation variable which will be used within UpdateTransform()
        _translation += movementDirection * KeyboardMovementSpeed * speed;
    }

    private void KeyboardAndGamePadBasedRotation(float deltaTime)
    {
        var speed = deltaTime;
        var rotation = Vector2.Zero;

        if (Gamepad && Input.HasGamePad)
        {
            var padState = Input.DefaultGamePad.State;
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
        // Mouse and touch deltas are already distances per frame, so no frame time here

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
        var right = Vector3.Cross(rotation.Forward, Vector3.UnitY);
        var up = Vector3.Cross(right, rotation.Forward);

        // Stabilize
        right.Normalize();
        up.Normalize();

        // Adjust pitch. Prevent it from exceeding up and down facing. Stabilize edge cases.
        var currentPitch = MathUtil.PiOverTwo - MathF.Acos(Vector3.Dot(rotation.Forward, Vector3.UnitY));
        _pitch = MathUtil.Clamp(currentPitch + _pitch, -MaximumPitch, MaximumPitch) - currentPitch;

        Vector3 finalTranslation = _translation;
        finalTranslation.Z = -finalTranslation.Z;
        finalTranslation = Vector3.TransformCoordinate(finalTranslation, rotation);

        // Move in local coordinates
        Entity.Transform.Position += finalTranslation;

        // Yaw around global up-vector, pitch and roll in local space
        Entity.Transform.Rotation *= Quaternion.RotationAxis(right, _pitch) * Quaternion.RotationAxis(Vector3.UnitY, _yaw);
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