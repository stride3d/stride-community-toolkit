using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Engine;
using Stride.Games;

namespace Stride.CommunityToolkit.Engine;

/// <summary>
/// The physics engine of a <see cref="SceneSetup"/>: what <see cref="GameExtensions.CreatePrimitive(IGame, Primitive3D, Primitive3DEntityOptions?)"/>
/// and the grounds attach a body or a static collider with. A physics package derives from it and registers its
/// setup through an extension method on the setup (<c>UseBepu()</c>, <c>UseBullet()</c>); <see cref="None"/> attaches
/// nothing, which is the core on its own.
/// </summary>
/// <remarks>
/// The setup is chosen once and decides what the implicit calls mean. The explicit route, the entity extensions of
/// each physics package (<c>AddBepuPhysics</c>, <c>AddBulletPhysics</c>), stays open whatever is chosen, so two
/// engines can share a game.
/// </remarks>
public abstract class PhysicsSetupBase
{
    /// <summary>No physics engine: the primitives get a mesh and nothing else.</summary>
    public static PhysicsSetupBase None { get; } = new NoPhysicsSetup();

    /// <summary>The physics setup registered on <paramref name="game"/> by <see cref="GameExtensions.Run(Game, SceneSetup, Action{Scene}?, Action{Scene, GameTime}?, GameContext?)"/>, or <see cref="None"/>.</summary>
    public static PhysicsSetupBase Of(IGame game)
    {
        ArgumentNullException.ThrowIfNull(game);

        return game.Services.GetService<PhysicsSetupBase>() ?? None;
    }

    /// <summary>Attaches a dynamic body with the collider that matches <paramref name="shape"/>.</summary>
    /// <param name="entity">The entity with the shape's model.</param>
    /// <param name="shape">The shape the model was built from.</param>
    /// <param name="options">The options the entity was created with; a package reads its own derived options back from them.</param>
    public abstract void AddBody(Entity entity, Primitive3D shape, Primitive3DEntityOptions? options);

    /// <inheritdoc cref="AddBody(Entity, Primitive3D, Primitive3DEntityOptions?)"/>
    public abstract void AddBody(Entity entity, Primitive2D shape, Primitive2DEntityOptions? options);

    /// <summary>Attaches a static collider that matches <paramref name="shape"/>: a ground, a wall.</summary>
    /// <param name="entity">The entity with the shape's model.</param>
    /// <param name="shape">The shape the model was built from.</param>
    /// <param name="options">The options the entity was created with; a package reads its own derived options back from them.</param>
    public abstract void AddStatic(Entity entity, Primitive3D shape, Primitive3DEntityOptions? options);

    /// <inheritdoc cref="AddStatic(Entity, Primitive3D, Primitive3DEntityOptions?)"/>
    public abstract void AddStatic(Entity entity, Primitive2D shape, Primitive2DEntityOptions? options);

    private sealed class NoPhysicsSetup : PhysicsSetupBase
    {
        public override void AddBody(Entity entity, Primitive3D shape, Primitive3DEntityOptions? options) { }

        public override void AddBody(Entity entity, Primitive2D shape, Primitive2DEntityOptions? options) { }

        public override void AddStatic(Entity entity, Primitive3D shape, Primitive3DEntityOptions? options) { }

        public override void AddStatic(Entity entity, Primitive2D shape, Primitive2DEntityOptions? options) { }
    }
}