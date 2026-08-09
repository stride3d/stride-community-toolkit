using Stride.BepuPhysics;
using Stride.BepuPhysics.Constraints;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace Example15_Constraint_Rope;

/// <summary>
/// How a rope is put together. Every field here changes how stable the finished rope is.
/// </summary>
/// <param name="LinkCount">Number of segments. Longer ropes are harder to keep stable.</param>
/// <param name="LinkRadius">Radius of one segment.</param>
/// <param name="LinkSpacing">Gap left between segments, so neighbours never start out overlapping.</param>
/// <param name="LinkMass">Mass of one segment. The ratio against <paramref name="WeightMass"/> is what
/// usually decides whether a rope behaves.</param>
/// <param name="LeverArm">Where each link constraint is anchored, measured from the segment centre.
/// Zero anchors at the centre and removes angular oscillation entirely; a value near
/// <paramref name="LinkRadius"/> anchors at the segment ends, which looks more natural and is far
/// less stable.</param>
/// <param name="SkipSpan">How far ahead each segment is also tied to. 1 links only to the next
/// segment; higher values add "skip" constraints that let impulses take shortcuts along the rope.</param>
/// <param name="WeightRadius">Radius of the weight hanging on the end.</param>
/// <param name="WeightMass">Mass of that weight.</param>
public sealed record RopeSettings(
    int LinkCount,
    float LinkRadius,
    float LinkSpacing,
    float LinkMass,
    float LeverArm,
    int SkipSpan,
    float WeightRadius,
    float WeightMass);

/// <summary>
/// A built rope: its segments, the weight on the end, and the skip constraints so they can be
/// switched off at runtime.
/// </summary>
public sealed record Rope(
    IReadOnlyList<BodyComponent> Links,
    BodyComponent Weight,
    IReadOnlyList<DistanceLimitConstraintComponent> LinkConstraints,
    DistanceLimitConstraintComponent WeightConstraint,
    IReadOnlyList<DistanceLimitConstraintComponent> SkipConstraints,
    RopeSettings Settings)
{
    /// <summary>
    /// Moves every link constraint between anchoring at the segment centres and at their ends, and
    /// switches the skip constraints with it, so a rope can be flipped between the stable build and
    /// the naive one while it hangs.
    /// </summary>
    /// <remarks>
    /// The allowed distance has to move with the anchors. Pulling them in from the ends shortens the
    /// gap they measure by the lever arm at each end, so it is subtracted twice; leave the distance
    /// alone and the rope changes length instead of changing behaviour.
    /// </remarks>
    public void SetStabilised(bool stabilised)
    {
        var leverArm = stabilised ? 0 : Settings.LeverArm;
        var step = Settings.LinkRadius * 2 + Settings.LinkSpacing;

        foreach (var constraint in LinkConstraints)
        {
            constraint.LocalOffsetA = new Vector3(0, -leverArm, 0);
            constraint.LocalOffsetB = new Vector3(0, leverArm, 0);
            constraint.MaximumDistance = step - leverArm * 2;
            constraint.MinimumDistance = constraint.MaximumDistance * 0.1f;
        }

        // The weight hangs from the last segment on a constraint of its own, and it has to move with
        // the rest. Leaving it anchored at the segment end while every other joint is anchored at a
        // centre leaves one asymmetric joint at the bottom, which shows up as the last link sitting
        // visibly out of line while the rope swings.
        WeightConstraint.LocalOffsetA = new Vector3(0, -leverArm, 0);
        WeightConstraint.MaximumDistance = Settings.LinkSpacing + Settings.LinkRadius - leverArm;
        WeightConstraint.MinimumDistance = WeightConstraint.MaximumDistance * 0.1f;

        foreach (var constraint in SkipConstraints)
        {
            constraint.Enabled = stabilised;
        }
    }

    /// <summary>
    /// Distance from the fixed anchor down to the weight. A rope holding its shape keeps this
    /// roughly constant; one that is losing the fight visibly stretches.
    /// </summary>
    public float Length => Vector3.Distance(Links[0].Position, Weight.Position);
}

