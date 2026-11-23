using Box2D.NET;
using Example.Common;
using Stride.CommunityToolkit.Box2D;
using Stride.CommunityToolkit.Engine;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace E06_Box2D.Helpers;

/// <summary>
/// Creates and removes the dynamic shapes the demo spawns, and keeps count of them.
/// </summary>
/// <remarks>
/// Split out of <see cref="SceneManager"/>, which orchestrates the demo: input, UI and the static
/// yard. Everything here is about one shape becoming an entity, a body and a fixture, which is the
/// part of the Box2D integration worth reading on its own.
/// </remarks>
/// <param name="simulation">The simulation the bodies are created in.</param>
/// <param name="shapeFactory">Supplies the models and entities the bodies are attached to.</param>
public sealed class ShapeSpawner(Box2DSimulation simulation, ShapeFactory shapeFactory)
{
    private readonly List<Entity> _spawned = [];

    /// <summary>Gets how many spawned shapes there are: every one since the last <see cref="Clear"/>.</summary>
    public int Count => _spawned.Count;

    /// <summary>
    /// The shape definition used for every fixture in this demo, built from the
    /// <see cref="GameConfig"/> material values.
    /// </summary>
    /// <returns>A shape definition with the demo's density, friction and restitution.</returns>
    public static B2ShapeDef DefaultShapeDef()
        => ShapeFixtureBuilder.CreateCustomShapeDef(GameConfig.DefaultDensity, GameConfig.DefaultFriction, GameConfig.DefaultRestitution);

    /// <summary>
    /// Spawns <paramref name="count"/> copies of one of the factory's models at its default positions.
    /// </summary>
    /// <param name="model">The model to spawn, one of the <see cref="ShapeFactory"/> properties.</param>
    /// <param name="count">How many to spawn.</param>
    /// <param name="color">An override colour, or <see langword="null"/> for the model's own.</param>
    public void Add(Shape2DModel model, int count, Color? color = null)
    {
        for (var i = 0; i < count; i++)
        {
            AttachBody(model, shapeFactory.CreateEntity(model, color));
        }
    }

    /// <summary>
    /// Spawns one of the factory's models at a world position.
    /// </summary>
    /// <param name="model">The model to spawn.</param>
    /// <param name="position">The world position to spawn at.</param>
    /// <param name="color">An override colour, or <see langword="null"/> for the model's own.</param>
    /// <returns>The body created, for a caller that sets something on it.</returns>
    public B2BodyId Add(Shape2DModel model, Vector2 position, Color? color = null)
        => AttachBody(model, shapeFactory.CreateEntity(model, color, position));

    /// <summary>
    /// Spawns <paramref name="count"/> shapes of randomly chosen types.
    /// </summary>
    /// <param name="count">How many to spawn.</param>
    public void AddRandom(int count)
    {
        for (var i = 0; i < count; i++)
        {
            var shapeModel = shapeFactory.GetRandomShapeModel();

            AttachBody(shapeModel, shapeFactory.CreateEntity(shapeModel, overrideColor: GameConfig.ShapeColor));
        }
    }

    /// <summary>
    /// Spawns <paramref name="count"/> pairs of shapes, each pair tied together by a distance joint.
    /// </summary>
    /// <param name="count">How many pairs to spawn.</param>
    public void AddWithJoints(int count)
    {
        for (var i = 0; i < count; i++)
        {
            CreateConnectedShapePair();
        }
    }

    /// <summary>
    /// Spawns one shape at a world position, for example where the mouse was clicked.
    /// </summary>
    /// <param name="position">The world position to spawn at.</param>
    /// <returns>The model that was spawned.</returns>
    public Shape2DModel AddAtPosition(Vector2 position)
    {
        var shapeModel = shapeFactory.GetRandomShapeModel();

        AttachBody(shapeModel, shapeFactory.CreateEntity(shapeModel, GameConfig.SelectedShapeColor, position));

        return shapeModel;
    }

    /// <summary>
    /// Removes every spawned shape from the scene and the simulation.
    /// </summary>
    public void Clear()
    {
        foreach (var entity in _spawned)
        {
            simulation.RemoveBody(entity);
            entity.Remove();
        }

        _spawned.Clear();
    }

    private B2BodyId AttachBody(Shape2DModel shapeModel, Entity entity)
    {
        var bodyId = simulation.CreateDynamicBody(entity, entity.Transform.Position.XY());

        ShapeFixtureBuilder.AttachShape(shapeModel.Type, bodyId, DefaultShapeDef());

        _spawned.Add(entity);

        return bodyId;
    }

    private void CreateConnectedShapePair()
    {
        var shapeModel1 = shapeFactory.GetRandomShapeModel();
        var shapeModel2 = shapeFactory.GetRandomShapeModel();

        var entity1 = shapeFactory.CreateEntity(shapeModel1, GameConfig.ConstraintColor);
        var entity2 = shapeFactory.CreateEntity(shapeModel2, GameConfig.ConstraintColor);

        // The second shape is placed a joint length away from the first, so the joint starts at rest
        // rather than yanking the pair together on the first step.
        entity2.Transform.Position = entity1.Transform.Position + new Vector3(GameConfig.DefaultJointLength, 0, 0);

        var bodyA = AttachBody(shapeModel1, entity1);
        var bodyB = AttachBody(shapeModel2, entity2);

        // Centre to centre, sprung, and limited to the length it starts at
        simulation.Joints.CreateDistance(bodyA, bodyB, entity1.Transform.Position.XY(), entity2.Transform.Position.XY(), new DistanceJointOptions
        {
            EnableSpring = true,
            Hertz = GameConfig.JointHertz,
            DampingRatio = GameConfig.JointDampingRatio,
            EnableLimit = true,
            Length = GameConfig.DefaultJointLength,
            MinLength = GameConfig.DefaultJointLength,
            MaxLength = GameConfig.DefaultJointLength,
        });
    }
}