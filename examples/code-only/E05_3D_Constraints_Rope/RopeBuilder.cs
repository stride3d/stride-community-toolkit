using Stride.BepuPhysics;
using Stride.BepuPhysics.Constraints;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace E05_3D_Constraints_Rope;

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
    /// The mass ratio is the whole point of this example, so each sphere's mass goes on the options and
    /// lands on the collider shape Bepu reads it from.
    /// </remarks>
    private static Entity CreateSphere(Game game, string name, Color color, Vector3 position, float radius, float mass, bool kinematic)
    {
        var entity = game.CreatePrimitive(Primitive3D.Sphere(radius), new Bepu3DPhysicsOptions
        {
            EntityName = name,
            Material = game.CreateMaterial(color),
            Mass = mass,
            Component = new BodyComponent { Kinematic = kinematic, Collider = new CompoundCollider() }
        });

        entity.Transform.Position = position;

        return entity;
    }
}