/// <summary>
/// Builds a rope as a chain of small dynamic bodies tied together with distance limits.
/// </summary>
/// <remarks>
/// There is no rope type in Bepu, and the obvious construction - rigid ball sockets between
/// segments - is the one that misbehaves. The approach here follows Bepu's own RopeStabilityDemo:
/// <list type="bullet">
/// <item>Link with a <see cref="DistanceLimitConstraintComponent"/> rather than a ball socket, with
/// a minimum of a tenth of the maximum. A rope should be free to go slack; only stretching is
/// forbidden.</item>
/// <item>Anchor those constraints at the segment centres when stability matters. A zero lever arm
/// means a segment's own rotation cannot feed back into the chain.</item>
/// <item>Add skip constraints. Tying a segment to several ahead of it lets an impulse travel along
/// shortcuts instead of crawling down the chain one link at a time.</item>
/// </list>
/// </remarks>
public static class RopeBuilder
{
    /// <summary>
    /// Builds a rope hanging straight down from <paramref name="anchor"/>, with a weight on the end.
    /// The topmost segment is kinematic, so it holds the rest up without being dragged down itself.
    /// </summary>
    public static Rope Build(Game game, Scene scene, Vector3 anchor, RopeSettings settings, Color linkColor, Color weightColor)
    {
        var links = new List<BodyComponent>(settings.LinkCount);
        var linkConstraints = new List<DistanceLimitConstraintComponent>();
        var skipConstraints = new List<DistanceLimitConstraintComponent>();

        // Centre-to-centre distance between neighbouring segments.
        var step = settings.LinkRadius * 2 + settings.LinkSpacing;

        for (var i = 0; i < settings.LinkCount; i++)
        {
            var isAnchor = i == 0;

            var entity = CreateSphere(game,
                isAnchor ? "Rope Anchor" : "Rope Link",
                isAnchor ? Color.DarkSlateGray : linkColor,
                anchor - new Vector3(0, i * step, 0),
                settings.LinkRadius,
                settings.LinkMass,
                kinematic: isAnchor);

            entity.Scene = scene;
            links.Add(entity.Get<BodyComponent>());
        }

        var weightEntity = CreateSphere(game, "Weight", weightColor,
            anchor - new Vector3(0, (settings.LinkCount - 1) * step + settings.LinkSpacing + settings.LinkRadius + settings.WeightRadius, 0),
            settings.WeightRadius,
            settings.WeightMass,
            kinematic: false);

        weightEntity.Scene = scene;
        var weight = weightEntity.Get<BodyComponent>();

        // Neighbour links. Pulling the anchors in from the segment ends shortens the allowed gap by
        // the same amount at both ends, which is why the lever arm is subtracted twice.
        var linkMaximum = step - settings.LeverArm * 2;

        for (var i = 0; i < links.Count - 1; i++)
        {
            var limit = CreateLimit(
                links[i], links[i + 1],
                new Vector3(0, -settings.LeverArm, 0),
                new Vector3(0, settings.LeverArm, 0),
                linkMaximum);

            links[i].Entity.Add(limit);
            linkConstraints.Add(limit);
        }

        // Skip constraints. The span is measured in segments, so the allowed distance grows with it.
        for (var i = 0; i < links.Count; i++)
        {
            for (var span = 2; span <= settings.SkipSpan; span++)
            {
                var target = i + span;

                if (target >= links.Count) break;

                var skip = CreateLimit(links[i], links[target], Vector3.Zero, Vector3.Zero, step * span);

                links[i].Entity.Add(skip);
                skipConstraints.Add(skip);
            }
        }

        // The weight itself, tied to the last segment and - when skip constraints are in use - to
        // several segments above it, so its load does not rest on one link alone.
        var weightOffset = new Vector3(0, settings.WeightRadius, 0);
        var weightMaximum = settings.LinkSpacing + settings.LinkRadius - settings.LeverArm;

        var weightConstraint = CreateLimit(
            links[^1], weight,
            new Vector3(0, -settings.LeverArm, 0),
            weightOffset,
            weightMaximum);

        links[^1].Entity.Add(weightConstraint);

        // These anchor at the link's centre, not at its end, so the lever arm plays no part in how far
        // apart they sit. Reusing weightMaximum here makes them a lever arm too short, which leaves
        // them stretched taut before anything has even moved, hauling the last links out of line.
        var weightSkipBase = settings.LinkSpacing + settings.LinkRadius;

        for (var span = 1; span < settings.SkipSpan; span++)
        {
            var index = links.Count - 1 - span;

            if (index < 0) break;

            var skip = CreateLimit(links[index], weight, Vector3.Zero, weightOffset, weightSkipBase + step * span);

            links[index].Entity.Add(skip);
            skipConstraints.Add(skip);
        }

        return new Rope(links, weight, linkConstraints, weightConstraint, skipConstraints, settings);
    }

    /// <remarks>
    /// A minimum of a tenth of the maximum is what makes this behave like rope rather than a rigid
    /// rod: the segments may drift together freely and are only stopped from pulling apart.
    /// </remarks>
    private static DistanceLimitConstraintComponent CreateLimit(BodyComponent a, BodyComponent b, Vector3 offsetA, Vector3 offsetB, float maximumDistance) => new()
    {
        A = a,
        B = b,
        LocalOffsetA = offsetA,
        LocalOffsetB = offsetB,
        MinimumDistance = maximumDistance * 0.1f,
        MaximumDistance = maximumDistance,
        SpringFrequency = 30,
        SpringDampingRatio = 1,
    };

    /// <remarks>
    /// The collider is built by hand rather than left to <c>IncludeCollider</c>, because mass is a
    /// property of the collider shape and the mass ratio is the whole point of this example.
    /// </remarks>
    private static Entity CreateSphere(Game game, string name, Color color, Vector3 position, float radius, float mass, bool kinematic)
    {
        var entity = game.Create3DPrimitive(PrimitiveModelType.Sphere, new Bepu3DPhysicsOptions
        {
            EntityName = name,
            Material = game.CreateMaterial(color),

            // Size for a sphere is its RADIUS, not its diameter - unlike a cube, where Size is the
            // full extent. Passing a diameter here draws every sphere at twice the size of the
            // collider below, and the weight then appears to sail straight through anything it hits.
            Size = new Vector3(radius),
            IncludeCollider = false,
            Component = new BodyComponent
            {
                Kinematic = kinematic,
                Collider = new CompoundCollider
                {
                    Colliders = { new SphereCollider { Radius = radius, Mass = mass } }
                }
            }
        });

        entity.Transform.Position = position;

        return entity;
    }
}
