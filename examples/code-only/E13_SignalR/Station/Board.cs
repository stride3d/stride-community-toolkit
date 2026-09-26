using Stride.Core.Mathematics;

namespace E13_SignalR.Station;

/// <summary>
/// A flat panel placed in the world: a centre, a size and the direction it faces. Everything drawn on
/// it - ShapeBatch rectangles, world text - is placed in the board's own (u, v) coordinates, so a
/// layout is worked out once on paper and the board decides where in the scene that is. The same
/// coordinates answer "which button is under the mouse", by intersecting a pick ray with the plane.
/// </summary>
public sealed class Board
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Board"/> class.
    /// </summary>
    /// <param name="center">The centre of the panel in the world.</param>
    /// <param name="facing">The direction the panel faces - typically towards the camera, so it is seen square-on.</param>
    /// <param name="size">Width along the board's X and height along its Y, in world units.</param>
    public Board(Vector3 center, Vector3 facing, Vector2 size)
    {
        Center = center;
        Size = size;
        Normal = Vector3.Normalize(facing);

        // Right is horizontal whatever the tilt, so text never rolls; up follows the tilt
        AxisX = Vector3.Normalize(Vector3.Cross(Vector3.UnitY, Normal));
        AxisY = Vector3.Cross(Normal, AxisX);
        Rotation = Orientation(AxisX, AxisY, Normal);
    }

    public Vector3 Center { get; }

    public Vector2 Size { get; }

    public Vector3 Normal { get; }

    public Vector3 AxisX { get; }

    public Vector3 AxisY { get; }

    /// <summary>The rotation a world-text entity needs to lie flat on the board and read the right way round.</summary>
    public Quaternion Rotation { get; }

    /// <summary>Half the width and height - the board runs from -Half to +Half in both directions.</summary>
    public Vector2 Half => Size / 2f;

    /// <summary>
    /// A point on the board. The lift keeps text and inner shapes a hair in front of the panel's
    /// fill, so the two never fight over the same depth.
    /// </summary>
    public Vector3 Place(float u, float v, float lift = 0.03f) => Center + AxisX * u + AxisY * v + Normal * lift;

    public Vector3 Place(Vector2 local, float lift = 0.03f) => Place(local.X, local.Y, lift);

    /// <summary>
    /// The rotation that maps local X, Y and Z onto the given world axes - the form the world-text
    /// renderer reads an entity's orientation in, so text faces along <paramref name="forward"/>.
    /// </summary>
    public static Quaternion Orientation(Vector3 right, Vector3 up, Vector3 forward)
        => Quaternion.RotationMatrix(new Matrix
        {
            M11 = right.X,
            M12 = right.Y,
            M13 = right.Z,
            M21 = up.X,
            M22 = up.Y,
            M23 = up.Z,
            M31 = forward.X,
            M32 = forward.Y,
            M33 = forward.Z,
            M44 = 1f,
        });
}