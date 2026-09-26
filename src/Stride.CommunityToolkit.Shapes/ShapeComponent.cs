using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Design;

namespace Stride.CommunityToolkit.Shapes;

/// <summary>
/// Draws a flat shape at this entity's world transform through a <see cref="ShapeBatch"/>, so shapes
/// take part in Stride's component system - scripts, hierarchy, enable and disable - without needing
/// a model or a material. Nothing else is required: where the game called <c>AddShapeBatch()</c> the
/// shape draws through that batch, and otherwise through a depth-tested one the processor registers
/// for itself, which is also how the shape shows in Game Studio's scene editor.
/// </summary>
/// <remarks>
/// <para>
/// The entity's world matrix places the shape: its X and Y axes become the plane the shape lies in,
/// so a rotated entity carries the shape into 3D, and the length of the X axis is taken as a uniform
/// scale. In a 2D scene, where entities only ever rotate about Z, this is just position and rotation.
/// </para>
/// <para>
/// Shapes submitted this way draw before manual <see cref="ShapeBatch"/> calls made later in the
/// same frame.
/// </para>
/// </remarks>
// Editor as well as runtime: the scene editor is a running game with the project's compositor, and
// the processor registers its own batch there, so a shape placed in Game Studio is drawn in it.
[DefaultEntityComponentProcessor(typeof(ShapeProcessor), ExecutionMode = ExecutionMode.All)]
// DataContract is what makes the component usable from Game Studio at all: without it the editor
// cannot clone the component to the game side and reports "No serializer available for type".
[DataContract("ShapeComponent")]
[Display("Shape", Expand = ExpandRule.Once)]
[ComponentCategory("Rendering")]
public sealed class ShapeComponent : ActivableEntityComponent
{
    /// <summary>
    /// Assigned to <see cref="BorderWidth"/>, <see cref="FillAlpha"/> or <see cref="GlowWidth"/> to take the batch's value
    /// instead of one set here. A negative width or fill is meaningless, which is what makes it a
    /// safe sentinel - and Game Studio's property grid cannot edit a nullable value type at all,
    /// so an optional float has to be expressed this way rather than as float?.
    /// </summary>
    public static readonly float Inherit = -1f;

    /// <summary>
    /// The shape outline in local space, counter-clockwise, at most 8 corners. May be swapped or
    /// edited at runtime; the next frame draws the new outline. A single vertex with
    /// <see cref="Radius"/> set draws a circle; two vertices with a radius draw a capsule.
    /// </summary>
    /// <remarks>
    /// A list rather than an array on purpose: Game Studio's property grid can add to and remove
    /// from a list, and the asset serializer can load one of any length, whereas an array is shown
    /// read-only and loads only into an instance of exactly its size.
    /// </remarks>
    public List<Vector2> Vertices { get; set; } = [];

    /// <summary>The outline colour; the fill derives from it unless <see cref="FillColor"/> is set.</summary>
    public Color Color { get; set; } = Color.White;

    /// <summary>Optional rounding radius added around the outline, in world units.</summary>
    public float Radius { get; set; }

    /// <summary>
    /// Whether the shape faces the camera instead of lying in the entity's own plane. The entity's
    /// rotation and scale are ignored when this is set; only its position is used.
    /// </summary>
    public bool Billboard { get; set; }

    /// <summary>
    /// Whether the shape answers <see cref="ShapeBatch.TryPick(Vector2, out ShapeHit, float)"/>: the
    /// hit's <see cref="ShapeHit.Tag"/> is then this component, and its entity one step away. Off
    /// by default.
    /// </summary>
    public bool Pickable { get; set; }

    /// <summary>
    /// The batch this shape draws through, or <c>null</c> to use the game's default - the first one
    /// registered with <c>AddShapeBatch()</c>.
    /// </summary>
    /// <remarks>
    /// Set this whenever a scene has more than one batch and the shape must land in a particular
    /// one. Library code especially cannot assume anything about the default: it is whichever batch
    /// the host game happened to register first, which may well be depth-tested, and a marker that
    /// must never be occluded would then silently disappear behind scene geometry.
    /// </remarks>
    /// <remarks>
    /// Not serialized: a batch is a live render object owned by the running game, so there is
    /// nothing for Game Studio to author here. Assign it from code.
    /// </remarks>
    [DataMemberIgnore]
    public ShapeBatch? Batch { get; set; }

    /// <summary>
    /// Outline width in on-screen pixels for this shape, or Inherit to use the batch's
    /// <see cref="ShapeBatch.BorderWidth"/>.
    /// </summary>
    public float BorderWidth { get; set; } = Inherit;

    /// <summary>
    /// Fill intensity for this shape, 0 to 1, or Inherit to use the batch's
    /// <see cref="ShapeFill.Alpha"/>. Set 0 for an unfilled outline.
    /// </summary>
    public float FillAlpha { get; set; } = Inherit;

    /// <summary>
    /// The fill's own colour for this shape. Leave it fully transparent - an alpha of zero, which
    /// is the default - to use the batch's <see cref="ShapeFill.Color"/>, which itself
    /// defaults to filling with the outline colour. For a see-through fill use <see cref="FillAlpha"/>
    /// rather than a transparent colour here.
    /// </summary>
    public Color FillColor { get; set; }

    /// <summary>
    /// Width of a soft glow outside this shape's outline, in on-screen pixels, or Inherit to use
    /// the batch's <see cref="ShapeGlow.Width"/>. Set 0 for none.
    /// </summary>
    public float GlowWidth { get; set; } = Inherit;

    /// <summary>
    /// The glow's colour for this shape. Leave it fully transparent - an alpha of zero, which is
    /// the default - to use the batch's <see cref="ShapeGlow.Color"/>, which itself defaults
    /// to the outline colour.
    /// </summary>
    public Color GlowColor { get; set; }
}