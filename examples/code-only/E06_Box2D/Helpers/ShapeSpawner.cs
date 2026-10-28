using Box2D.NET;
using Example.Common;
using Stride.CommunityToolkit.Box2D;
using Stride.CommunityToolkit.Engine;
using Stride.Core.Mathematics;
using Stride.Engine;
using static Box2D.NET.B2Joints;

namespace E06_Box2D.Helpers;

/// <summary>
/// Creates and removes the dynamic shapes the demo spawns, and keeps the running total.
/// </summary>
/// <remarks>
/// Split out of <see cref="SceneManager"/>, which orchestrates the demo: input, UI and the static
/// yard. Everything here is about one shape becoming an entity, a body and a fixture, which is the
/// part of the Box2D integration worth reading on its own.
/// </remarks>
/// <param name="scene">The scene the shape entities are added to and removed from.</param>
/// <param name="simulation">The simulation the bodies are created in.</param>
/// <param name="shapeFactory">Supplies the models and entities the bodies are attached to.</param>
public sealed class ShapeSpawner(Scene scene, Box2DSimulation simulation, ShapeFactory shapeFactory)
{
    private readonly B2WorldId _worldId = simulation.WorldId;

    /// <summary>Gets how many shapes have been spawned since the last <see cref="Clear"/>.</summary>
    public int TotalCreated { get; private set; }

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
    /// Spawns <paramref name="count"/> shapes of randomly chosen types.
    /// </summary>
    /// <param name="count">How many to spawn.</param>
    public void AddRandom(int count)
    {
        for (var i = 0; i < count; i++)
        {
            var shapeModel = ShapeFactory.GetRandomShapeModel();

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
        var shapeModel = ShapeFactory.GetRandomShapeModel();

        AttachBody(shapeModel, shapeFactory.CreateEntity(shapeModel, GameConfig.SelectedShapeColor, position));

        return shapeModel;
    }

    /// <summary>
    /// Removes every spawned shape from the scene and the simulation, and resets the total.
    /// </summary>
    public void Clear()
    {
        var shapesToRemove = scene.Entities.Where(e => e.Name.EndsWith(GameConfig.ShapeName)).ToList();

        foreach (var entity in shapesToRemove)
        {
            simulation.RemoveBody(entity);
            entity.Remove();
        }

        TotalCreated = 0;
    }

    private B2BodyId AttachBody(Shape2DModel shapeModel, Entity entity)
    {
        var bodyId = simulation.CreateDynamicBody(entity, entity.Transform.Position);

        ShapeFixtureBuilder.AttachShape(shapeModel.Type, bodyId, DefaultShapeDef());

        TotalCreated++;

        return bodyId;
    }

    private void CreateConnectedShapePair()
    {
        var shapeModel1 = ShapeFactory.GetRandomShapeModel();
        var shapeModel2 = ShapeFactory.GetRandomShapeModel();

        var entity1 = shapeFactory.CreateEntity(shapeModel1, GameConfig.ConstraintColor);
        var entity2 = shapeFactory.CreateEntity(shapeModel2, GameConfig.ConstraintColor);

        // The second shape is placed a joint length away from the first, so the joint starts at rest
        // rather than yanking the pair together on the first step.
        entity2.Transform.Position = entity1.Transform.Position + new Vector3(GameConfig.DefaultJointLength, 0, 0);

        CreateDistanceJoint(AttachBody(shapeModel1, entity1), AttachBody(shapeModel2, entity2));
    }

    private void CreateDistanceJoint(B2BodyId bodyA, B2BodyId bodyB)
    {
        var jointDef = b2DefaultDistanceJointDef();
        jointDef.hertz = GameConfig.JointHertz;
        jointDef.dampingRatio = GameConfig.JointDampingRatio;
        jointDef.length = GameConfig.DefaultJointLength;
        jointDef.maxLength = GameConfig.DefaultJointLength;
        jointDef.minLength = GameConfig.DefaultJointLength;
        jointDef.enableSpring = true;
        jointDef.enableLimit = true;

        jointDef.@base.bodyIdA = bodyA;
        jointDef.@base.bodyIdB = bodyB;
        jointDef.@base.localFrameA.p = new B2Vec2(0, 0);
        jointDef.@base.localFrameB.p = new B2Vec2(0, 0);

        b2CreateDistanceJoint(_worldId, in jointDef);
    }
}