namespace Stride.CommunityToolkit.Scripts;

/// <summary>
/// The test a follow camera makes before it settles: whether anything stands on the line from
/// <paramref name="from"/>, the point it looks at, to <paramref name="to"/>, where it wants to be.
/// </summary>
/// <param name="from">The pivot the camera looks at.</param>
/// <param name="to">Where the camera would sit with nothing in the way.</param>
/// <returns>The distance from <paramref name="from"/> to the first obstacle, or <see langword="null"/> for a clear line.</returns>
public delegate float? CameraObstacleTest(Vector3 from, Vector3 to);