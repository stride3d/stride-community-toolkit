using Stride.CommunityToolkit.Rendering.Overlay;
using Stride.Engine;
using Stride.Input;

namespace Stride.CommunityToolkit.Scripts;

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
/// through the floor. Both angles are measured against <see cref="Up"/>, the world's up unless the
/// game says otherwise: a character walking round a planet sets it to the direction away from the
/// centre every frame, and the camera orbits and pitches over the surface. <see cref="Forward"/> and
/// <see cref="Right"/> are the matching ground directions, what a character that walks "forward"
/// wants to read. The 'F2' help section is shared with the other controllers through
/// <see cref="TextOverlay"/>.
/// </para>
/// </remarks>
[Display("Follow Camera Controller")]
[ComponentCategory("Camera")]
public class Basic3DFollowCameraController : SyncScript
{
    private Vector2? _lastMousePosition;
    private Vector3 _pivot;
    private Vector3 _back = Vector3.UnitZ;
    private Vector3 _defaultBack = Vector3.UnitZ;
    private float _pitch;
    private float _distance;
    private float _shownDistance;
    private float _elapsed;
    private float _defaultPitch;
    private float _defaultDistance;
    private TextOverlaySection? _instructions;

    /// <summary>Gets or sets the entity the camera follows. With none set the camera stays where it is.</summary>
    public Entity? Target { get; set; }

    /// <summary>Gets or sets the point the camera looks at, relative to the target's position: a character's chest rather than its feet. Defaults to 1.5 up.</summary>
    public Vector3 TargetOffset { get; set; } = new(0f, 1.5f, 0f);

    /// <summary>
    /// Gets or sets the direction the camera treats as up, which the yaw turns round and the pitch leans away from.
    /// Defaults to the world's up. On a planet, set it to the direction from the centre to the target every frame.
    /// </summary>
    public Vector3 Up { get; set; } = Vector3.UnitY;

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

    /// <summary>
    /// Gets or sets how long the camera takes to ease most of the way back out once an obstacle has cleared, in
    /// seconds. Coming in is always immediate, so the camera never clips; going back out at once would be a jump.
    /// <c>0</c> returns at once. Defaults to <c>0.3</c>.
    /// </summary>
    public float ObstacleReturnSmoothing { get; set; } = 0.3f;

    /// <summary>Gets or sets the key that collapses and expands the help section. Defaults to <see cref="Keys.F2"/>.</summary>
    public Keys HelpToggleKey { get; set; } = Keys.F2;

    /// <summary>Gets or sets whether the help section starts collapsed. Defaults to <see langword="true"/>.</summary>
    public bool HelpCollapsed { get; set; } = true;

    /// <summary>Gets the point the camera is looking at: the target plus its offset, after the follow lag.</summary>
    public Vector3 Pivot => _pivot;

    /// <summary>
    /// Gets the camera's yaw in radians: the angle of its ground direction round <see cref="Up"/>, measured from the
    /// world's +Z laid into the ground. The direction itself is kept from frame to frame and carried along as the
    /// up changes, so a walk over a planet never flips the view; the angle is only its reading.
    /// </summary>
    public float Yaw
    {
        get
        {
            var (_, back, right) = Frame();

            return MathF.Atan2(Vector3.Dot(_back, right), Vector3.Dot(_back, back));
        }
    }

    /// <summary>Gets the direction the camera looks along the ground: the look direction with the pitch taken out. A character that walks "forward" walks this way.</summary>
    public Vector3 Forward { get; private set; } = -Vector3.UnitZ;

    /// <summary>Gets the direction to the camera's right along the ground.</summary>
    public Vector3 Right { get; private set; } = Vector3.UnitX;

