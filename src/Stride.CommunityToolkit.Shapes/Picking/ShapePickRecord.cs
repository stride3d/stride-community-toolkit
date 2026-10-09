namespace Stride.CommunityToolkit.Shapes;

/// <summary>
/// What a pick needs to know about a tagged shape: the outline as the caller gave it, not as the
/// GPU packs it. Points are kept in the caller's own units, unshifted and unscaled, so the
/// distance the CPU measures is in those units too.
/// </summary>
/// <param name="Tag">The tag the shape was drawn with.</param>
/// <param name="Index">Its position in the frame's draw order; at equal depth the later shape is on top.</param>
/// <param name="Plane">Where the shape's plane sits; unused by a space stroke.</param>
/// <param name="Screen">Whether the shape was drawn on the screen, in scaled pixels from the top left.</param>
/// <param name="Slice">The angular cut, band, caps and run flags, as the draw call built them.</param>
/// <param name="Radius">The rounding radius, in the caller's units - or in pixels when the slice says so.</param>
/// <param name="Scale">The uniform scale on the shape's world footprint.</param>
/// <param name="BorderWidth">The border's width in scaled pixels; half of it lies outside the outline and counts as the shape.</param>
/// <param name="Offset">Where the shape's points start in the batch's pick points, or its space points for a space stroke.</param>
/// <param name="Count">How many points it has.</param>
internal readonly record struct ShapePickRecord(
    object Tag,
    int Index,
    ShapePlane Plane,
    bool Screen,
    ShapeSlice Slice,
    float Radius,
    float Scale,
    float BorderWidth,
    int Offset,
    int Count);