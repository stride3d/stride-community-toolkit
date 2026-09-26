using Box2D.NET;
using Stride.Core.Mathematics;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2MathFunction;

namespace Stride.CommunityToolkit.Box2D;

/// <summary>
/// The arithmetic behind <see cref="Joints2D"/>: a joint definition wants each anchor as a
/// <em>local frame</em>, a point and a rotation in the body's own space, and callers think in
/// world space. This converts one to the other, and is kept free of world state so it can be
/// tested on a bare transform.
/// </summary>
internal static class JointFrames2D
{
    /// <summary>
    /// The local frame on a body whose transform is <paramref name="body"/> for a world pivot and
    /// a world rotation: the pivot in body space, and the rotation that, applied after the body's
    /// own, gives <paramref name="worldAngle"/>. Two frames built from the same pivot and angle on
    /// two bodies coincide in world space, which is what makes the current pose the joint's zero.
    /// </summary>
    internal static B2Transform LocalFrame(in B2Transform body, Vector2 worldPivot, float worldAngle = 0f)
    {
        var pivot = new B2Vec2(worldPivot.X, worldPivot.Y);
        var frameRotation = b2MakeRot(worldAngle);

        return new B2Transform(b2InvTransformPoint(body, pivot), b2InvMulRot(body.q, frameRotation));
    }

    /// <summary>Same, read straight off a live body.</summary>
    internal static B2Transform LocalFrame(B2BodyId body, Vector2 worldPivot, float worldAngle = 0f)
        => LocalFrame(b2Body_GetTransform(body), worldPivot, worldAngle);

    /// <summary>
    /// The angle of a world axis, for joints whose frame x-axis is their axis of motion. The axis
    /// need not be normalised.
    /// </summary>
    /// <exception cref="ArgumentException">The axis has no length.</exception>
    internal static float AxisAngle(Vector2 worldAxis)
    {
        if (worldAxis.LengthSquared() <= 0)
            throw new ArgumentException("The joint axis must have a direction.", nameof(worldAxis));

        return MathF.Atan2(worldAxis.Y, worldAxis.X);
    }

    /// <summary>A local frame back in world space, for drawing anchors.</summary>
    internal static Vector2 WorldPoint(in B2Transform body, in B2Transform localFrame)
    {
        var point = b2TransformPoint(body, localFrame.p);

        return new Vector2(point.X, point.Y);
    }
}