    /// <summary>
    /// Reads the starting pose from the entity's position relative to the pivot, or takes the defaults when
    /// the entity sits on it, and registers the shared help section.
    /// </summary>
    public override void Start()
    {
        _pivot = PivotOf(Target);
        _distance = Math.Clamp(Distance, MinDistance, MaxDistance);
        _pitch = Math.Clamp(Pitch, MinPitch, MaxPitch);

        var (up, back, _) = Frame();
        var offset = Entity.Transform.Position - _pivot;

        _back = back;

        if (offset.LengthSquared() > MathUtil.ZeroTolerance)
        {
            _distance = Math.Clamp(offset.Length(), MinDistance, MaxDistance);
            var direction = offset / offset.Length();

            // The ground direction is the offset laid into the ground; pitch is the angle out of it
            _back = Level(direction, up, back);
            _pitch = Math.Clamp(-MathF.Asin(Math.Clamp(Vector3.Dot(direction, up), -1f, 1f)), MinPitch, MaxPitch);
        }

        _defaultBack = _back;
        _defaultPitch = _pitch;
        _defaultDistance = _distance;
        _shownDistance = _distance;

        Apply();

        _instructions = TextOverlay.GetOrCreate(Game).AddCollapsibleSection(
            "Camera", "Camera controls", HelpToggleKey, () =>
            [
                new("F3", "Reposition help", Color.LightGoldenrodYellow),
                new("H", "Reset view"),
                new($"{OrbitButton} drag", "Orbit the target"),
                new("Mouse wheel", "Zoom"),
                new("Shift", "Hold for faster zoom"),
                new($"Distance: {_distance:0.00}  Yaw: {MathUtil.RadiansToDegrees(Yaw):0.0}°  Pitch: {MathUtil.RadiansToDegrees(_pitch):0.0}°", Color.Yellow),
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

        _elapsed = (float)Game.UpdateTime.Elapsed.TotalSeconds;
        Follow(_elapsed);

        var mousePosition = Input.MousePosition;
        var orbiting = Input.IsMouseButtonDown(OrbitButton);

        if (orbiting && _lastMousePosition is { } last)
        {
            var delta = mousePosition - last;

            // Dragging right swings the camera to the right around the target; dragging up climbs
            _back = Vector3.Transform(_back, Quaternion.RotationAxis(Frame().Up, -delta.X * OrbitSensitivity));
            _pitch = Math.Clamp(_pitch - delta.Y * OrbitSensitivity, MinPitch, MaxPitch);
        }

        _lastMousePosition = orbiting ? mousePosition : null;

        Zoom();

        if (Input.IsKeyPressed(Keys.H))
        {
            _back = _defaultBack;
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
        var (up, back0, _) = Frame();

        // The ground direction is carried from the last frame and laid into this frame's ground, which is what keeps
        // the view continuous as the up turns under a walker; pitch then leans it out of the ground about the right axis
        _back = Level(_back, up, back0);

        var level = _back;
        var right = Vector3.Cross(up, level);
        var back = Vector3.Transform(level, Quaternion.RotationAxis(right, _pitch));
        var wanted = _pivot + back * _distance;
        var allowed = _distance;

        if (ObstacleTest?.Invoke(_pivot, wanted) is { } blocked)
        {
            allowed = Math.Clamp(blocked - ObstacleMargin, 0f, _distance);
        }

        // In at once, out with a lag: the camera never clips, and never jumps when the way behind it clears
        var distance = allowed < _shownDistance || ObstacleReturnSmoothing <= 0f
            ? allowed
            : _shownDistance + (allowed - _shownDistance) * (1f - MathF.Exp(-_elapsed / ObstacleReturnSmoothing));

        _shownDistance = distance;

        Forward = -level;
        Right = right;

        var basis = Matrix.Identity;

        basis.Right = right;
        basis.Up = Vector3.Cross(back, right);
        basis.Backward = back;

        Entity.Transform.Rotation = Quaternion.RotationMatrix(basis);
        Entity.Transform.Position = _pivot + back * distance;
    }

    /// <summary>A direction laid into the ground the up axis defines and normalised, or the fallback when it stands along the up.</summary>
    private static Vector3 Level(Vector3 direction, Vector3 up, Vector3 fallback)
    {
        var level = direction - up * Vector3.Dot(direction, up);

        return level.LengthSquared() < 1e-6f ? fallback : Vector3.Normalize(level);
    }

    /// <summary>
    /// The frame the yaw is read in: <see cref="Up"/> normalised, the back direction of zero yaw, which is the
    /// world's +Z laid into the ground, or +X when up runs along Z, and the right direction that goes with it.
    /// </summary>
    private (Vector3 Up, Vector3 Back, Vector3 Right) Frame()
    {
        var up = Up.LengthSquared() > MathUtil.ZeroTolerance ? Vector3.Normalize(Up) : Vector3.UnitY;
        var back = Vector3.UnitZ - up * Vector3.Dot(Vector3.UnitZ, up);

        if (back.LengthSquared() < 1e-6f) back = Vector3.UnitX - up * Vector3.Dot(Vector3.UnitX, up);

        back.Normalize();

        return (up, back, Vector3.Cross(up, back));
    }
}