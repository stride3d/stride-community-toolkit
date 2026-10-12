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
    /// The entity options. <see cref="PrimitiveEntityOptionsBase.EntityName"/> defaults to <see cref="GameDefaults.DefaultGroundName"/>,
    /// <see cref="PrimitiveEntityOptionsBase.Material"/> to a dark grey; <see cref="PrimitiveEntityOptionsBase.IncludePhysics"/>
    /// off leaves the collider out. A physics package's options (<c>Bepu3DPhysicsOptions</c>) are read back by that package.
    /// </param>
    /// <returns>The ground entity, already in the root scene.</returns>
    /// <remarks>
    /// Without a physics engine in the setup, or in a Game Studio project, the ground is the plane alone.
    /// </remarks>
    public static Entity Add3DGround(this IGame game, Vector2? size = null, Primitive3DEntityOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(game);

        options ??= new();
        options.EntityName ??= GameDefaults.DefaultGroundName;
        options.Material ??= game.CreateMaterial(GameDefaults.DefaultGroundMaterialColor, 0.0f, 0.1f);

        var groundSize = size ?? new Vector2(GameDefaults.Default3DGroundSize.X, GameDefaults.Default3DGroundSize.Z);
        var shape = Primitive3D.Plane(groundSize.X, groundSize.Y);

        return AddGround(game, shape, options, static (physics, entity, shape, options) => physics.AddStatic(entity, shape, options));
    }

    /// <summary>
    /// Adds a 2D ground to the root scene: a flat box of the given width (X) and height (Y) in the XY plane, at
    /// <see cref="GameDefaults.Default2DGroundPosition"/>, with a static collider from the physics engine the scene
    /// setup chose.
    /// </summary>
    /// <param name="game">The game whose root scene gets the ground.</param>
    /// <param name="size">The width and height of the ground. Defaults to <see cref="GameDefaults.Default2DGroundSize"/>; its depth along Z is always that default's.</param>
    /// <param name="options">
    /// The entity options. <see cref="PrimitiveEntityOptionsBase.EntityName"/> defaults to <see cref="GameDefaults.DefaultGroundName"/>,
    /// <see cref="PrimitiveEntityOptionsBase.Position"/> to <see cref="GameDefaults.Default2DGroundPosition"/>, the material to a flat grey;
    /// <see cref="Primitive2DEntityOptions.Depth"/> is the collider's thickness. A physics package's options (<c>Bepu2DPhysicsOptions</c>) are read back by that package.
    /// </param>
    /// <returns>The ground entity, already in the root scene.</returns>
    public static Entity Add2DGround(this IGame game, Vector2? size = null, Primitive2DEntityOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(game);

        options ??= new();
        options.EntityName ??= GameDefaults.DefaultGroundName;
        options.Material ??= game.CreateFlatMaterial(GameDefaults.Default2DGroundMaterialColor);
        options.Position ??= GameDefaults.Default2DGroundPosition;

        var groundSize = size ?? new Vector2(GameDefaults.Default2DGroundSize.X, GameDefaults.Default2DGroundSize.Y);

        // The model is a box with some depth, so the ground reads as solid from a tilted camera; the collider is
        // the flat shape, extruded by the options' Depth like every other 2D collider
        var box = Primitive3D.Box(new Vector3(groundSize.X, groundSize.Y, GameDefaults.Default2DGroundSize.Z));
        var shape = Primitive2D.Rectangle(groundSize.X, groundSize.Y);

        var model = new Primitive3DEntityOptions
        {
            EntityName = options.EntityName,
            Material = options.Material,
            RenderGroup = options.RenderGroup,
            Position = options.Position,
            IncludePhysics = false,
        };

        var entity = game.CreatePrimitive(box, model);

        if (options.IncludePhysics)
            PhysicsSetupBase.Of(game).AddStatic(entity, shape, options);

        entity.Scene = game.GetSceneSystem().SceneInstance.RootScene;

        return entity;
    }

    private static Entity AddGround(IGame game, Primitive3D shape, Primitive3DEntityOptions options, Action<PhysicsSetupBase, Entity, Primitive3D, Primitive3DEntityOptions> attach)
    {
        // The ground is static, attached below rather than as the body CreatePrimitive would add
        var includePhysics = options.IncludePhysics;

        options.IncludePhysics = false;

        var entity = game.CreatePrimitive(shape, options);

        options.IncludePhysics = includePhysics;

        if (includePhysics)
            attach(PhysicsSetupBase.Of(game), entity, shape, options);

        entity.Scene = game.GetSceneSystem().SceneInstance.RootScene;

        return entity;
    }
}