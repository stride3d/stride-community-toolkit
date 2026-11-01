using Stride.CommunityToolkit.Rendering.Overlay;
using Stride.Engine;
using Stride.Input;

namespace Stride.CommunityToolkit.Scripts;

/// <summary>
/// The test a follow camera makes before it settles: whether anything stands on the line from
/// <paramref name="from"/>, the point it looks at, to <paramref name="to"/>, where it wants to be.
/// </summary>
/// <param name="from">The pivot the camera looks at.</param>
/// <param name="to">Where the camera would sit with nothing in the way.</param>
/// <returns>The distance from <paramref name="from"/> to the first obstacle, or <see langword="null"/> for a clear line.</returns>
public delegate float? CameraObstacleTest(Vector3 from, Vector3 to);

/// <summary>
/// A third-person follow camera: it hangs behind and above a target entity, keeps it in view as it moves,
/// and lets the player swing round it with the mouse. Dragging with <see cref="OrbitButton"/> orbits, the
/// mouse wheel moves closer or further, and 'H' returns to the starting view. With an
/// <see cref="ObstacleTest"/> the camera comes forward rather than pass through a wall between it and
/// the target.
/// </summary>
/// <remarks>
/// <para>
/// Attach it to an entity with a <see cref="CameraComponent"/> and set <see cref="Target"/>. The camera
/// looks at the target's position plus <see cref="TargetOffset"/>, the pivot, which follows the target
/// with a short lag set by <see cref="FollowSmoothing"/> so the view does not jolt with every step. The
/// entity's starting position relative to the pivot gives the first distance and angles; an entity sitting
/// on the pivot starts at <see cref="Distance"/>, behind the target and a little above.
/// </para>
/// <para>
/// The controller knows nothing of physics. A game that wants the camera to stay out of walls gives it an
/// <see cref="ObstacleTest"/>, a raycast from the pivot to the wanted position in whatever engine it
/// uses, and the camera stops <see cref="ObstacleMargin"/> short of the first hit.
/// </para>
/// <para>
/// Pitch is the camera's own: negative looks down from above the target, and the clamps
/// <see cref="MinPitch"/> and <see cref="MaxPitch"/> keep it from flipping over the top or sinking
/// through the floor. The 'F2' help section is shared with the other controllers through
/// <see cref="TextOverlay"/>.
/// </para>
/// </remarks>
[Display("Follow Camera Controller")]
[ComponentCategory("Camera")]
public class Basic3DFollowCameraController : SyncScript
{
    private Vector2? _lastMousePosition;
    private Vector3 _pivot;
    private float _yaw;
    private float _pitch;
    private float _distance;
    private float _defaultYaw;
    private float _defaultPitch;
    private float _defaultDistance;
    private TextOverlaySection? _instructions;

    /// <summary>Gets or sets the entity the camera follows. With none set the camera stays where it is.</summary>
    public Entity? Target { get; set; }

    /// <summary>Gets or sets the point the camera looks at, relative to the target's position: a character's chest rather than its feet. Defaults to 1.5 up.</summary>
    public Vector3 TargetOffset { get; set; } = new(0f, 1.5f, 0f);

    /// <summary>Gets or sets the distance the camera starts at when its entity begins on the pivot, in world units. Defaults to <c>6</c>.</summary>
    public float Distance { get; set; } = 6f;

    /// <summary>Gets or sets the pitch the camera starts at when its entity begins on the pivot, in radians. Defaults to about −20°, looking down a little.</summary>
    public float Pitch { get; set; } = -0.35f;

    /// <summary>Gets or sets the closest the camera can come, in world units. Defaults to <c>1</c>.</summary>
    public float MinDistance { get; set; } = 1f;

    /// <summary>Gets or sets the furthest the camera can go, in world units. Defaults to <c>30</c>.</summary>
    public float MaxDistance { get; set; } = 30f;

    /// <summary>
    /// Gets or sets how long the pivot takes to close most of the gap to the target, in seconds: the lag that
    /// smooths a character's steps out of the view. <c>0</c> follows exactly. Defaults to <c>0.12</c>.
    /// </summary>
    public float FollowSmoothing { get; set; } = 0.12f;

    /// <summary>Gets or sets how far a drag orbits, in radians per full window of mouse travel. Defaults to <c>4</c>.</summary>
    public float OrbitSensitivity { get; set; } = 4f;

    /// <summary>Gets or sets the fraction the distance changes per mouse-wheel notch. Defaults to <c>0.1</c> (10 %).</summary>
    public float ZoomStep { get; set; } = 0.1f;

    /// <summary>Gets or sets the multiplier applied to zoom while a shift key is held. Defaults to <c>5</c>.</summary>
    public float SpeedFactor { get; set; } = 5.0f;

    /// <summary>Gets or sets the mouse button that orbits while held. Defaults to the right button, leaving the left one to the game.</summary>
    public MouseButton OrbitButton { get; set; } = MouseButton.Right;

    /// <summary>Gets or sets the lowest pitch in radians: how far above the target the camera may climb. Defaults to about −80°.</summary>
    public float MinPitch { get; set; } = -1.4f;

    /// <summary>Gets or sets the highest pitch in radians: how far below the pivot the camera may sink. Defaults to about 20°.</summary>
    public float MaxPitch { get; set; } = 0.35f;

    /// <summary>Gets or sets the test for walls between the pivot and the camera, or <see langword="null"/> to pass through everything.</summary>
    public CameraObstacleTest? ObstacleTest { get; set; }

