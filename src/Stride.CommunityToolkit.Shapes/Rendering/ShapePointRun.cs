using Stride.Core.Mathematics;

namespace Stride.CommunityToolkit.Shapes;

/// <summary>
/// Where a shape's points sit in the batch's point buffer, and how they were normalized on the way
/// in: every point was shifted by <paramref name="Center"/> and divided by <paramref name="LocalScale"/>
/// so the pixel stage reads them ready to use, and the record carries both so the shader can undo it.
/// </summary>
/// <param name="Offset">Index of the shape's first point in the batch's point list.</param>
/// <param name="Count">How many points the shape has.</param>
/// <param name="Center">Centre of the points' bounding box, in the plane's local units: what the points were shifted by.</param>
/// <param name="LocalScale">The rounding radius plus half the widest extent: what the shifted points were divided by.</param>
internal readonly record struct ShapePointRun(int Offset, int Count, Vector2 Center, float LocalScale);