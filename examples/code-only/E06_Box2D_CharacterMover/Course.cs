using Box2D.NET;
using Stride.CommunityToolkit.Box2D;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Shapes;
using Stride.Core.Mathematics;
using Stride.Engine;
using static Box2D.NET.B2Bodies;

namespace E06_Box2D_CharacterMover;

/// <summary>What the course hands back: the pieces the program reads or moves every frame.</summary>
/// <param name="Hero">The character's entity, for the camera to follow.</param>
/// <param name="HeroShape">The capsule drawn for the character, recoloured as it lands and jumps.</param>
/// <param name="Mover">The mover that drives the character.</param>
/// <param name="Ball">The ball K kicks and R puts back.</param>
/// <param name="Terrain">The ground outlines, in world space, for drawing.</param>
public sealed record CourseParts(Entity Hero, ShapeComponent HeroShape, CharacterMover2D Mover, B2BodyId Ball, IReadOnlyList<Vector2[]> Terrain);

/// <summary>
/// The course of the samples' Mover scene, in the order the sample builds it: two grounds, the bridge
/// between them, a friend, a ball, an elevator and the hero.
/// </summary>
public static class Course
{
    private const float Scale = 0.2f;
    private const int PlankCount = 50;
    private const float BridgeStart = 48.7f;
    private const float BridgeHeight = 9.2f;
    private const float ElevatorAmplitude = 4f;

    /// <summary>Where the character starts, and where R puts it back.</summary>
    public static readonly Vector2 HeroStart = new(2, 8);

    /// <summary>Where the ball waits, and where R puts it back.</summary>
    public static readonly Vector2 BallStart = new(7, 7);

    // Where the elevator rides
    private static readonly Vector2 ElevatorBase = new(112, 10);

    public static CourseParts Build(Box2DSimulation simulation, Scene scene)
    {
        List<Vector2[]> terrain = [];

        // The two grounds sit side by side; the bridge spans the gap between them.
        var ground1 = BuildGround(simulation, terrain, GroundPaths.Ground1, new Vector2(0, 0), new Vector2(-50, -200));
        var ground2 = BuildGround(simulation, terrain, GroundPaths.Ground2, new Vector2(98, 0), new Vector2(0, -200));

        BuildBridge(simulation, scene, ground1, ground2);
        BuildFriend(simulation, scene);

        var ball = BuildBall(simulation, scene);

        BuildElevator(simulation, scene);

        var (hero, heroShape, mover) = BuildHero(simulation, scene);

        return new CourseParts(hero, heroShape, mover, ball, terrain);
    }

    // One ground outline: the sample's Inkscape path, offset and scaled into metres, as a closed chain
    // on a static body at bodyPosition. Chain points are in the body's frame, so the drawn copy is
    // shifted into the world.
    private static B2BodyId BuildGround(Box2DSimulation simulation, List<Vector2[]> terrain, string path, Vector2 bodyPosition, Vector2 svgOffset)
    {
        var local = SvgPath2D.Parse(path, svgOffset, Scale);
        var body = simulation.CreateStaticBody(bodyPosition);

        ShapeFixtureBuilder.AttachChain(local, body, isLoop: true);
        terrain.Add([.. local.Select(p => p + bodyPosition)]);

        return body;
    }

    // Fifty planks on revolute joints between the two grounds: the soft spring keeps the bridge from
    // folding, the small motor torque stiffens it.
    private static void BuildBridge(Box2DSimulation simulation, Scene scene, B2BodyId ground1, B2BodyId ground2)
    {
        var options = new RevoluteJointOptions { EnableSpring = true, Hertz = 3, DampingRatio = 0.8f, EnableMotor = true, MaxMotorTorque = 10 };
        Vector2[] plank = [new(-0.5f, -0.125f), new(0.5f, -0.125f), new(0.5f, 0.125f), new(-0.5f, 0.125f)];
        // Each plank hinges on the one before it: the first on ground one, the last pinned to ground two.
        var previous = ground1;

        for (var i = 0; i < PlankCount; i++)
        {
            var centre = new Vector2(BridgeStart + 0.5f + i, BridgeHeight);
            var entity = Spawn(scene, "Plank", new ShapeComponent { Vertices = [.. plank], Color = Color.RoyalBlue }, centre);

            var body = simulation.CreateDynamicBody(entity, centre);
            b2Body_SetAngularDamping(body, 0.2f);
            ShapeFixtureBuilder.AttachShape(Primitive2D.Rectangle(1f, 0.25f), body);

            simulation.Joints.CreateRevolute(previous, body, new Vector2(BridgeStart + i, BridgeHeight), options);
            previous = body;
        }

        simulation.Joints.CreateRevolute(previous, ground2, new Vector2(BridgeStart + PlankCount, BridgeHeight), options);
    }

