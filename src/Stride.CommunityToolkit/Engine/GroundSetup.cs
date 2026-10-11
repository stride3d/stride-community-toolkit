using Stride.Engine;

namespace Stride.CommunityToolkit.Engine;

/// <summary>
/// The ground of a <see cref="SceneSetup"/>: a plane in 3D, a flat box in 2D, of a width and a length, with a
/// collider from the setup's physics engine unless <see cref="Collider"/> is off. Built through <see cref="Plane"/>
/// or <see cref="Visual"/>.
/// </summary>
/// <param name="Size">The width (X) and length (Z, or height in 2D) of the ground.</param>
/// <param name="Collider">Whether the physics engine gives it a static collider. Without a physics engine there is none either way.</param>
public sealed record GroundSetup(Vector2 Size, bool Collider = true)
{
    /// <summary>A ground with a collider, <see cref="GameDefaults.Default3DGroundSize"/> unless told otherwise.</summary>
    public static GroundSetup Plane(Vector2? size = null) => new(size ?? new Vector2(GameDefaults.Default3DGroundSize.X, GameDefaults.Default3DGroundSize.Z));

    /// <summary>A ground to look at only: no collider even when the setup has a physics engine.</summary>
    public static GroundSetup Visual(Vector2? size = null) => Plane(size) with { Collider = false };

    internal Entity Apply3D(Game game) => game.Add3DGround(Size, new Primitive3DEntityOptions { IncludePhysics = Collider });

    internal Entity Apply2D(Game game) => game.Add2DGround(Size, new Primitive2DEntityOptions { IncludePhysics = Collider });
}