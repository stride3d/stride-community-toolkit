using Stride.CommunityToolkit.Games;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace Example02_GiveMeACube;

/// <summary>
/// Moves its entity in a horizontal circle around wherever the entity started.
/// </summary>
/// <remarks>
/// This is the point of the example: behaviour lives in a <see cref="SyncScript"/> attached to the
/// entity, instead of in the update callback passed to <c>game.Run(update: ...)</c>. Stride calls
/// <see cref="Start"/> once when the entity enters the scene and <see cref="Update"/> once per frame.
/// <para>
/// The script writes <c>Transform.Position</c> directly, which moves the mesh only. That is fine here
/// because the orbiting cube deliberately has no physics body - see Program.cs. To move something that
/// should collide, drive a kinematic <c>BodyComponent</c> with <c>SetTargetPose</c> instead.
/// </para>
/// </remarks>
public class OrbitScript : SyncScript
{
    private Vector3 _orbitCentre;
    private float _angle;

    /// <summary>
    /// Distance from the orbit centre, in world units.
    /// </summary>
    public float Radius { get; set; } = 3f;

    /// <summary>
    /// How fast the entity travels around the circle, in radians per second.
    /// </summary>
    public float AngularSpeed { get; set; } = 1f;

    /// <summary>
    /// Called once when the entity is added to the scene.
    /// </summary>
    public override void Start()
    {
        // Whatever position the entity was given in Program.cs becomes the centre of the circle
        _orbitCentre = Entity.Transform.Position;
    }

    /// <summary>
    /// Called once per frame.
    /// </summary>
    public override void Update()
    {
        // DeltaTime keeps the motion frame rate independent
        _angle += AngularSpeed * Game.DeltaTime();

        var offset = new Vector3(MathF.Sin(_angle), 0, MathF.Cos(_angle)) * Radius;

        Entity.Transform.Position = _orbitCentre + offset;
    }
}