    // A static capsule in the mover category: the mover overlaps it but never sweeps against it, and
    // a tiny push limit with no velocity clip makes it soft - walk into it and you pass through slowly.
    private static void BuildFriend(Box2DSimulation simulation, Scene scene)
    {
        var position = new Vector2(32, 4.5f);
        var entity = Spawn(scene, "Friend", new ShapeComponent { Vertices = [new(0, -0.5f), new(0, 0.5f)], Radius = 0.3f, Color = Color.PaleGreen }, position);

        var body = simulation.CreateStaticBody(entity, position);
        var def = ShapeFixtureBuilder.CreateDefaultShapeDef().InCategory(CharacterMover2D.MoverCategory);
        var shape = ShapeFixtureBuilder.AttachShape(Primitive2D.Capsule(radius: 0.3f, height: 1.6f), body, def);

        CharacterMover2D.SetResponse(shape, maxPush: 0.025f, clipVelocity: false);
    }

    // A bouncy ball in the debris category: the mover walks through it and K kicks it.
    private static B2BodyId BuildBall(Box2DSimulation simulation, Scene scene)
    {
        var entity = Spawn(scene, "Ball", new ShapeComponent { Vertices = [Vector2.Zero], Radius = 0.3f, Color = Color.Goldenrod }, BallStart);

        var body = simulation.CreateDynamicBody(entity, BallStart);
        var def = ShapeFixtureBuilder.CreateDefaultShapeDef().InCategory(CharacterMover2D.DebrisCategory);
        // Bouncy, with a little rolling resistance so it settles.
        def.material.restitution = 0.7f;
        def.material.rollingResistance = 0.2f;
        ShapeFixtureBuilder.AttachShape(Primitive2D.Circle(0.3f), body, def);

        return body;
    }

    // A kinematic platform in the dynamic category, rigid to the mover: a push limit of ten
    // centimetres a step and velocity clipping, so it carries the mover rather than passing through.
    private static void BuildElevator(Box2DSimulation simulation, Scene scene)
    {
        var start = ElevatorBase - new Vector2(0, ElevatorAmplitude);
        Vector2[] slab = [new(-2, -0.1f), new(2, -0.1f), new(2, 0.1f), new(-2, 0.1f)];
        var entity = Spawn(scene, "Elevator", new ShapeComponent { Vertices = [.. slab], Color = Color.Plum }, start);

        var body = simulation.CreateKinematicBody(entity, start);
        var def = ShapeFixtureBuilder.CreateDefaultShapeDef().InCategory(CharacterMover2D.DynamicCategory);
        var shape = ShapeFixtureBuilder.AttachShape(Primitive2D.Rectangle(4f, 0.2f), body, def);

        CharacterMover2D.SetResponse(shape, maxPush: 0.1f, clipVelocity: true);

        simulation.RegisterSimulationUpdate(new Elevator(body, ElevatorBase, ElevatorAmplitude));
    }

    // The character: an entity with a capsule outline, and the mover that drives it. Registered with
    // the simulation, the mover steps itself after every fixed physics step.
    private static (Entity Hero, ShapeComponent Shape, CharacterMover2D Mover) BuildHero(Box2DSimulation simulation, Scene scene)
    {
        var shape = new ShapeComponent { Vertices = [new(0, -0.5f), new(0, 0.5f)], Radius = 0.3f, Color = Color.Orange };
        var hero = Spawn(scene, "Hero", shape, HeroStart);
        var mover = new CharacterMover2D(HeroStart) { Entity = hero };

        simulation.RegisterSimulationUpdate(mover);

        return (hero, shape, mover);
    }

    // An entity with an outline to draw, placed in the scene; the body that moves it comes next.
    private static Entity Spawn(Scene scene, string name, ShapeComponent shape, Vector2 position)
    {
        var entity = new Entity(name, new Vector3(position, 0)) { shape };

        entity.Scene = scene;

        return entity;
    }
}