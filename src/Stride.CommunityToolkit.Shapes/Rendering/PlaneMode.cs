namespace Stride.CommunityToolkit.Shapes;

/// <summary>How a shape's flat plane is oriented in the world.</summary>
internal enum PlaneMode
{
    /// <summary>The caller's two axes are used as given.</summary>
    Fixed = 0,

    /// <summary>The shape faces the camera, aligned to the screen.</summary>
    Screen = 1,

    /// <summary>The X axis is kept and the plane swings about it to face the camera.</summary>
    Axial = 2,
}