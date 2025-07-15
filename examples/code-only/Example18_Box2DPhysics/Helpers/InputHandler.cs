using Box2D.NET;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Joints;

namespace Example18_Box2DPhysics.Helpers;

public class InputHandler
{
    private readonly Game _game;
    private readonly Scene _scene;
    private readonly Box2DSimulation _simulation;
    private readonly CameraComponent _camera;
    private readonly ShapeFactory _shapeFactory;
    private readonly B2WorldId _worldId;
    public int CubeCount { get; private set; }

    public InputHandler(Game game, Scene scene, Box2DSimulation simulation,
        CameraComponent camera, ShapeFactory shapeFactory)
    {
        _game = game;
        _scene = scene;
        _simulation = simulation;
        _camera = camera;
        _shapeFactory = shapeFactory;
        _worldId = simulation.GetWorldId();
    }

    public void ProcessKeyboardInput()
    {
        if (_game.Input.IsKeyPressed(Keys.M))
        {
            AddShapes(Primitive2DModelType.Square2D, 10);
        }
        else if (_game.Input.IsKeyPressed(Keys.R))
        {
            AddShapes(Primitive2DModelType.Rectangle2D, 10);
        }
        else if (_game.Input.IsKeyPressed(Keys.C))
        {
            AddShapes(Primitive2DModelType.Circle2D, 10);
        }
        else if (_game.Input.IsKeyPressed(Keys.T))
        {
            AddShapes(Primitive2DModelType.Triangle2D, 10);
        }
        else if (_game.Input.IsKeyPressed(Keys.V))
        {
            AddShapes(Primitive2DModelType.Capsule, 10);
        }
        else if (_game.Input.IsKeyPressed(Keys.P))
        {
            AddShapes(count: 50);
        }
        else if (_game.Input.IsKeyPressed(Keys.J))
        {
            AddShapesWithConstraint(10);
        }
        else if (_game.Input.IsKeyReleased(Keys.X))
        {
            ClearAllShapes();
        }
        else if (_game.Input.IsKeyPressed(Keys.G))
        {
            AddBlackRectangleShapes();
        }

        void ClearAllShapes()
        {
            // Should we remove also constraints?
            foreach (var entity in _scene.Entities.Where(w => w.Name.EndsWith(GameConfig.ShapeName)).ToList())
            {
                _simulation?.RemoveBody(entity);
                entity.Remove();
            }

            SetCubeCount();
        }
    }

    public void ProcessMouseInput()
    {
        if (!_game.Input.IsMouseButtonPressed(MouseButton.Left)) return;

        var mousePosition = _game.Input.MousePosition;
        var ray = _camera.CalculateRayPlaneIntersectionPoint(mousePosition);

        if (ray is null)
        {
            Console.WriteLine("No intersection with the plane found for the mouse ray.");

            return;
        }

        var hitBodyId = _simulation.OverlapPoint(ray.Value);

        if (!hitBodyId.HasValue) return;

        var entity = _simulation.GetEntity(hitBodyId.Value);

        if (entity == null)
        {
            var bodyName = b2Body_GetName(hitBodyId.Value);
            Console.WriteLine($"Hit body with name {bodyName} but no associated entity found.");

            return;
        }

        var position = b2Body_GetPosition(hitBodyId.Value);
        // If the entity is a 2D primitive, we can apply an impulse to it
        var impulse = new B2Vec2(0.0f, 3.0f); // Apply an upward impulse

        b2Body_ApplyLinearImpulseToCenter(hitBodyId.Value, impulse, true);

        Console.WriteLine($"Applied impulse to {entity.Name} at position {position.X} , {position.Y}");
    }

    private void AddShapes(Primitive2DModelType? type = null, int count = 5, Color? color = null)
    {
        for (var i = 1; i <= count; i++)
        {
            var shapeModel = _shapeFactory.GetShapeModel(type);

            if (shapeModel == null) return;

            var entity = _shapeFactory.CreateEntity(shapeModel, color);
            var bodyId = _simulation.CreateDynamicBody(entity, entity.Transform.Position);

            PhysicsHelper.CreateShapePhysics(shapeModel, bodyId);
        }

        SetCubeCount();
    }

    private void SetCubeCount() => CubeCount = _scene.Entities.Count(w => w.Name.EndsWith(GameConfig.ShapeName));

    public void AddBlackRectangleShapes() => AddShapes(Primitive2DModelType.Rectangle2D, 50, Color.Black);

    private void AddShapesWithConstraint(int count = 5)
    {
        var defaultLength = 1f;

        for (var i = 1; i <= count; i++)
        {
            var shapeModel1 = _shapeFactory.GetShapeModel();
            var shapeModel2 = _shapeFactory.GetShapeModel();

            if (shapeModel1 == null || shapeModel2 == null) return;

            var entity1 = _shapeFactory.CreateEntity(shapeModel1);
            var entity2 = _shapeFactory.CreateEntity(shapeModel2);
            entity2.Transform.Position = new Vector3(entity1.Transform.Position.X + defaultLength,
                entity1.Transform.Position.Y, entity1.Transform.Position.Z);

            var bodyIdA = _simulation.CreateDynamicBody(entity1, entity1.Transform.Position);
            var bodyIdB = _simulation.CreateDynamicBody(entity2, entity2.Transform.Position);

            PhysicsHelper.CreateShapePhysics(shapeModel1, bodyIdA);
            PhysicsHelper.CreateShapePhysics(shapeModel2, bodyIdB);

            B2DistanceJointDef jointDef = b2DefaultDistanceJointDef();
            jointDef.hertz = 2.0f;
            jointDef.dampingRatio = 0.5f;
            jointDef.length = defaultLength;
            jointDef.maxLength = defaultLength;
            jointDef.minLength = defaultLength;
            jointDef.enableSpring = false;
            jointDef.enableLimit = false;
            //jointDef.collideConnected = true;

            jointDef.@base.bodyIdA = bodyIdA;
            jointDef.@base.bodyIdB = bodyIdB;
            jointDef.@base.localFrameA.p = new B2Vec2(0, 0);
            jointDef.@base.localFrameB.p = new B2Vec2(0.0f, 0);

            b2CreateDistanceJoint(_worldId, ref jointDef);
        }

        SetCubeCount();
    }
}