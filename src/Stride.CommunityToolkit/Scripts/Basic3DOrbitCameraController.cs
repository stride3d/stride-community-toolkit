using Stride.CommunityToolkit.Scripts.Utilities;
using Stride.Core;
using Stride.Engine;
using Stride.Input;

namespace Stride.CommunityToolkit.Scripts;

/// <summary>
/// An orbit camera controller: the camera circles a target point instead of flying freely - the natural way
/// to inspect one object, a scene centrepiece or a 3D chart. Dragging with <see cref="OrbitButton"/> orbits,
/// the mouse wheel moves closer or further, dragging with <see cref="PanButton"/> pans the target, and 'H'
/// returns to the starting view.
/// </summary>
/// <remarks>
/// <para>
/// Attach it to an entity with a <see cref="CameraComponent"/>; the entity's starting position relative to
/// <see cref="Target"/> provides the initial distance and angles, so framing the camera before this script
/// starts (for example with a chart's <c>FrameCamera</c>) also frames the orbit.
/// </para>
/// <para>
/// Yaw is unconstrained; pitch is clamped between <see cref="MinPitch"/> and <see cref="MaxPitch"/> so the
/// camera cannot flip over the top. Zoom is multiplicative - every wheel notch changes the distance by the
/// same fraction - and clamped between <see cref="MinDistance"/> and <see cref="MaxDistance"/>. While the
/// middle button is both <see cref="PanButton"/> and held down, wheel input is ignored, the same guard
/// <see cref="Basic2DCameraController"/> uses against the wheel-press-rolls-a-notch problem.
/// </para>
/// <para>
/// The 'F2' help section is shared with the other controllers through <see cref="DebugOverlay"/> and shows
/// the live target, distance and angles.
/// </para>
/// </remarks>
[Display("Orbit Camera Controller")]
[ComponentCategory("Camera")]
public class Basic3DOrbitCameraController : SyncScript
{
    private CameraComponent? _camera;
    private Vector2? _lastMousePosition;
    private float _yaw;
    private float _pitch;
    private float _distance = 10f;
    private Vector3 _defaultTarget;
    private float _defaultYaw;
    private float _defaultPitch;
    private float _defaultDistance;
    private DebugOverlaySection? _instructions;

    /// <summary>
    /// Gets or sets the point the camera orbits and looks at. Move it to follow something; the camera keeps
    /// its angles and distance.
    /// </summary>
    public Vector3 Target { get; set; } = Vector3.Zero;

    /// <summary>Gets or sets the closest the camera can dolly in, in world units. Defaults to <c>0.5</c>.</summary>
    public float MinDistance { get; set; } = 0.5f;

    /// <summary>Gets or sets the furthest the camera can dolly out, in world units. Defaults to <c>500</c>.</summary>
    public float MaxDistance { get; set; } = 500f;

    /// <summary>
    /// Gets or sets how far a drag orbits, in radians per full window of mouse travel. Defaults to <c>4</c> -
    /// dragging across the whole window turns the camera roughly two thirds of a full circle.
    /// </summary>
    public float OrbitSensitivity { get; set; } = 4f;

    /// <summary>Gets or sets the fraction the distance changes per mouse-wheel notch. Defaults to <c>0.1</c> (10 %).</summary>
    public float ZoomStep { get; set; } = 0.1f;

    /// <summary>Gets or sets the multiplier applied to zoom and pan while a shift key is held. Defaults to <c>5</c>.</summary>
    public float SpeedFactor { get; set; } = 5.0f;

    /// <summary>Gets or sets the mouse button that orbits while held. Defaults to the left button.</summary>
    public MouseButton OrbitButton { get; set; } = MouseButton.Left;

    /// <summary>Gets or sets the mouse button that pans the target while held. Defaults to the middle button.</summary>
    public MouseButton PanButton { get; set; } = MouseButton.Middle;

    /// <summary>Gets or sets the lowest pitch in radians - how far above the target the camera may climb. Defaults to about −85°.</summary>
    public float MinPitch { get; set; } = -1.48f;

    /// <summary>Gets or sets the highest pitch in radians - how far below the target the camera may sink. Defaults to about 85°.</summary>
    public float MaxPitch { get; set; } = 1.48f;

    /// <summary>Gets or sets the key that collapses and expands the help section. Defaults to <see cref="Keys.F2"/>.</summary>
    public Keys HelpToggleKey { get; set; } = Keys.F2;

    /// <summary>Gets or sets whether the help section starts collapsed. Defaults to <see langword="true"/>.</summary>
    public bool HelpCollapsed { get; set; } = true;

