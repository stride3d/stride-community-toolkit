using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Engine;
using Stride.Games;

namespace Stride.CommunityToolkit.Engine;

/// <summary>
/// The physics engine a scene setup chose, as the core sees it: the one that gives a created primitive its body
/// or collider. The physics packages implement it and <c>UseBepu()</c> or <c>UseBullet()</c> set it on the setup;
/// the core alone has <see cref="None"/>, which attaches nothing.
/// </summary>
/// <remarks>
/// Registered as a service by the setup, so <see cref="Of"/> finds it from any game. The options' <see cref="BodyMode"/>
/// decides what is attached; <see cref="BodyMode.None"/> never reaches an implementation.
/// </remarks>
public abstract class PhysicsSetupBase
{
    /// <summary>No physics engine: primitives and grounds get a model and nothing else.</summary>
    public static PhysicsSetupBase None { get; } = new NoPhysicsSetup();

    /// <summary>
    /// The physics setup the game runs with: the one registered as a service, else <see cref="None"/>.
    /// </summary>
    /// <param name="game">The game.</param>
    /// <returns>The setup; never <see langword="null"/>.</returns>
    public static PhysicsSetupBase Of(IGame game)
    {
        ArgumentNullException.ThrowIfNull(game);

        return game.Services.GetService<PhysicsSetupBase>() ?? None;
    }

    /// <summary>
    /// Attaches the physics the options ask for to an entity built from a 3D shape: a body, a kinematic body or
    /// a static collider, with the collider fitted to the shape.
    /// </summary>
    /// <param name="entity">The entity, already carrying the shape's model.</param>
    /// <param name="shape">The shape the model was built from; the collider is derived from it.</param>
    /// <param name="options">The options, whose <see cref="PrimitiveEntityOptions.Body"/> picks the kind; an engine's own options type carries its component and mass.</param>
    public abstract void AddBody(Entity entity, Primitive3D shape, PrimitiveEntityOptions? options);

    /// <summary>
    /// Attaches the physics the options ask for to an entity built from a flat shape, with the collider fitted
    /// to the shape and extruded by the shape's depth.
    /// </summary>
    /// <param name="entity">The entity, already carrying the shape's model.</param>
    /// <param name="shape">The flat shape the model was built from; the collider is derived from it.</param>
    /// <param name="options">The options, whose <see cref="PrimitiveEntityOptions.Body"/> picks the kind; an engine's own options type carries its component and mass.</param>
    public abstract void AddBody(Entity entity, Primitive2D shape, PrimitiveEntityOptions? options);

    private sealed class NoPhysicsSetup : PhysicsSetupBase
    {
        public override void AddBody(Entity entity, Primitive3D shape, PrimitiveEntityOptions? options) { }
        public override void AddBody(Entity entity, Primitive2D shape, PrimitiveEntityOptions? options) { }
    }
}