    /// <summary>Gets or sets how far short of an obstacle the camera stops, in world units. Defaults to <c>0.3</c>.</summary>
    public float ObstacleMargin { get; set; } = 0.3f;

    /// <summary>Gets or sets the key that collapses and expands the help section. Defaults to <see cref="Keys.F2"/>.</summary>
    public Keys HelpToggleKey { get; set; } = Keys.F2;

    /// <summary>Gets or sets whether the help section starts collapsed. Defaults to <see langword="true"/>.</summary>
    public bool HelpCollapsed { get; set; } = true;

    /// <summary>Gets the point the camera is looking at: the target plus its offset, after the follow lag.</summary>
    public Vector3 Pivot => _pivot;

    /// <summary>Gets the camera's yaw in radians, the direction it looks along the ground; a character that walks "forward" walks this way.</summary>
    public float Yaw => _yaw;

    /// <summary>
    /// Reads the starting pose from the entity's position relative to the pivot, or takes the defaults when
    /// the entity sits on it, and registers the shared help section.
    /// </summary>
    public override void Start()
    {
        _pivot = PivotOf(Target);
        _distance = Math.Clamp(Distance, MinDistance, MaxDistance);
        _pitch = Math.Clamp(Pitch, MinPitch, MaxPitch);

        var offset = Entity.Transform.Position - _pivot;

        if (offset.LengthSquared() > MathUtil.ZeroTolerance)
        {
            _distance = Math.Clamp(offset.Length(), MinDistance, MaxDistance);
            var direction = offset / offset.Length();
            _yaw = MathF.Atan2(direction.X, direction.Z);
            _pitch = Math.Clamp(-MathF.Asin(Math.Clamp(direction.Y, -1f, 1f)), MinPitch, MaxPitch);
        }

        _defaultYaw = _yaw;
        _defaultPitch = _pitch;
        _defaultDistance = _distance;

        Apply();

        _instructions = TextOverlay.GetOrCreate(Game).AddCollapsibleSection(
            "Camera", "Camera controls", HelpToggleKey, () =>
            [
                new("F3", "Reposition help", Color.LightGoldenrodYellow),
                new("H", "Reset view"),
                new($"{OrbitButton} drag", "Orbit the target"),
                new("Mouse wheel", "Zoom"),
                new("Shift", "Hold for faster zoom"),
                new($"Distance: {_distance:0.00}  Yaw: {MathUtil.RadiansToDegrees(_yaw):0.0}°  Pitch: {MathUtil.RadiansToDegrees(_pitch):0.0}°", Color.Yellow),
            ], HelpCollapsed, order: -100);
    }

    /// <summary>
    /// Moves the pivot after the target, processes orbit, zoom and reset input, and places the camera,
    /// pulled in from any obstacle.
    /// </summary>
    public override void Update()
    {
        if (Target is null)
            return;

        Follow((float)Game.UpdateTime.Elapsed.TotalSeconds);

        var mousePosition = Input.MousePosition;
        var orbiting = Input.IsMouseButtonDown(OrbitButton);

        if (orbiting && _lastMousePosition is { } last)
        {
            var delta = mousePosition - last;

            // Dragging right swings the camera to the right around the target; dragging up climbs
            _yaw -= delta.X * OrbitSensitivity;
            _pitch = Math.Clamp(_pitch - delta.Y * OrbitSensitivity, MinPitch, MaxPitch);
        }

        _lastMousePosition = orbiting ? mousePosition : null;

        Zoom();

        if (Input.IsKeyPressed(Keys.H))
        {
            _yaw = _defaultYaw;
            _pitch = _defaultPitch;
            _distance = _defaultDistance;
        }

        Apply();
    }

    /// <summary>The target's position plus the offset, or where the camera is when there is no target yet.</summary>
    private Vector3 PivotOf(Entity? target)
        => target is null ? Entity.Transform.Position : target.Transform.WorldMatrix.TranslationVector + TargetOffset;

    private void Follow(float elapsed)
    {
        var wanted = PivotOf(Target);

        if (FollowSmoothing <= 0f)
        {
            _pivot = wanted;
            return;
        }

        // Exponential: the same fraction of the remaining gap closes each second whatever the frame rate
        _pivot += (wanted - _pivot) * (1f - MathF.Exp(-elapsed / FollowSmoothing));
    }

    private void Zoom()
    {
        var wheel = Input.MouseWheelDelta;

        if (wheel == 0f)
            return;

        if (Input.IsShiftDown())
        {
            wheel *= SpeedFactor;
        }

        // Multiplicative, so every notch changes the distance by the same fraction near or far
        _distance = Math.Clamp(_distance * MathF.Pow(1f + ZoomStep, -wheel), MinDistance, MaxDistance);
    }

    /// <summary>
    /// Places the camera behind the pivot along its yaw and pitch, at the wanted distance or as close as the
    /// first obstacle allows, looking at the pivot.
    /// </summary>
    private void Apply()
    {
        var rotation = Quaternion.RotationYawPitchRoll(_yaw, _pitch, 0f);
        var back = Vector3.Transform(Vector3.UnitZ, rotation);
        var wanted = _pivot + back * _distance;
        var distance = _distance;

        if (ObstacleTest?.Invoke(_pivot, wanted) is { } blocked)
        {
            distance = Math.Clamp(blocked - ObstacleMargin, 0f, _distance);
        }

        Entity.Transform.Rotation = rotation;
        Entity.Transform.Position = _pivot + back * distance;
    }
}