    /// <summary>
    /// Reads the starting pose - distance and angles come from the entity's position relative to
    /// <see cref="Target"/> - and registers the shared help section.
    /// </summary>
    public override void Start()
    {
        _camera = Entity.Get<CameraComponent>();

        var offset = Entity.Transform.Position - Target;

        if (offset.LengthSquared() > MathUtil.ZeroTolerance)
        {
            _distance = Math.Clamp(offset.Length(), MinDistance, MaxDistance);
            var direction = offset / offset.Length();
            _yaw = MathF.Atan2(direction.X, direction.Z);
            _pitch = Math.Clamp(-MathF.Asin(Math.Clamp(direction.Y, -1f, 1f)), MinPitch, MaxPitch);
        }

        _defaultTarget = Target;
        _defaultYaw = _yaw;
        _defaultPitch = _pitch;
        _defaultDistance = _distance;

        Apply();

        _instructions = DebugOverlay.GetOrCreate(Game).AddCollapsibleSection(
            "Camera", "Camera controls", HelpToggleKey, () =>
            [
                // Keys first, one per line, each in the brackets the title uses, then the live values
                // with fixed decimals so they stop changing width as the camera orbits
                new("F3", "Reposition help", Color.LightGoldenrodYellow),
                new("H", "Reset view"),
                new($"{OrbitButton} drag", "Orbit"),
                new($"{PanButton} drag", "Pan target"),
                new("Mouse wheel", "Zoom"),
                new("Shift", "Hold for faster zoom and pan"),
                new($"Target: {Target.X:0.00}, {Target.Y:0.00}, {Target.Z:0.00}", Color.Yellow),
                new($"Distance: {_distance:0.00}  Yaw: {MathUtil.RadiansToDegrees(_yaw):0.0}°  Pitch: {MathUtil.RadiansToDegrees(_pitch):0.0}°", Color.Yellow),
            ], HelpCollapsed, order: -100);
    }

    /// <summary>
    /// Processes orbit, pan, zoom and reset input, then places the camera on its orbit.
    /// </summary>
    public override void Update()
    {
        if (_camera is null)
            return;

        var mousePosition = Input.MousePosition;
        var panning = Input.IsMouseButtonDown(PanButton);
        var orbiting = !panning && Input.IsMouseButtonDown(OrbitButton);

        if ((panning || orbiting) && _lastMousePosition is { } last)
        {
            var delta = mousePosition - last;

            if (panning)
            {
                Pan(delta);
            }
            else
            {
                // Dragging right orbits the camera to the right around the target; dragging up climbs
                _yaw -= delta.X * OrbitSensitivity;
                _pitch = Math.Clamp(_pitch - delta.Y * OrbitSensitivity, MinPitch, MaxPitch);
            }
        }

        _lastMousePosition = panning || orbiting ? mousePosition : null;

        Zoom();

        if (Input.IsKeyPressed(Keys.H))
        {
            Target = _defaultTarget;
            _yaw = _defaultYaw;
            _pitch = _defaultPitch;
            _distance = _defaultDistance;
        }

        Apply();
    }

    private void Pan(Vector2 delta)
    {
        // A full-window drag moves the target by the world size of the view at the target's distance,
        // so the point under the cursor stays roughly under the cursor - the 2D controller's rule
        var backBuffer = Game.GraphicsDevice.Presenter.BackBuffer;
        var aspect = (float)backBuffer.Width / backBuffer.Height;
        var fov = MathUtil.DegreesToRadians(_camera!.VerticalFieldOfView);
        var worldHeight = 2f * _distance * MathF.Tan(fov * 0.5f);

        if (Input.IsKeyDown(Keys.LeftShift) || Input.IsKeyDown(Keys.RightShift))
        {
            worldHeight *= SpeedFactor;
        }

        var rotation = Quaternion.RotationYawPitchRoll(_yaw, _pitch, 0f);
        var right = Vector3.Transform(Vector3.UnitX, rotation);
        var up = Vector3.Transform(Vector3.UnitY, rotation);

        Target += right * (-delta.X * worldHeight * aspect) + up * (delta.Y * worldHeight);
    }

    private void Zoom()
    {
        // Pressing the wheel to pan almost always rolls it a notch too; while the middle button is both
        // the pan button and held down, the wheel is a pan grip, not a zoom request
        if (PanButton == MouseButton.Middle && Input.IsMouseButtonDown(MouseButton.Middle))
            return;

        var wheel = Input.MouseWheelDelta;

        if (wheel == 0f)
            return;

        if (Input.IsKeyDown(Keys.LeftShift) || Input.IsKeyDown(Keys.RightShift))
        {
            wheel *= SpeedFactor;
        }

        // Multiplicative, so every notch changes the distance by the same fraction near or far
        _distance = Math.Clamp(_distance * MathF.Pow(1f + ZoomStep, -wheel), MinDistance, MaxDistance);
    }

    /// <summary>
    /// Places the camera on its orbit: at <see cref="Target"/> plus the rotated distance offset, looking at
    /// the target.
    /// </summary>
    private void Apply()
    {
        var rotation = Quaternion.RotationYawPitchRoll(_yaw, _pitch, 0f);

        Entity.Transform.Rotation = rotation;
        Entity.Transform.Position = Target + Vector3.Transform(new Vector3(0f, 0f, _distance), rotation);
    }
}