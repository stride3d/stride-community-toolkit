using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Engine;
using Stride.Games;

namespace Stride.CommunityToolkit.Engine;

/// <summary>
/// Extension methods on <see cref="Game"/> that add a ground to the root scene, with a static collider from the
/// physics engine the scene setup chose.
/// </summary>
public static class GroundExtensions
{
    /// <summary>
    /// Adds a ground to the root scene: a plane of the given width (X) and length (Z), with a static collider from
    /// the physics engine the scene setup chose.
    /// </summary>
    /// <param name="game">The game whose root scene gets the ground.</param>
    /// <param name="size">The width and length of the plane. Defaults to <see cref="GameDefaults.Default3DGroundSize"/>.</param>
    /// <param name="options">
    /// The entity options. <see cref="PrimitiveEntityOptions.EntityName"/> defaults to <see cref="GameDefaults.DefaultGroundName"/>,
    /// <see cref="PrimitiveEntityOptions.Material"/> to a dark grey. The ground is <see cref="BodyMode.Static"/> whatever
    /// <see cref="PrimitiveEntityOptions.Body"/> says, except <see cref="BodyMode.None"/>, which leaves the collider out; a
    /// physics package's options (<c>BepuPhysicsOptions</c>) are read back by that package, and a component supplied on
    /// them is used as it is. The options are read, not changed: the ground is built from a copy.
    /// </param>
    /// <returns>The ground entity, already in the root scene.</returns>
    /// <remarks>
    /// Without a physics engine in the setup, or in a Game Studio project, the ground is the plane alone.
    /// </remarks>
    public static Entity Add3DGround(this IGame game, Vector2? size = null, PrimitiveEntityOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(game);

        options ??= new();

        // A ground is immovable: static unless the caller asked for no physics at all. A copy, so the
        // caller's options are left as given
        var ground = options.WithBody(options.Body == BodyMode.None ? BodyMode.None : BodyMode.Static);

        ground.EntityName ??= GameDefaults.DefaultGroundName;
        ground.Material ??= game.CreateMaterial(GameDefaults.DefaultGroundMaterialColor, 0.0f, 0.1f);

        var groundSize = size ?? new Vector2(GameDefaults.Default3DGroundSize.X, GameDefaults.Default3DGroundSize.Z);
        var shape = Primitive3D.Plane(groundSize.X, groundSize.Y);

        var entity = game.CreatePrimitive(shape, ground);

        entity.Scene = game.GetSceneSystem().SceneInstance.RootScene;

        return entity;
    }

    /// <summary>
    /// Adds a ground to the root scene of a 2D game: a box of the given width (X) and height (Y), with a static
    /// collider from the physics engine the scene setup chose.
    /// </summary>
    /// <param name="game">The game whose root scene gets the ground.</param>
    /// <param name="size">The width and height of the ground. Defaults to <see cref="GameDefaults.Default2DGroundSize"/>.</param>
    /// <param name="options">
    /// The entity options. <see cref="PrimitiveEntityOptions.EntityName"/> defaults to <see cref="GameDefaults.DefaultGroundName"/>,
    /// <see cref="PrimitiveEntityOptions.Material"/> to a flat grey and <see cref="PrimitiveEntityOptions.Position"/> to
    /// <see cref="GameDefaults.Default2DGroundPosition"/>. The ground is <see cref="BodyMode.Static"/> whatever
    /// <see cref="PrimitiveEntityOptions.Body"/> says, except <see cref="BodyMode.None"/>, which leaves the collider out; a
    /// component supplied on a physics package's options is used as it is. The options are read, not changed: the
    /// ground is built from a copy.
    /// </param>
    /// <returns>The ground entity, already in the root scene.</returns>
    /// <remarks>
    /// The model is a box with some depth, so the ground reads as solid from a tilted camera; the collider is the
    /// flat rectangle, one unit deep like every other 2D collider.
    /// </remarks>
    public static Entity Add2DGround(this IGame game, Vector2? size = null, PrimitiveEntityOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(game);

        options ??= new();

        // Two copies, so the caller's options are left as given: the model from the box with no physics,
        // the collider from the flat shape, static, attached below
        var model = options.WithBody(BodyMode.None);

        model.EntityName ??= GameDefaults.DefaultGroundName;
        model.Material ??= game.CreateFlatMaterial(GameDefaults.Default2DGroundMaterialColor);
        model.Position ??= GameDefaults.Default2DGroundPosition;

        var groundSize = size ?? new Vector2(GameDefaults.Default2DGroundSize.X, GameDefaults.Default2DGroundSize.Y);

        var box = Primitive3D.Box(new Vector3(groundSize.X, groundSize.Y, GameDefaults.Default2DGroundSize.Z));
        var shape = Primitive2D.Rectangle(groundSize.X, groundSize.Y);

        var entity = game.CreatePrimitive(box, model);

        if (options.Body != BodyMode.None)
            PhysicsSetupBase.Of(game).AddBody(entity, shape, options.WithBody(BodyMode.Static));

        entity.Scene = game.GetSceneSystem().SceneInstance.RootScene;

        return entity;
    }
}